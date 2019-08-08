using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using UnityEditor;

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

        public static void Execute(IEnumerable<string> assemblyPaths)
        {
            InitializeFields();
            EditorApplication.LockReloadAssemblies();
            try
            {
                foreach (var assemblyPath in assemblyPaths)
                {
                    ProcessEachAssembly(assemblyPath);
                }
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        private static void InitializeFields()
        {
            InitializeDictionary();
            _processorSByte = new EnumExtensionProcessorGeneric<sbyte>(_typeToStringDictionary);
            _processorInt16 = new EnumExtensionProcessorGeneric<short>(_typeToStringDictionary);
            _processorInt32 = new EnumExtensionProcessorGeneric<int>(_typeToStringDictionary);
            _processorInt64 = new EnumExtensionProcessorGeneric<long>(_typeToStringDictionary);
            _processorByte = new EnumExtensionProcessorGeneric<byte>(_typeToStringDictionary);
            _processorUInt16 = new EnumExtensionProcessorGeneric<ushort>(_typeToStringDictionary);
            _processorUInt32 = new EnumExtensionProcessorGeneric<uint>(_typeToStringDictionary);
            _processorUInt64 = new EnumExtensionProcessorGeneric<ulong>(_typeToStringDictionary);
            _processorInt32Flags = new EnumExtensionProcessorFlagsGeneric<int>(_typeToStringDictionary);
            _processorUInt32Flags = new EnumExtensionProcessorFlagsGeneric<uint>(_typeToStringDictionary);
            _processorUInt64Flags = new EnumExtensionProcessorFlagsGeneric<ulong>(_typeToStringDictionary);
            _processorInt64Flags = new EnumExtensionProcessorFlagsGeneric<long>(_typeToStringDictionary);
            _processorInt16Flags = new EnumExtensionProcessorFlagsGeneric<short>(_typeToStringDictionary);
            _processorUInt16Flags = new EnumExtensionProcessorFlagsGeneric<ushort>(_typeToStringDictionary);
            _processorSByteFlags = new EnumExtensionProcessorFlagsGeneric<sbyte>(_typeToStringDictionary);
            _processorByteFlags = new EnumExtensionProcessorFlagsGeneric<byte>(_typeToStringDictionary);
        }

        private static void InitializeDictionary()
        {
            var systemModule = GetSystemModule();

            void AddMethodDefinitionToDictionary(string name)
            {
                _typeToStringDictionary.Add(name, GetToStringMethodDefinition(systemModule, name));
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
                        _processorByteFlags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "SByte":
                        _processorSByteFlags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "UInt16":
                        _processorUInt16Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "UInt32":
                        _processorUInt32Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "UInt64":
                        _processorUInt64Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "Int16":
                        _processorInt16Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "Int32":
                        _processorInt32Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "Int64":
                        _processorInt64Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                }
                return;
            }
            switch (valueFieldDefinition.FieldType.Name)
            {
                case "Byte":
                    _processorByte.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "SByte":
                    _processorSByte.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "UInt16":
                    _processorUInt16.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "UInt32":
                    _processorUInt32.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "UInt64":
                    _processorUInt64.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "Int16":
                    _processorInt16.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "Int32":
                    _processorInt32.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "Int64":
                    _processorInt64.Process(enumTypeDefinition, valueFieldDefinition);
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
