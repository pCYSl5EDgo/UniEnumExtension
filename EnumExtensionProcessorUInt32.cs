using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace UniEnumExtension
{
    public sealed class EnumExtensionProcessorUInt32 : IEnumExtensionProcessor<uint>
    {
        private readonly Dictionary<string, MethodDefinition> typeToStringDictionary;

        public EnumExtensionProcessorUInt32(Dictionary<string, MethodDefinition> typeToStringDictionary)
        {
            this.typeToStringDictionary = typeToStringDictionary;
        }

        public void Process(TypeDefinition enumTypeDefinition, FieldDefinition valueFieldDefinition)
        {
            var dictionary = EnumExtensionUtility.ToDictionary<uint>(enumTypeDefinition, valueFieldDefinition, out var minFieldDefinition, out var maxFieldDefinition, out var minValue, out var maxValue);
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
                    EnumExtensionUtility.ProcessCountGreaterThan2(methodToString, valueFieldDefinition, baseToStringMethodDefinition, new SortedList<uint, FieldDefinition>(dictionary).Select(pair => (pair.Value.Name, pair.Key)).ToArray());
                    break;
            }
            enumTypeDefinition.Methods.Add(methodToString);
        }
    }
}
