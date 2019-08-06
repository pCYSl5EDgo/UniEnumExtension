using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.UniEnumExtension;
using Mono.Cecil;
using UnityEditor;

namespace UniEnumExtension
{
    internal static class EnumExtender
    {
        private static Dictionary<string, MethodDefinition> typeToStringDictionary;
        private static IEnumExtensionProcessor<int> processorInt32;
        private static IEnumExtensionProcessor<int> processorInt32Flags;
        private static IEnumExtensionProcessor<uint> processorUInt32;
        private static IEnumExtensionProcessor<uint> processorUInt32Flags;
        private static IEnumExtensionProcessor<long> processorInt64;
        private static IEnumExtensionProcessor<long> processorInt64Flags;
        private static IEnumExtensionProcessor<ulong> processorUInt64;
        private static IEnumExtensionProcessor<ulong> processorUInt64Flags;
        private static IEnumExtensionProcessor<short> processorInt16;
        private static IEnumExtensionProcessor<short> processorInt16Flags;
        private static IEnumExtensionProcessor<ushort> processorUInt16;
        private static IEnumExtensionProcessor<ushort> processorUInt16Flags;
        private static IEnumExtensionProcessor<byte> processorByte;
        private static IEnumExtensionProcessor<byte> processorByteFlags;
        private static IEnumExtensionProcessor<sbyte> processorSByte;
        private static IEnumExtensionProcessor<sbyte> processorSByteFlags;

        public static void Main(IEnumerable<string> assemblyPaths)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }
            EditorApplication.LockReloadAssemblies();
            InitializeFields();
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
            processorInt32 = new EnumExtensionProcessorInt32(typeToStringDictionary);
            processorInt32Flags = new EnumExtensionProcessorInt32Flags(typeToStringDictionary);
        }

        private static void InitializeDictionary()
        {
            var systemModule = GetSystemModule();

            void AddMethodDefinitionToDictionary(string name)
            {
                typeToStringDictionary.Add(name, GetToStringMethodDefinition(systemModule, name));
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
                        processorByteFlags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "SByte":
                        processorSByteFlags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "UInt16":
                        processorUInt16Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "UInt32":
                        processorUInt32Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "UInt64":
                        processorUInt64Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "Int16":
                        processorInt16Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "Int32":
                        processorInt32Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                    case "Int64":
                        processorInt64Flags.Process(enumTypeDefinition, valueFieldDefinition);
                        break;
                }
                return;
            }
            switch (valueFieldDefinition.FieldType.Name)
            {
                case "Byte":
                    processorByte.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "SByte":
                    processorSByte.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "UInt16":
                    processorUInt16.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "UInt32":
                    processorUInt32.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "UInt64":
                    processorUInt64.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "Int16":
                    processorInt16.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "Int32":
                    processorInt32.Process(enumTypeDefinition, valueFieldDefinition);
                    break;
                case "Int64":
                    processorInt64.Process(enumTypeDefinition, valueFieldDefinition);
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
