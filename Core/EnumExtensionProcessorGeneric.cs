using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace UniEnumExtension
{
    public sealed class EnumExtensionProcessorGeneric<T>
        : IEnumExtensionProcessor<T>
        where T : unmanaged, IComparable<T>, IEquatable<T>
    {
        private readonly Dictionary<string, MethodDefinition> typeToStringDictionary;

        public EnumExtensionProcessorGeneric(Dictionary<string, MethodDefinition> typeToStringDictionary)
        {
            this.typeToStringDictionary = typeToStringDictionary;
        }

        public void Process(TypeDefinition enumTypeDefinition, FieldDefinition valueFieldDefinition)
        {
            AddToString(enumTypeDefinition, valueFieldDefinition);
            enumTypeDefinition.Methods.Add(EnumExtensionUtility.MakeIEquatable(enumTypeDefinition, valueFieldDefinition, typeToStringDictionary["Int32"].Module));
        }

        private void AddToString(TypeDefinition enumTypeDefinition, FieldDefinition valueFieldDefinition)
        {
            var dictionary = EnumExtensionUtility.ToDictionary<T>(enumTypeDefinition, valueFieldDefinition, out var minFieldDefinition, out var maxFieldDefinition, out var minValue, out var maxValue);
            var method = EnumExtensionUtility.MakeToString(enumTypeDefinition);
            var baseToStringMethodDefinition = typeToStringDictionary[valueFieldDefinition.FieldType.Name];
            switch (dictionary.Count)
            {
                case 0:
                    EnumExtensionUtility.ProcessCount0(method, valueFieldDefinition, baseToStringMethodDefinition);
                    break;
                case 1:
                    EnumExtensionUtility.ProcessCount1(method, valueFieldDefinition, baseToStringMethodDefinition, minFieldDefinition, minValue);
                    break;
                case 2:
                    EnumExtensionUtility.ProcessCount2(method, valueFieldDefinition, baseToStringMethodDefinition, minFieldDefinition, maxFieldDefinition, minValue, maxValue);
                    break;
                default:
                    EnumExtensionUtility.ProcessCountGreaterThan2(method, valueFieldDefinition, baseToStringMethodDefinition, new SortedList<T, FieldDefinition>(dictionary).Select(pair => (pair.Value.Name, pair.Key)).ToArray());
                    break;
            }
            enumTypeDefinition.Methods.Add(method);
        }
    }
}
