using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace UniEnumExtension
{
    public sealed class EnumIsDefinedTypeProcessorNumberGeneric
        : ITypeProcessor
    {
        public byte Stage => 0;
        public void Process(ModuleDefinition systemModuleDefinition, TypeDefinition typeDefinition)
        {
            if (!typeDefinition.IsEnum)
            {
                return;
            }
            var module = typeDefinition.Module;
            ParameterDefinition parameterDefinition;
            const string paramName = "value_";
            var fieldTypeName = typeDefinition.Fields[0].FieldType.Name;
            switch (fieldTypeName)
            {
                case nameof(SByte):
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Byte):
                case nameof(UInt16):
                    parameterDefinition = new ParameterDefinition(paramName, ParameterAttributes.None, module.TypeSystem.Int32);
                    break;
                case nameof(UInt32):
                    parameterDefinition = new ParameterDefinition(paramName, ParameterAttributes.None, module.TypeSystem.UInt32);
                    break;
                case nameof(Int64):
                    parameterDefinition = new ParameterDefinition(paramName, ParameterAttributes.None, module.TypeSystem.Int64);
                    break;
                case nameof(UInt64):
                    parameterDefinition = new ParameterDefinition(paramName, ParameterAttributes.None, module.TypeSystem.UInt64);
                    break;
                default:
                    throw new ArgumentException();
            }
            var method = new MethodDefinition("IsDefinedNumber", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, module.TypeSystem.Boolean)
            {
                AggressiveInlining = true,
                Parameters = { parameterDefinition }
            };
            typeDefinition.Methods.Add(method);

            var count = typeDefinition.Fields.Count - 1;
            var processor = method.Body.GetILProcessor();
            switch (count)
            {
                case 0:
                    parameterDefinition.Name += "0";
                    processor.LdC(false).Ret();
                    return;
                case 1:
                    parameterDefinition.Name += "1";
                    processor.LdArg(0).LdC(typeDefinition.Fields[1].Constant).Ceq().Ret();
                    return;
                case 2:
                    if (typeDefinition.Fields[1].Constant.Equals(typeDefinition.Fields[2].Constant))
                        goto case 1;
                    parameterDefinition.Name += "2";
                    processor.LdC(typeDefinition.Fields[1].Constant).LdArg(0).Ceq().LdC(typeDefinition.Fields[2].Constant).LdArg(0).Ceq().Or().Ret();
                    return;
                default:
                    MoreThan2(typeDefinition, processor);
                    return;
            }
        }

        private void MoreThan2(TypeDefinition typeDefinition, ILProcessor processor)
        {
            var fields = typeDefinition.Fields;
            var values = Sort(fields);
            switch (fields[0].FieldType.Name)
            {
                case nameof(SByte):
                    MoreThan2Generic<sbyte>(processor, values);
                    break;
                case nameof(Int16):
                    MoreThan2Generic<short>(processor, values);
                    break;
                case nameof(Int32):
                    MoreThan2Generic<int>(processor, values);
                    break;
                case nameof(Int64):
                    MoreThan2Generic<long>(processor, values);
                    break;
                case nameof(Byte):
                    MoreThan2Generic<byte>(processor, values);
                    break;
                case nameof(UInt16):
                    MoreThan2Generic<ushort>(processor, values);
                    break;
                case nameof(UInt32):
                    MoreThan2Generic<uint>(processor, values);
                    break;
                case nameof(UInt64):
                    MoreThan2Generic<ulong>(processor, values);
                    break;
            }
        }

        private void MoreThan2Generic<T>(ILProcessor processor, List<IComparable> values)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            var array = values.Select(x => (T)x).ToArray();

            var first = NumberHelper.Increment(array[0]);
            var isContinuous = true;
            for (var i = 1; i < array.Length; i++, first = NumberHelper.Increment(first))
            {
                if (first.Equals(array[i])) continue;
                isContinuous = false;
                break;
            }
            var isZeroStart = array[0].Equals(default);
            var nameContainer = processor.Body.Method.Parameters[0];
            if (isZeroStart)
            {
                nameContainer.Name += array.Length + "_FromZero_";
            }
            else
            {
                nameContainer.Name += array.Length + "_NotFromZero_";
            }
            nameContainer.Name += isContinuous ? "Continuous" : "Discontinuous";
            var isUnsigned = typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong);

            if (isContinuous)
            {
                if (isUnsigned && isZeroStart)
                {
                    ContinuousUnsignedZeroStart(processor, array[array.Length - 1]);
                }
                else
                {
                    processor
                        .AddRange(InstructionUtility.LoadConstantGeneric(array[array.Length - 1]))
                        .AddRange(new[]
                        {
                            Instruction.Create(OpCodes.Ldarg_0),
                            InstructionUtility.Clt<T>(),
                            InstructionUtility.LoadConstant(0),
                            Instruction.Create(OpCodes.Ceq),
                        })
                        .AddRange(InstructionUtility.LoadConstantGeneric(array[0]))
                        .AddRange(new[]
                        {
                            Instruction.Create(OpCodes.Ldarg_0),
                            InstructionUtility.Cgt<T>(),
                            InstructionUtility.LoadConstant(0),
                            Instruction.Create(OpCodes.Ceq),
                            Instruction.Create(OpCodes.And),
                            Instruction.Create(OpCodes.Ret),
                        });
                }
            }
            else
            {
                var @false = Instruction.Create(OpCodes.Ldc_I4_0);
                processor.LdArg(0).LdC(array[array.Length - 1]).Bgt<T>(@false);
                if (!isUnsigned || !isZeroStart)
                {
                    processor.LdArg(0).LdC(array[0]).Blt<T>(@false);
                }
                processor.AddRange(Discontinuous(array, 0, array.Length - 1, out _))
                    .Add(@false)
                    .Ret();
            }
        }

        private IEnumerable<Instruction> Discontinuous<T>(T[] array, int minIncludeIndex, int maxIncludeIndex, out Instruction first, bool lessOrEqualToMax = false)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            var minIncludeValue = array[minIncludeIndex];
            var maxIncludeValue = array[maxIncludeIndex];
            lessOrEqualToMax |= maxIncludeValue.Equals(array[array.Length - 1]);
            switch (maxIncludeIndex - minIncludeIndex)
            {
                case 0:
                    return Check1Element(out first, minIncludeValue, array);
                case 1:
                    return Check2Element(out first, minIncludeValue, maxIncludeValue, array, lessOrEqualToMax);
                default:
                    if (NumberHelper.DifferenceEqualsTo(minIncludeValue, maxIncludeValue, maxIncludeIndex - minIncludeIndex))
                    {
                        return Continuous(minIncludeValue, maxIncludeValue, out first, array, lessOrEqualToMax);
                    }
                    else
                    {
                        return DiscontinuousBinarySearch(array, minIncludeIndex, maxIncludeIndex, out first, lessOrEqualToMax);
                    }
            }
        }

        private static IEnumerable<Instruction> Check2Element<T>(out Instruction first, T minIncludeValue, T maxIncludeValue, T[] array, bool lessOrEqualToMax)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            Instruction[] loadConstantGeneric;
            if (array[0].Equals(minIncludeValue))
            {
                loadConstantGeneric = InstructionUtility.LoadConstantGeneric(NumberHelper.Increment(maxIncludeValue));
                first = loadConstantGeneric[0];
                return loadConstantGeneric
                    .Concat(new[]
                    {
                        Instruction.Create(OpCodes.Ldarg_0),
                        InstructionUtility.Cgt<T>(),
                        Instruction.Create(OpCodes.Ret),
                    });
            }
            if (lessOrEqualToMax)
            {
                loadConstantGeneric = InstructionUtility.LoadConstantGeneric(NumberHelper.Decrement(minIncludeValue));
                first = loadConstantGeneric[0];
                return loadConstantGeneric
                    .Concat(new[]
                    {
                        Instruction.Create(OpCodes.Ldarg_0),
                        InstructionUtility.Clt<T>(),
                        Instruction.Create(OpCodes.Ret),
                    });
            }
            loadConstantGeneric = InstructionUtility.LoadConstantGeneric(minIncludeValue);
            first = loadConstantGeneric[0];
            return loadConstantGeneric
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ceq),
                    Instruction.Create(OpCodes.Ldarg_0)
                })
                .Concat(InstructionUtility.LoadConstantGeneric(maxIncludeValue))
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Ceq),
                    Instruction.Create(OpCodes.Or),
                    Instruction.Create(OpCodes.Ret),
                });
        }

        private static IEnumerable<Instruction> Check1Element<T>(out Instruction first, T minIncludeValue, T[] array)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            if (minIncludeValue.Equals(array[0]) || minIncludeValue.Equals(array[1]))
            {
                first = InstructionUtility.LoadConstant(1);
                return new[]
                {
                    first,
                    Instruction.Create(OpCodes.Ret),
                };
            }
            var loadConstantGeneric = InstructionUtility.LoadConstantGeneric(minIncludeValue);
            first = loadConstantGeneric[0];
            return loadConstantGeneric
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ceq),
                    Instruction.Create(OpCodes.Ret)
                });
        }

        private IEnumerable<Instruction> DiscontinuousBinarySearch<T>(T[] array, int minIncludeIndex, int maxIncludeIndex, out Instruction first, bool lessOrEqualToMax)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            var index = FindSplit(array, minIncludeIndex, maxIncludeIndex);

            var lessOrEqual = Discontinuous(array, minIncludeIndex, index, out var lessFirst, lessOrEqualToMax: true);
            var greaterThan = Discontinuous(array, index + 1, maxIncludeIndex, out _, lessOrEqualToMax);
            var loadConstantGeneric = InstructionUtility.LoadConstantGeneric(array[index]);
            first = loadConstantGeneric[0];
            return loadConstantGeneric
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Ldarg_0),
                    InstructionUtility.Bge<T>(lessFirst)
                })
                .Concat(greaterThan)
                .Concat(lessOrEqual);
        }

        private static int FindSplit<T>(T[] array, int minIncludeIndex, int maxIncludeIndex)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            var index = (minIncludeIndex + maxIncludeIndex) >> 1;
            while (index != maxIncludeIndex)
            {
                if (NumberHelper.Increment(array[index]).Equals(array[index + 1]))
                {
                    ++index;
                    continue;
                }
                break;
            }
            if (index == maxIncludeIndex)
            {
                index = (minIncludeIndex + maxIncludeIndex) >> 1;
                while (index != minIncludeIndex)
                {
                    if (NumberHelper.Decrement(array[index]).Equals(array[index - 1]))
                    {
                        --index;
                        continue;
                    }
                    break;
                }
                if (index == minIncludeIndex)
                {
                    index = (minIncludeIndex + maxIncludeIndex) >> 1;
                }
            }
            return index;
        }

        private IEnumerable<Instruction> Continuous<T>(T minInclude, T maxInclude, out Instruction firstInstruction, T[] array, bool lessOrEqualToMax)
            where T : unmanaged
        {
            Instruction[] loadConstantGeneric;

            if (minInclude.Equals(array[0]))
            {
                if (lessOrEqualToMax)
                {
                    firstInstruction = Instruction.Create(OpCodes.Ldc_I4_1);
                    return new[] { firstInstruction, Instruction.Create(OpCodes.Ret), };
                }
                loadConstantGeneric = InstructionUtility.LoadConstantGeneric(maxInclude);
                firstInstruction = loadConstantGeneric[0];
                return loadConstantGeneric
                    .Concat(new[]
                    {
                        Instruction.Create(OpCodes.Ldarg_0),
                        InstructionUtility.Clt<T>(),
                        InstructionUtility.LoadConstant(0),
                        Instruction.Create(OpCodes.Ceq),
                        Instruction.Create(OpCodes.Ret),
                    });
            }
            if (lessOrEqualToMax)
            {
                loadConstantGeneric = InstructionUtility.LoadConstantGeneric(minInclude);
                firstInstruction = loadConstantGeneric[0];
                return loadConstantGeneric
                    .Concat(new[]
                    {
                        Instruction.Create(OpCodes.Ldarg_0),
                        InstructionUtility.Cgt<T>(),
                        InstructionUtility.LoadConstant(0),
                        Instruction.Create(OpCodes.Ceq),
                        Instruction.Create(OpCodes.Ret),
                    });
            }
            loadConstantGeneric = InstructionUtility.LoadConstantGeneric(maxInclude);
            firstInstruction = loadConstantGeneric[0];
            return loadConstantGeneric
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Ldarg_0),
                    InstructionUtility.Clt<T>(),
                    InstructionUtility.LoadConstant(0),
                    Instruction.Create(OpCodes.Ceq),
                })
                .Concat(InstructionUtility.LoadConstantGeneric(minInclude))
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Ldarg_0),
                    InstructionUtility.Cgt<T>(),
                    InstructionUtility.LoadConstant(0),
                    Instruction.Create(OpCodes.Ceq),
                    Instruction.Create(OpCodes.And),
                    Instruction.Create(OpCodes.Ret),
                });
        }

        private IEnumerable<Instruction> ContinuousMinChecked<T>(T minInclude, T maxInclude, out Instruction firstInstruction)
            where T : unmanaged
        {
            var loadConstantGeneric = InstructionUtility.LoadConstantGeneric(maxInclude);
            firstInstruction = loadConstantGeneric[0];
            return loadConstantGeneric
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Ldarg_0),
                    InstructionUtility.Clt<T>(),
                    InstructionUtility.LoadConstant(0),
                    Instruction.Create(OpCodes.Ceq),
                    Instruction.Create(OpCodes.Ret),
                });
        }

        private IEnumerable<Instruction> ContinuousMaxChecked<T>(T minInclude, T maxInclude, out Instruction firstInstruction)
            where T : unmanaged
        {
            var loadConstantGeneric = InstructionUtility.LoadConstantGeneric(minInclude);
            firstInstruction = loadConstantGeneric[0];
            return loadConstantGeneric
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Ldarg_0),
                    InstructionUtility.Cgt<T>(),
                    InstructionUtility.LoadConstant(0),
                    Instruction.Create(OpCodes.Ceq),
                    Instruction.Create(OpCodes.Ret),
                });
        }

        private void ContinuousUnsignedZeroStart<T>(ILProcessor processor, T maxInclude)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            processor.LdArg(0).LdC(maxInclude).Cgt<T>().LdC0Ceq().Ret();
        }

        private static List<IComparable> Sort(Collection<FieldDefinition> fields)
        {
            var list = new List<IComparable>(fields.Skip(1).Select(x => (IComparable)x.Constant));
            list.Sort();
            for (var i = list.Count - 1; i >= 1; i--)
            {
                if (list[i].CompareTo(list[i - 1]) == 0)
                {
                    list.RemoveAt(i);
                }
            }
            return list;
        }
    }
}
