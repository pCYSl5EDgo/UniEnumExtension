using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using UnityEditor;
using UnityEngine;

namespace UniEnumExtension
{
    internal class EnumExtender : IExtender
    {
        private Dictionary<string, MethodDefinition> typeToStringDictionary;
        private IEnumExtensionProcessor<int> processorInt32;
        private IEnumExtensionProcessor<int> processorInt32Flags;
        private IEnumExtensionProcessor<uint> processorUInt32;
        private IEnumExtensionProcessor<uint> processorUInt32Flags;
        private IEnumExtensionProcessor<long> processorInt64;
        private IEnumExtensionProcessor<long> processorInt64Flags;
        private IEnumExtensionProcessor<ulong> processorUInt64;
        private IEnumExtensionProcessor<ulong> processorUInt64Flags;
        private IEnumExtensionProcessor<short> processorInt16;
        private IEnumExtensionProcessor<short> processorInt16Flags;
        private IEnumExtensionProcessor<ushort> processorUInt16;
        private IEnumExtensionProcessor<ushort> processorUInt16Flags;
        private IEnumExtensionProcessor<byte> processorByte;
        private IEnumExtensionProcessor<byte> processorByteFlags;
        private IEnumExtensionProcessor<sbyte> processorSByte;
        private IEnumExtensionProcessor<sbyte> processorSByteFlags;
        private IEnumsReplacer enumsReplacer;
        private ModuleDefinition systemModuleDefinition;
        private Dictionary<string, (ModuleDefinition module, Stream stream)> nameToModuleDefinitionDictionary;
        private DefaultAssemblyResolver assemblyResolver;
        private readonly string searchDirectory;

        public EnumExtender(string searchDirectory = null)
        {
            this.searchDirectory = searchDirectory ?? "";
        }

        public void Extend(IEnumerable<string> assemblyPaths)
        {
            InitializeFields(assemblyPaths);
            foreach (var nameAndModule in nameToModuleDefinitionDictionary)
            {
                var start = DateTime.Now.Ticks;
                ProcessEachAssembly(nameAndModule.Value.module);
                Debug.Log(nameAndModule.Key + " : " + new DateTime(DateTime.Now.Ticks - start).Millisecond);
            }
        }

        public void Dispose()
        {
            foreach (var nameAndModule in nameToModuleDefinitionDictionary)
            {
                nameAndModule.Value.module.Write();
            }
            typeToStringDictionary = null;
            processorByte = processorByteFlags = null;
            processorSByte = processorSByteFlags = null;
            processorInt16 = processorInt16Flags = null;
            processorUInt16 = processorUInt16Flags = null;
            processorInt32 = processorInt32Flags = null;
            processorUInt32 = processorUInt32Flags = null;
            processorInt64 = processorInt64Flags = null;
            processorUInt64 = processorUInt64Flags = null;
            if (!(nameToModuleDefinitionDictionary is null))
            {
                foreach (var pair in nameToModuleDefinitionDictionary)
                {
                    var (module, stream) = pair.Value;
                    module.Dispose();
                    stream.Dispose();
                }
                nameToModuleDefinitionDictionary = null;
            }
            systemModuleDefinition.Dispose();
            systemModuleDefinition = null;
        }

        private void InitializeFields(IEnumerable<string> assemblyPaths)
        {
            assemblyResolver = new DefaultAssemblyResolver();
            if (searchDirectory != "")
            {
                assemblyResolver.AddSearchDirectory(searchDirectory);
            }
            systemModuleDefinition = GetSystemModule();
            nameToModuleDefinitionDictionary = new Dictionary<string, (ModuleDefinition, Stream)>();
            InitializeModules(assemblyPaths);
            enumsReplacer = new EnumsReplacer(new IMethodVisitor[]
            {
                new GetValuesVisitor(),
                new HasFlagVisitor(),
                new IsDefinedVisitor(), 

                // Don't need to implement because System.Enum.GetNames is fast enough!
                // new GetNamesVisitor(),
            });
            InitializeDictionary();
            processorSByte = new EnumExtensionProcessorGeneric<sbyte>(typeToStringDictionary);
            processorInt16 = new EnumExtensionProcessorGeneric<short>(typeToStringDictionary);
            processorInt32 = new EnumExtensionProcessorGeneric<int>(typeToStringDictionary);
            processorInt64 = new EnumExtensionProcessorGeneric<long>(typeToStringDictionary);
            processorByte = new EnumExtensionProcessorGeneric<byte>(typeToStringDictionary);
            processorUInt16 = new EnumExtensionProcessorGeneric<ushort>(typeToStringDictionary);
            processorUInt32 = new EnumExtensionProcessorGeneric<uint>(typeToStringDictionary);
            processorUInt64 = new EnumExtensionProcessorGeneric<ulong>(typeToStringDictionary);
            processorInt32Flags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<int>(typeToStringDictionary);
            processorUInt32Flags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<uint>(typeToStringDictionary);
            processorInt16Flags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<short>(typeToStringDictionary);
            processorUInt16Flags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<ushort>(typeToStringDictionary);
            processorSByteFlags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<sbyte>(typeToStringDictionary);
            processorByteFlags = new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<byte>(typeToStringDictionary);
            processorUInt64Flags = new EnumExtensionProcessorFlags64BitSizeGeneric<ulong>(typeToStringDictionary);
            processorInt64Flags = new EnumExtensionProcessorFlags64BitSizeGeneric<long>(typeToStringDictionary);
        }

        private void InitializeModules(IEnumerable<string> assemblyPaths)
        {
            foreach (var assemblyPath in assemblyPaths)
            {
                try
                {
                    var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.ReadWrite);
                    var module = ModuleDefinition.ReadModule(stream, new ReaderParameters
                    {
                        AssemblyResolver = assemblyResolver,
                    });
                    this.nameToModuleDefinitionDictionary.Add(module.Assembly.Name.FullName, (module, stream));
                }
                catch (FileNotFoundException)
                {
                    Debug.LogWarning("File not found!\n" + assemblyPath);
                }
                catch (UnauthorizedAccessException)
                {
                    Debug.LogWarning("Unauthorized Access!\n" + assemblyPath);
                }
            }
        }

        private void InitializeDictionary()
        {
            void AddMethodDefinitionToDictionary(string name)
            {
                typeToStringDictionary.Add(name, GetToStringMethodDefinition(systemModuleDefinition, name));
            }

            typeToStringDictionary = new Dictionary<string, MethodDefinition>(8);
            AddMethodDefinitionToDictionary("Byte");
            AddMethodDefinitionToDictionary("SByte");
            AddMethodDefinitionToDictionary("Int16");
            AddMethodDefinitionToDictionary("UInt16");
            AddMethodDefinitionToDictionary("Int32");
            AddMethodDefinitionToDictionary("UInt32");
            AddMethodDefinitionToDictionary("Int64");
            AddMethodDefinitionToDictionary("UInt64");
        }

        private MethodDefinition GetToStringMethodDefinition(ModuleDefinition systemModule, string name)
        {
            return systemModule.GetType("System", name).Methods.Single(x => x.IsPublic && !x.IsStatic && !x.HasParameters && x.Name == "ToString");
        }

        private void ProcessEachAssembly(ModuleDefinition module)
        {
            enumsReplacer.Replace(module, systemModuleDefinition);
            foreach (var typeDefinition in module.Types)
            {
                if (typeDefinition.IsEnum)
                    ProcessEachEnumType(typeDefinition);
                else if (typeDefinition.HasNestedTypes)
                    ProcessNestedTypes(typeDefinition);
            }
        }

        private void ProcessNestedTypes(TypeDefinition typeDefinition)
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

        private void ProcessEachEnumType(TypeDefinition enumTypeDefinition)
        {
            var valueFieldDefinition = enumTypeDefinition.Fields.Single(x => !x.IsStatic);
            if (enumTypeDefinition.HasCustomAttributes && enumTypeDefinition.CustomAttributes.Any(x => x.AttributeType.FullName == "System.FlagsAttribute"))
            {
                switch (valueFieldDefinition.FieldType.Name)
                {
                    case "Byte":
                        processorByteFlags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        processorByteFlags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "SByte":
                        processorSByteFlags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        processorSByteFlags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "UInt16":
                        processorUInt16Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        processorUInt16Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "UInt32":
                        processorUInt32Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        processorUInt32Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "UInt64":
                        processorUInt64Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        processorUInt64Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "Int16":
                        processorInt16Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        processorInt16Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "Int32":
                        processorInt32Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        processorInt32Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                    case "Int64":
                        processorInt64Flags.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                        processorInt64Flags.ProcessAddIEquatable(enumTypeDefinition);
                        break;
                }
                return;
            }
            switch (valueFieldDefinition.FieldType.Name)
            {
                case "Byte":
                    processorByte.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    processorByte.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "SByte":
                    processorSByte.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    processorSByte.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "UInt16":
                    processorUInt16.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    processorUInt16.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "UInt32":
                    processorUInt32.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    processorUInt32.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "UInt64":
                    processorUInt64.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    processorUInt64.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "Int16":
                    processorInt16.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    processorInt16.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "Int32":
                    processorInt32.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    processorInt32.ProcessAddIEquatable(enumTypeDefinition);
                    break;
                case "Int64":
                    processorInt64.ProcessRewriteToString(enumTypeDefinition, valueFieldDefinition);
                    processorInt64.ProcessAddIEquatable(enumTypeDefinition);
                    break;
            }
        }

        private ModuleDefinition GetUnityCoreModule() => ModuleDefinition.ReadModule(UnityEditorInternal.InternalEditorUtility.GetEngineCoreModuleAssemblyPath(), new ReaderParameters
        {
            AssemblyResolver = assemblyResolver,
        });

        private ModuleDefinition GetSystemModule()
        {
            switch (PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup))
            {
                case ApiCompatibilityLevel.NET_Standard_2_0:
                    return AssemblyDefinition.ReadAssembly(GetDllFolderHelper.GetFolder() + "netstandard.bytes", new ReaderParameters
                    {
                        AssemblyResolver = assemblyResolver,
                    }).MainModule;
                default:
                    return GetUnityCoreModule().ImportReference(typeof(object)).Resolve().Module;
            }
        }
    }
}
