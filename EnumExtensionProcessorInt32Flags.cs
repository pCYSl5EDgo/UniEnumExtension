using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UniEnumExtension;

namespace Assets.UniEnumExtension
{
    public sealed class EnumExtensionProcessorInt32Flags : IEnumExtensionProcessor<int>
    {
        private readonly Dictionary<string, MethodDefinition> typeToStringDictionary;

        public EnumExtensionProcessorInt32Flags(Dictionary<string, MethodDefinition> typeToStringDictionary)
        {
            this.typeToStringDictionary = typeToStringDictionary;
        }

        public void Process(TypeDefinition enumTypeDefinition, FieldDefinition valueFieldDefinition)
        {
            var dictionary = EnumExtensionUtility.ToDictionary<int>(enumTypeDefinition, valueFieldDefinition, out var minFieldDefinition, out var maxFieldDefinition, out var minValue, out var maxValue);
            var methodToString = EnumExtensionUtility.MakeToString(enumTypeDefinition);
            var sortedList = new SortedList<int, FieldDefinition>(dictionary);
            var baseToStringMethodDefinition = typeToStringDictionary[valueFieldDefinition.FieldType.Name];
            switch (dictionary.Count)
            {
                case 0:
                    EnumExtensionUtility.ProcessCount0(methodToString, valueFieldDefinition, baseToStringMethodDefinition);
                    return;
                case 1:
                    EnumExtensionUtility.ProcessCount1(methodToString, valueFieldDefinition, baseToStringMethodDefinition, minFieldDefinition, minValue);
                    return;
                case 2:
                    var or = minValue | maxValue;
                    if (or == minValue || or == maxValue)
                    {
                        EnumExtensionUtility.ProcessCount2(methodToString, valueFieldDefinition, baseToStringMethodDefinition, minFieldDefinition, maxFieldDefinition, minValue, maxValue);
                    }
                    else
                    {
                        var connect = minFieldDefinition.Name + ", " + maxFieldDefinition.Name;
                        if (or < minValue)
                        {
                            ProcessInt32(methodToString, valueFieldDefinition, baseToStringMethodDefinition, new[]
                            {
                                connect, minFieldDefinition.Name, maxFieldDefinition.Name
                            }, new[]
                            {
                                or, minValue, maxValue
                            });
                        }
                        else if (or > maxValue)
                        {
                            ProcessInt32(methodToString, valueFieldDefinition, baseToStringMethodDefinition, new[]
                            {
                                minFieldDefinition.Name, maxFieldDefinition.Name, connect,
                            }, new[]
                            {
                                minValue, maxValue, or,
                            });
                        }
                        else
                        {
                            ProcessInt32(methodToString, valueFieldDefinition, baseToStringMethodDefinition, new[]
                            {
                                minFieldDefinition.Name, connect, maxFieldDefinition.Name,
                            }, new[]
                            {
                                minValue, or, maxValue,
                            });
                        }
                    }
                    return;
            }
            if (minValue < 0)
            {
                if (maxValue < 0)
                {

                }
                else if (maxValue == 0)
                {

                }
                else
                {

                }
            }
            else if (minValue == 0)
            {

            }
            else
            {
                ProcessOnlyPositive(enumTypeDefinition, methodToString, valueFieldDefinition, sortedList);
            }
        }

        private void ProcessInt32(MethodDefinition methodToString, FieldDefinition valueFieldDefinition, MethodDefinition baseToStringMethodDefinition, string[] strings, int[] ints)
        {
            if (ints.Length == ints[ints.Length - 1] - ints[0] + 1)
            {
                ProcessContinuousInt32(methodToString, valueFieldDefinition, baseToStringMethodDefinition, strings, ints);
            }
            else
            {
                ProcessDiscontinuousInt32(methodToString, valueFieldDefinition, baseToStringMethodDefinition, strings, ints);
            }
        }

        private void ProcessDiscontinuousInt32(MethodDefinition methodToString, FieldDefinition valueFieldDefinition, MethodDefinition baseToStringMethodDefinition, string[] strings, int[] ints)
        {
            
        }

        private void ProcessContinuousInt32(MethodDefinition methodToString, FieldDefinition valueFieldDefinition, MethodDefinition baseToStringMethodDefinition, string[] strings, int[] ints)
        {
            var processor = methodToString.Body.GetILProcessor();
            var elseRoutineFirst = Instruction.Create(OpCodes.Ldarg_0);

            var destinations = new Instruction[ints.Length];
            processor
                .LdArg(0)
                .LdFld(valueFieldDefinition)
                .Sub(ints[0])
                .Switch(destinations)
                .Br(elseRoutineFirst);

            for (var i = 0; i < destinations.Length; i++)
            {
                destinations[i] = Instruction.Create(OpCodes.Ldstr, strings[i]);
                processor.Add(destinations[i]).Ret();
            }

            processor.Add(elseRoutineFirst).LdFldA(valueFieldDefinition).Call(valueFieldDefinition.Module.ImportReference(typeToStringDictionary[valueFieldDefinition.FieldType.Name])).Ret();
        }

        private void ProcessOnlyPositive(TypeDefinition enumTypeDefinition, MethodDefinition methodToString, FieldDefinition valueFieldDefinition, SortedList<int, FieldDefinition> sortedList)
        {
            throw new NotImplementedException();
        }
    }
}
