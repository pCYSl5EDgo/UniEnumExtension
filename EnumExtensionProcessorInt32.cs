using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace UniEnumExtension
{
    public sealed class EnumExtensionProcessorInt32 : IEnumExtensionProcessor<int>
    {
        private readonly Dictionary<string, MethodDefinition> typeToStringDictionary;

        public EnumExtensionProcessorInt32(Dictionary<string, MethodDefinition> typeToStringDictionary)
        {
            this.typeToStringDictionary = typeToStringDictionary;
        }

        public void Process(TypeDefinition enumTypeDefinition, FieldDefinition valueFieldDefinition)
        {
            var dictionary = EnumExtensionUtility.ToDictionary<int>(enumTypeDefinition, valueFieldDefinition, out var minFieldDefinition, out var maxFieldDefinition, out var minValue, out var maxValue);
            var methodToString = EnumExtensionUtility.MakeToString(enumTypeDefinition);
            var baseToStringMethodDefinition = typeToStringDictionary[valueFieldDefinition.FieldType.Name];
            switch (dictionary.Count)
            {
                case 0:
                    EnumExtensionUtility.ProcessCount0(methodToString, valueFieldDefinition, baseToStringMethodDefinition);
                    break;
                case 1:
                    EnumExtensionUtility.ProcessCount1(methodToString, valueFieldDefinition, baseToStringMethodDefinition, minFieldDefinition, minValue);
                    break;
                case 2:
                    EnumExtensionUtility.ProcessCount2(methodToString, valueFieldDefinition, baseToStringMethodDefinition, minFieldDefinition, maxFieldDefinition, minValue, maxValue);
                    break;
                default:
                    EnumExtensionUtility.ProcessCountGreaterThan2(methodToString, valueFieldDefinition, baseToStringMethodDefinition, new SortedList<int, FieldDefinition>(dictionary).Select(pair => (pair.Value.Name, pair.Key)).ToArray());
                    break;
            }
            enumTypeDefinition.Methods.Add(methodToString);
        }
    }
}
