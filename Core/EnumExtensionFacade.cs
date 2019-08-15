﻿using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using UnityEditor;
using UnityEngine;

namespace UniEnumExtension
{
    public sealed class EnumExtender : IExtender
    {
        private readonly ModuleDefinition systemModuleDefinition;
        private readonly Dictionary<string, (ModuleDefinition module, Stream stream)> nameToModuleDefinitionDictionary;
        private readonly DefaultAssemblyResolver assemblyResolver;


        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly IModuleProcessor[][] moduleProcessors;
        private readonly ITypeProcessor[][] typeProcessors;
        private readonly IMethodProcessor[][] methodProcessors;

        public EnumExtender(string searchDirectory = null, bool isLittleEndian = true)
            : this(
                Array.Empty<IModuleProcessor>(),
                new ITypeProcessor[]
                {
                    new EnumIsDefinedTypeProcessorString(isLittleEndian),
                    new EnumIsDefinedTypeProcessorNumberGeneric(),
                    new EnumExtensionProcessorGeneric<byte>(),
                    new EnumExtensionProcessorGeneric<sbyte>(),
                    new EnumExtensionProcessorGeneric<short>(),
                    new EnumExtensionProcessorGeneric<ushort>(),
                    new EnumExtensionProcessorGeneric<int>(),
                    new EnumExtensionProcessorGeneric<uint>(),
                    new EnumExtensionProcessorGeneric<long>(),
                    new EnumExtensionProcessorGeneric<ulong>(),
                    new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<byte>(),
                    new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<sbyte>(),
                    new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<short>(),
                    new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<ushort>(),
                    new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<int>(),
                    new EnumExtensionProcessorFlags32BitOrLessSizeGeneric<uint>(),
                    new EnumExtensionProcessorFlags64BitSizeGeneric<long>(),
                    new EnumExtensionProcessorFlags64BitSizeGeneric<ulong>(),
                },
                new IMethodProcessor[]
                {
                    new EnumGetValuesMethodProcessor(),
                    new EnumHasFlagMethodProcessor(),
                },
                searchDirectory)
        {
        }

        public EnumExtender(IModuleProcessor[] moduleProcessorCollection, ITypeProcessor[] typeProcessorCollection, IMethodProcessor[] methodProcessorCollection, string searchDirectory)
        {
            assemblyResolver = new DefaultAssemblyResolver();
            if (!(searchDirectory is null))
            {
                assemblyResolver.AddSearchDirectory(searchDirectory);
            }
            systemModuleDefinition = GetSystemModule();
            nameToModuleDefinitionDictionary = new Dictionary<string, (ModuleDefinition, Stream)>();

            const int count = 256;
            moduleProcessors = new IModuleProcessor[count][];
            typeProcessors = new ITypeProcessor[count][];
            methodProcessors = new IMethodProcessor[count][];

            for (var i = 0; i < count; i++)
            {
                moduleProcessors[i] = Array.Empty<IModuleProcessor>();
                typeProcessors[i] = Array.Empty<ITypeProcessor>();
                methodProcessors[i] = Array.Empty<IMethodProcessor>();
            }

            foreach (var processor in moduleProcessorCollection)
            {
                ref var processorReference = ref moduleProcessors[processor.Stage];
                Array.Resize(ref processorReference, processorReference.Length + 1);
                processorReference[processorReference.Length - 1] = processor;
            }

            foreach (var processor in typeProcessorCollection)
            {
                ref var processorReference = ref typeProcessors[processor.Stage];
                Array.Resize(ref processorReference, processorReference.Length + 1);
                processorReference[processorReference.Length - 1] = processor;
            }

            foreach (var processor in methodProcessorCollection)
            {
                ref var processorReference = ref methodProcessors[processor.Stage];
                Array.Resize(ref processorReference, processorReference.Length + 1);
                processorReference[processorReference.Length - 1] = processor;
            }
        }

        public void Extend(IEnumerable<string> assemblyPaths)
        {
            InitializeModules(assemblyPaths);
            foreach (var nameAndModule in nameToModuleDefinitionDictionary)
            {
                ProcessEachAssembly(nameAndModule.Value.module);
            }
        }

        private void ProcessEachAssembly(ModuleDefinition module)
        {
            var types = module.Types;
            for (var j = 0; j < 256; j++)
            {
                var methodProcessorArray = methodProcessors[j];
                var typeProcessorArray = typeProcessors[j];
                for (var i = types.Count - 1; i >= 0; i--)
                {
                    Process(types[i], typeProcessorArray, methodProcessorArray);
                }
            }
        }

        public void Dispose()
        {
            foreach (var pair in nameToModuleDefinitionDictionary)
            {
                var (module, stream) = pair.Value;
                module.Write();
                module.Dispose();
                stream.Dispose();
            }
            systemModuleDefinition.Dispose();
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
                    nameToModuleDefinitionDictionary.Add(module.Assembly.Name.FullName, (module, stream));
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

        private void Process(TypeDefinition typeDefinition, ITypeProcessor[] typeProcessorArray, IMethodProcessor[] methodProcessorArray)
        {
            ProcessType(typeDefinition, typeProcessorArray);
            ProcessMethod(typeDefinition, methodProcessorArray);
            ProcessNestedTypes(typeDefinition, typeProcessorArray, methodProcessorArray);
        }

        private void ProcessNestedTypes(TypeDefinition typeDefinition, ITypeProcessor[] typeProcessorArray, IMethodProcessor[] methodProcessorArray)
        {
            if (!typeDefinition.HasNestedTypes) return;
            var types = typeDefinition.NestedTypes;
            for (var i = types.Count - 1; i >= 0; i--)
            {
                Process(types[i], typeProcessorArray, methodProcessorArray);
            }
        }

        private void ProcessMethod(TypeDefinition typeDefinition, IMethodProcessor[] methodProcessorArray)
        {
            var methods = typeDefinition.Methods;
            for (var j = methods.Count - 1; j >= 0; j--)
            {
                var methodDefinition = methods[j];
                if (!methodDefinition.HasBody) continue;
                for (var index = methodProcessorArray.Length - 1; index >= 0; index--)
                {
                    methodProcessorArray[index].Process(systemModuleDefinition, methodDefinition);
                }
            }
        }

        private void ProcessType(TypeDefinition typeDefinition, ITypeProcessor[] typeProcessorArray)
        {
            for (var index = typeProcessorArray.Length - 1; index >= 0; index--)
            {
                typeProcessorArray[index].Process(systemModuleDefinition, typeDefinition);
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
