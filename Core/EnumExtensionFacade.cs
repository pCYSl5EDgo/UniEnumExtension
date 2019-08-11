using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using UnityEditor;
using UnityEngine;

namespace UniEnumExtension
{
    internal static class EnumExtender
    {
        private static Dictionary<string, MethodDefinition> _typeToStringDictionary;
        private static IEnumExtensionProcessor<int> _processorInt32;
        private static IEnumExtensionProcessor<int> _processorInt32Flags;
        private static IEnumExtensionProcessor<uint> _processorUInt32;
        private static IEnumExtensionProcessor<uint> _processorUInt32Flags;
        private static IEnumExtensionProcessor<long> _processorInt64;
        private static IEnumExtensionProcessor<long> _processorInt64Flags;
        private static IEnumExtensionProcessor<ulong> _processorUInt64;
        private static IEnumExtensionProcessor<ulong> _processorUInt64Flags;
        private static IEnumExtensionProcessor<short> _processorInt16;
        private static IEnumExtensionProcessor<short> _processorInt16Flags;
        private static IEnumExtensionProcessor<ushort> _processorUInt16;
        private static IEnumExtensionProcessor<ushort> _processorUInt16Flags;
        private static IEnumExtensionProcessor<byte> _processorByte;
        private static IEnumExtensionProcessor<byte> _processorByteFlags;
        private static IEnumExtensionProcessor<sbyte> _processorSByte;
        private static IEnumExtensionProcessor<sbyte> _processorSByteFlags;
        private static ModuleDefinition _systemModule;

        public static void Execute(IEnumerable<string> assemblyPaths)
        {
            InitializeFields();
            EditorApplication.LockReloadAssemblies();
            try
            {
                foreach (var assemblyPath in assemblyPaths)
                {
                    var start = DateTime.Now.Ticks;
                    ProcessEachAssembly(assemblyPath);
                    Debug.Log(assemblyPath + " : " + new DateTime(DateTime.Now.Ticks - start).Millisecond);
                }
            }
            finally
            {
                Dispose();
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        private static void Dispose()
        {
            _typeToStringDictionary = null;
            _processorByte = _processorByteFlags = null;
            _processorSByte = _processorSByteFlags = null;
            _processorInt16 = _processorInt16Flags = null;
            _processorUInt16 = _processorUInt16Flags = null;
            _processorInt32 = _processorInt32Flags = null;
            _processorUInt32 = _processorUInt32Flags = null;
            _processorInt64 = _processorInt64Flags = null;
            _processorUInt64 = _processorUInt64Flags = null;
            _systemModule.Dispose();
            _systemModule = null;
        }

        private static void InitializeFields()
        {
            _systemModule = GetSystemModule();
            InitializeDictionary();
            _processorSByte = new EnumExtensionProcessorGeneric<sbyte>(_typeToStringDictionary);
            _processorInt16 = new EnumExtensionProcessorGeneric<short>(_typeToStringDictionary);
            _processorInt32 = new EnumExtensionProcessorGeneric<int>(_typeToStringDictionary);
            _processorInt64 = new EnumExtensionProcessorGeneric<long>(_typeToStringDictionary);
            _processorByte = new EnumExtensionProcessorGeneric<byte>(_typeToStringDictionary);
            _processorUInt16 = new EnumExtensionProcessorGeneric<ushort>(_typeToStringDictionary);
            _processorUInt32 = new EnumExtensionProcessorGeneric<uint>(_typeToStringDictionary);
            _processorUInt64 = new EnumExtensionProcessorGeneric<ulong>(_typeToStringDictionary);
            _processorInt32Flags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<int>(_typeToStringDictionary);
            _processorUInt32Flags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<uint>(_typeToStringDictionary);
            _processorInt16Flags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<short>(_typeToStringDictionary);
            _processorUInt16Flags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<ushort>(_typeToStringDictionary);
            _processorSByteFlags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<sbyte>(_typeToStringDictionary);
            _processorByteFlags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<byte>(_typeToStringDictionary);
            _processorUInt64Flags = new EnumExtensionProcessorFlags64BitSizeGeneric<ulong>(_typeToStringDictionary);
            _processorInt64Flags = new EnumExtensionProcessorFlags64BitSizeGeneric<long>(_typeToStringDictionary);
        }

        private static void InitializeDictionary()
        {
            void AddMethodDefinitionToDictionary(string name)
            {
                _typeToStringDictionary.Add(name, GetToStringMethodDefinition(_systemModule, name));
            }

            _typeToStringDictionary = new Dictionary<string, MethodDefinition>(8);
            AddMethodDefinitionToDictionary("Byte");
            AddMethodDefinitionToDictionary("SByte");
            AddMethodDefinitionToDictionary("Int16");
            AddMethodDefinitionToDictionary("UInt16");
            AddMethodDefinitionToDictionary("Int32");
            AddMethodDefinitionToDictionary("UInt32");
            AddMethodDefinitionToDictionary("Int64");
            AddMethodDefinitionToDictionary("UInt64");
        }

        private static MethodDefinition GetToStringMethodDefinition(ModuleDefinition systemModule, string name)
        {
            return systemModule.GetType("System", name).Methods.Single(x => x.IsPublic && !x.IsStatic && !x.HasParameters && x.Name == "ToString");
        }

        private static void ProcessEachAssembly(string assemblyPath)
        {
            try
            {
                using (var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var module = ModuleDefinition.ReadModule(stream);
                    foreach (var typeDefinition in module.Types)
                    {
                        if (typeDefinition.IsEnum)
                            ProcessEachEnumType(typeDefinition);
                        else if (typeDefinition.HasNestedTypes)
                            ProcessNestedTypes(typeDefinition);
                    }
                    module.Write(stream);
                }
            }
            catch (FileNotFoundException)
            {
                Debug.LogWarning("File Not Found!\n" + assemblyPath);
            }
            catch (UnauthorizedAccessException)
            {
                Debug.LogWarning("Unauthorized Access!\n" + assemblyPath);
            }
        }

        private static void ProcessNestedTypes(TypeDefinition typeDefinition)
        {
            foreach (var typeDefinitionNestedType in typeDefinition.NestedTypes)
            {
                if (typeDefinitionNestedType.IsEnum)
                {
                    ProcessEachEnumType(typeDefinitionNestedType);
                }
                else if (typeDefinitionNestedType.HasNestedTypes)
                {
                    ProcessNestedTypes(typeDefinitionNestedType);
                }
            }
        }

        private static void ProcessEachEnumType(TypeDefinition enumTypeDefinition)
        {
            var valueFieldDefinition = enumTypeDefinition.Fields.Single(x => !x.IsStatic);
            if (enumTypeDefinition.HasCustomAttributes && enumTypeDefinition.CustomAttributes.Any(x => x.AttributeType.FullName == "System.FlagsAttribute"))
            {
                switch (valueFieldDefinition.FieldType.Name)
                {
                    case "Byte":
                        _processorByteFlags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        _processorByteFlags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "SByte":
                        _processorSByteFlags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        _processorSByteFlags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "UInt16":
                        _processorUInt16Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        _processorUInt16Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "UInt32":
                        _processorUInt32Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        _processorUInt32Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "UInt64":
                        _processorUInt64Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        _processorUInt64Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "Int16":
                        _processorInt16Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        _processorInt16Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "Int32":
                        _processorInt32Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        _processorInt32Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "Int64":
                        _processorInt64Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        _processorInt64Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                }
                return;
            }
            switch (valueFieldDefinition.FieldType.Name)
            {
                case "Byte":
                    _processorByte.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    _processorByte.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "SByte":
                    _processorSByte.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    _processorSByte.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "UInt16":
                    _processorUInt16.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    _processorUInt16.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "UInt32":
                    _processorUInt32.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    _processorUInt32.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "UInt64":
                    _processorUInt64.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    _processorUInt64.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "Int16":
                    _processorInt16.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    _processorInt16.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "Int32":
                    _processorInt32.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    _processorInt32.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "Int64":
                    _processorInt64.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    _processorInt64.ProcessAddIEquatable(enumTypeDefinition);
                    break;
            }
        }
        private static ModuleDefinition GetUnityCoreModule() => ModuleDefinition.ReadModule(UnityEditorInternal.InternalEditorUtility.GetEngineCoreModuleAssemblyPath());

        private static ModuleDefinition GetSystemModule()
        {
            switch (PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup))
            {
                case ApiCompatibilityLevel.NET_Standard_2_0:
                    return AssemblyDefinition.ReadAssembly(GetDllFolderHelper.GetFolder() + "netstandard.bytes").MainModule;
                default:
                    return GetUnityCoreModule().ImportReference(typeof(object)).Resolve().Module;
            }
        }
    }
}
