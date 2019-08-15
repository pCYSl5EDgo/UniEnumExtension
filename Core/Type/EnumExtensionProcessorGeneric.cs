using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace UniEnumExtension
{
    public sealed unsafe class EnumExtensionProcessorGeneric<T>
        : ITypeProcessor
        where T : unmanaged, IComparable<T>, IEquatable<T>
    {
        private static readonly string FullName;

        static EnumExtensionProcessorGeneric()
        {
            FullName = typeof(T).FullName;
        }

        public void ProcessRewriteToString(ModuleDefinition systemModuleDefinition, TypeDefinition enumTypeDefinition, FieldDefinition valueFieldDefinition)
        {
            var toStringMethodReference = enumTypeDefinition.Module.ImportReference(systemModuleDefinition.GetType("System", valueFieldDefinition.FieldType.Name).Methods.Single(x => x.IsPublic && !x.IsStatic && !x.HasParameters && x.Name == "ToString"));
            var dictionary = EnumExtensionUtility.ToDictionary<T>(enumTypeDefinition, valueFieldDefinition, out var minFieldDefinition, out var maxFieldDefinition, out var minValue, out var maxValue);
            var method = EnumExtensionUtility.MakeToString(enumTypeDefinition);
            switch (dictionary.Count)
            {
                case 0:
                    EnumExtensionUtility.ProcessCount0(method, valueFieldDefinition, toStringMethodReference);
                    break;
                case 1:
                    EnumExtensionUtility.ProcessCount1(method, valueFieldDefinition, toStringMethodReference, minFieldDefinition, minValue);
                    break;
                case 2:
                    EnumExtensionUtility.ProcessCount2(method, valueFieldDefinition, toStringMethodReference, minFieldDefinition, maxFieldDefinition, minValue, maxValue);
                    break;
                default:
                    EnumExtensionUtility.ProcessCountGreaterThan2(method, valueFieldDefinition, toStringMethodReference, new SortedList<T, FieldDefinition>(dictionary).Select(pair => (pair.Value.Name, pair.Key)).ToArray());
                    break;
            }
            enumTypeDefinition.Methods.Add(method);
        }

        public void ProcessAddIEquatable(ModuleDefinition systemModuleDefinition, TypeDefinition enumTypeDefinition)
        {
            enumTypeDefinition.Methods.Add(EnumExtensionUtility.MakeIEquatable(enumTypeDefinition, systemModuleDefinition));
        }

        public byte Stage => (byte)(64 + sizeof(T));

        public void Process(ModuleDefinition systemModuleDefinition, TypeDefinition typeDefinition)
        {
            if (!typeDefinition.IsEnum)
            {
                return;
            }
            if (typeDefinition.HasCustomAttributes && typeDefinition.CustomAttributes.Any(customAttribute => customAttribute.AttributeType.FullName == "System.FlagsAttribute"))
            {
                return;
            }
            var valueFieldDefinition = typeDefinition.Fields[0];
            if (valueFieldDefinition.FieldType.FullName != FullName)
            {
                return;
            }
            ProcessRewriteToString(systemModuleDefinition, typeDefinition, typeDefinition.Fields[0]);
            ProcessAddIEquatable(systemModuleDefinition, typeDefinition);
        }
    }
}
