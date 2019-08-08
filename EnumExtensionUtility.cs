using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public static class EnumExtensionUtility
    {
        public static Dictionary<T, FieldDefinition> ToDictionary<T>(TypeDefinition enumTypeDefinition, FieldDefinition valueFieldDefinition, out FieldDefinition min, out FieldDefinition max, out T minValue, out T maxValue)
            where T : IComparable<T>
        {
            var dictionary = new Dictionary<T, FieldDefinition>();
            var enumerable = enumTypeDefinition.Fields.Where(x => x != valueFieldDefinition);
            using (var enumerator = enumerable.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    min = max = default;
                    minValue = maxValue = default;
                    return dictionary;
                }
                var fieldDefinition = enumerator.Current ?? throw new NullReferenceException();
                min = max = fieldDefinition;
                var current = (T)fieldDefinition.Constant;
                dictionary[current] = fieldDefinition;
                while (enumerator.MoveNext())
                {
                    fieldDefinition = enumerator.Current ?? throw new NullReferenceException();
                    current = (T)fieldDefinition.Constant;
                    if (current.CompareTo((T)min.Constant) < 0)
                        min = fieldDefinition;
                    else if (current.CompareTo((T)max.Constant) > 0)
                        max = fieldDefinition;
                    if (!dictionary.ContainsKey(current))
                        dictionary.Add(current, fieldDefinition);
                }
                minValue = (T)min.Constant;
                maxValue = (T)max.Constant;
                return dictionary;
            }
        }

        public static MethodDefinition MakeIEquatable(TypeDefinition enumTypeDefinition, FieldDefinition valueFieldDefinition, ModuleDefinition systemModule)
        {
            var interfaceIEquatable = systemModule.GetType("System", "IEquatable`1");
            var mainModule = enumTypeDefinition.Module;
            enumTypeDefinition.Interfaces.Add(new InterfaceImplementation(new GenericInstanceType(mainModule.ImportReference(interfaceIEquatable)) { GenericArguments = { enumTypeDefinition } }));
            var equals = new MethodDefinition("Equals", MethodAttributes.Final | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig, mainModule.TypeSystem.Boolean)
            {
                Parameters = { new ParameterDefinition(enumTypeDefinition) },
                AggressiveInlining = true,
            };
            var processor = equals.Body.GetILProcessor();
            processor
                .LdArg(0)
                .LdFld(valueFieldDefinition)
                .LdArg(1)
                .Ceq()
                .Ret();
            return equals;
        }

        public static MethodDefinition MakeToString(TypeDefinition typeDefinition)
            => new MethodDefinition("ToString",
                MethodAttributes.Final | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                typeDefinition.Module.TypeSystem.String)
            {
                AggressiveInlining = true,
            };

        public static bool ProcessCount0(MethodDefinition methodToString, FieldDefinition valueFieldDefinition, MethodDefinition toStringMethodDefinition)
        {
            methodToString.Body.GetILProcessor()
                .LdArg(0)
                .LdFldA(valueFieldDefinition)
                .Call(valueFieldDefinition.Module.ImportReference(toStringMethodDefinition))
                .Ret();
            return true;
        }

        public static bool ProcessCount1<T>(MethodDefinition methodToString, FieldDefinition valueFieldDefinition, MethodDefinition toStringMethodDefinition, FieldDefinition minFieldDefinition, T minValue) where T : unmanaged, IComparable<T>
        {
            var processor = methodToString.Body.GetILProcessor();
            var defaultIl = Instruction.Create(OpCodes.Ldarg_0);
            processor
                .LdArg(0);
            if (NumberHelper.EqualsZero(minValue))
            {
                processor.BrTrueS(defaultIl);
            }
            else
            {
                processor
                    .LdFld(valueFieldDefinition)
                    .LdC(minValue)
                    .Add(Instruction.Create(OpCodes.Bne_Un_S, defaultIl));
            }
            processor
                .LdStr(minFieldDefinition.Name)
                .Ret()
                .Add(defaultIl)
                .LdFldA(valueFieldDefinition)
                .Call(valueFieldDefinition.Module.ImportReference(toStringMethodDefinition))
                .Ret();
            return true;
        }

        public static void ProcessCount2<T>(MethodDefinition methodToString, FieldDefinition valueFieldDefinition, MethodDefinition toStringMethodDefinition, FieldDefinition minFieldDefinition, FieldDefinition maxFieldDefinition, T minValue, T maxValue) where T : unmanaged, IComparable<T>
        {
            var processor = methodToString.Body.GetILProcessor();
            var defaultIl = Instruction.Create(OpCodes.Ldarg_0);
            if (NumberHelper.EqualsZero(minValue))
            {
                var shortJump = InstructionUtility.LoadConstantGeneric(maxValue);
                processor.LdArg(0).LdFld(valueFieldDefinition).Dup().BrTrueS(shortJump[0]).Pop().LdStr(minFieldDefinition.Name).Ret().AddRange(shortJump).BneS(defaultIl).LdStr(maxFieldDefinition.Name).Ret();
            }
            else if (NumberHelper.EqualsZero(maxValue))
            {
                var shortJump = InstructionUtility.LoadConstantGeneric(minValue);
                processor.LdArg(0).LdFld(valueFieldDefinition).Dup().BrTrueS(shortJump[0]).Pop().LdStr(maxFieldDefinition.Name).Ret().AddRange(shortJump).BneS(defaultIl).LdStr(minFieldDefinition.Name).Ret();
            }
            else
            {
                var shortJump = InstructionUtility.LoadConstantGeneric(maxValue);
                processor
                    .LdArg(0)
                    .LdFld(valueFieldDefinition)
                    .Dup()
                    .LdC(minValue)
                    .BneS(shortJump[0])
                    .Pop()
                    .LdStr(minFieldDefinition.Name)
                    .Ret()
                    .AddRange(shortJump)
                    .BneS(defaultIl)
                    .LdStr(maxFieldDefinition.Name)
                    .Ret();
            }
            processor
                .Add(defaultIl)
                .LdFldA(valueFieldDefinition)
                .Call(valueFieldDefinition.Module.ImportReference(toStringMethodDefinition))
                .Ret();
        }

        public static void ProcessCountGreaterThan2<T>(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodDefinition baseToStringMethodDefinition, (string name, T value)[] sortedArray)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            var processor = method.Body.GetILProcessor();
            var elseRoutineFirst = Instruction.Create(OpCodes.Ldarg_0);
            processor.LdArg(0).LdFld(valueFieldDefinition);
            ref var minValue = ref sortedArray[0].value;
            if (IsContinuous(sortedArray))
            {
                processor.ProcessContinuous(sortedArray, minValue, elseRoutineFirst);
            }
            else if (NumberHelper.IsDifferenceLessThanOrEqualsTo(minValue, sortedArray[sortedArray.LongLength - 1].value, sortedArray.LongLength << 1, out var actualCount))
            {
                processor.MakeSortedArray(sortedArray, actualCount, elseRoutineFirst);
            }
            else
            {
                processor.ProcessDiscontinuous(method, valueFieldDefinition, sortedArray, ref minValue, elseRoutineFirst);
            }
            processor
                .Add(elseRoutineFirst)
                .LdFldA(valueFieldDefinition)
                .Call(valueFieldDefinition.Module.ImportReference(baseToStringMethodDefinition))
                .Ret();
        }

        public static void MakeSortedArray<T>(this ILProcessor processor, (string name, T value)[] sortedArray, long actualCount, Instruction elseRoutineFirst)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            var destinations = new Instruction[actualCount];
            var value = sortedArray[0].value;
            processor.Sub(value).Switch<T>(destinations).Br(elseRoutineFirst);
            for (long i = 0, j = 0; i < destinations.LongLength; i++, value = NumberHelper.Increment(value))
            {
                ref var current = ref sortedArray[j];
                if (current.value.Equals(value))
                {
                    j++;
                    destinations[i] = Instruction.Create(OpCodes.Ldstr, current.name);
                }
                else
                {
                    destinations[i] = Instruction.Create(OpCodes.Ldstr, value.ToString());
                }
                processor.Add(destinations[i]).Ret();
            }
        }

        public static void ProcessDiscontinuous<T>(this ILProcessor processor, MethodDefinition method, FieldDefinition valueFieldDefinition, (string name, T value)[] sortedArray, ref T minValue, Instruction elseRoutineFirst)
            where T : unmanaged, IComparable<T>
        {
            var variableDefinition = new VariableDefinition(valueFieldDefinition.FieldType);
            method.Body.Variables.Add(variableDefinition);

            processor
                .Dup()
                .StLoc(0)
                .LdC(minValue)
                .Blt<T>(elseRoutineFirst)
                .LdLoc(0)
                .LdC(sortedArray[sortedArray.LongLength - 1L].value)
                .Bgt<T>(elseRoutineFirst);

            var groups = DivideIntoContinuousGroup(sortedArray).ToArray();

            if (groups.Length == sortedArray.Length)
            {
                processor.AddRange(BinarySearch(sortedArray, elseRoutineFirst));
            }
            else
            {
                processor.LdLoc(0).AddRange(BinarySearchGroup(sortedArray, groups, 0, groups.Length - 1, elseRoutineFirst));
            }
        }

        private static IEnumerable<Instruction> BinarySearchGroup<T>((string name, T value)[] sortedArray, ArraySegment<(string name, T value)>[] groups, int minIncluded, int maxIncluded, Instruction elseRoutineFirstInstruction)
            where T : unmanaged, IComparable<T>
        {
            if (maxIncluded == minIncluded)
            {
                return OneGroup(sortedArray, ref groups[minIncluded], elseRoutineFirstInstruction);
            }
            var index = (maxIncluded + minIncluded) >> 1;
            ref var group = ref groups[index];
            var bigger = Instruction.Create(OpCodes.Ldloc_0);
            return InstructionUtility.LoadConstantGeneric(group.GetLast().value)
                .Concat(new[]
                {

                    InstructionUtility.Bgt<T>(bigger),
                    Instruction.Create(OpCodes.Ldloc_0),
                })
                .Concat(BinarySearchGroup(sortedArray, groups, minIncluded, index, elseRoutineFirstInstruction))
                .Append(bigger)
                .Concat(BinarySearchGroup(sortedArray, groups, index + 1, maxIncluded, elseRoutineFirstInstruction));
        }

        private static IEnumerable<Instruction> OneGroup<T>((string name, T value)[] sortedArray, ref ArraySegment<(string name, T value)> group, Instruction elseRoutineFirstInstruction)
            where T : unmanaged, IComparable<T>
        {
            var groupOffset = group.Offset;
            ref var first = ref sortedArray[groupOffset];
            switch (group.Count)
            {
                case 1:
                    return InstructionUtility.LoadConstantGeneric(first.value)
                        .Concat(new[]
                        {
                            Instruction.Create(OpCodes.Bne_Un, elseRoutineFirstInstruction),
                            Instruction.Create(OpCodes.Ldstr, first.name),
                            Instruction.Create(OpCodes.Ret),
                        });
                case 2:
                    ref var second = ref sortedArray[groupOffset + 1];
                    var next0 = Instruction.Create(OpCodes.Ldloc_0);
                    return InstructionUtility.LoadConstantGeneric(first.value)
                        .Concat(new[]
                        {
                            Instruction.Create(OpCodes.Bne_Un, next0),
                            Instruction.Create(OpCodes.Ldstr, first.name),
                            Instruction.Create(OpCodes.Ret),
                            next0,
                        })
                        .Concat(InstructionUtility.LoadConstantGeneric(second.value))
                        .Concat(new[]
                        {
                            Instruction.Create(OpCodes.Bne_Un, elseRoutineFirstInstruction),
                            Instruction.Create(OpCodes.Ldstr, second.name),
                            Instruction.Create(OpCodes.Ret),
                        });
                default:
                    var groupCount = group.Count;
                    var destinations = new Instruction[groupCount];
                    Instruction[] instructions;
                    if (InstructionUtility.SwitchCount<T>() == 1)
                    {
                        instructions = new Instruction[3 + destinations.Length * 2];
                        instructions[0] = Instruction.Create(OpCodes.Sub);
                        instructions[1] = Instruction.Create(OpCodes.Switch, destinations);
                        instructions[2] = Instruction.Create(OpCodes.Br, elseRoutineFirstInstruction);
                        for (var i = 0; i < groupCount; i++)
                        {
                            instructions[i * 2 + 3] = destinations[i] = Instruction.Create(OpCodes.Ldstr, sortedArray[groupOffset + i].name);
                            instructions[i * 2 + 4] = Instruction.Create(OpCodes.Ret);
                        }
                    }
                    else
                    {
                        instructions = new Instruction[4 + destinations.Length * 2];
                        instructions[0] = Instruction.Create(OpCodes.Sub);
                        instructions[1] = Instruction.Create(typeof(T) == typeof(ulong) ? OpCodes.Conv_U4 : OpCodes.Conv_I4);
                        instructions[2] = Instruction.Create(OpCodes.Switch, destinations);
                        instructions[3] = Instruction.Create(OpCodes.Br, elseRoutineFirstInstruction);
                        for (var i = 0; i < groupCount; i++)
                        {
                            instructions[i * 2 + 4] = destinations[i] = Instruction.Create(OpCodes.Ldstr, sortedArray[groupOffset + i].name);
                            instructions[i * 2 + 5] = Instruction.Create(OpCodes.Ret);
                        }
                    }
                    return InstructionUtility.LoadConstantGeneric(first.value)
                        .Concat(instructions);
            }
        }

        // ReSharper disable once PossibleNullReferenceException
        private static ref (string name, T value) GetLast<T>(this ref ArraySegment<(string name, T value)> segment) => ref segment.Array[segment.Offset + segment.Count - 1];

        public static void ProcessContinuous<T>(this ILProcessor processor, (string name, T value)[] sortedArray, T minValue, Instruction elseRoutineFirst)
            where T : unmanaged, IComparable<T>
        {
            var destinations = new Instruction[sortedArray.Length];
            processor
                .Sub(minValue)
                .Switch<T>(destinations)
                .Br(elseRoutineFirst);
            for (var i = 0; i < destinations.Length; i++)
            {
                processor
                    .Add(destinations[i] = Instruction.Create(OpCodes.Ldstr, sortedArray[i].name))
                    .Ret();
            }
        }

        public static IEnumerable<ArraySegment<(string name, T value)>> DivideIntoContinuousGroup<T>((string name, T value)[] sortedArray) where T : unmanaged, IComparable<T>
        {
            if ((sortedArray?.Length ?? 0) == 0) yield break;
            var start = 0;
            var length = 1;
            while (true)
            {
                if (start + length >= sortedArray.Length)
                {
                    yield return new ArraySegment<(string name, T value)>(sortedArray, start, length);
                    yield break;
                }
                if (NumberHelper.IsDifferenceOne(sortedArray[start + length - 1].value, sortedArray[start + length].value))
                {
                    length++;
                }
                else
                {
                    yield return new ArraySegment<(string name, T value)>(sortedArray, start, length);
                    start += length;
                    length = 1;
                }
            }
        }

        private static IEnumerable<Instruction> BinarySearch<T>((string name, T value)[] sortedArray, Instruction elseRoutineFirstInstruction) where T : unmanaged, IComparable<T>
        {
            var index = sortedArray.LongLength >> 1;
            var neq = Instruction.Create(OpCodes.Ldloc_0);
            var blt = Instruction.Create(OpCodes.Ldloc_0);
            return InstructionUtility.LoadConstantGeneric(sortedArray[index].value)
                .Prepend(Instruction.Create(OpCodes.Ldloc_0))
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Bne_Un_S, neq),
                    Instruction.Create(OpCodes.Ldstr, sortedArray[index].name),
                    Instruction.Create(OpCodes.Ret),
                    neq,
                })
                .Concat(InstructionUtility.LoadConstantGeneric(sortedArray[index].value))
                .Concat(new[]
                {
                    InstructionUtility.Blt<T>(blt),
                    Instruction.Create(OpCodes.Ldloc_0),
                })
                .Concat(BinarySearch(sortedArray, index + 1L, sortedArray.LongLength - 1L, elseRoutineFirstInstruction))
                .Append(blt)
                .Concat(BinarySearch(sortedArray, 0L, index - 1L, elseRoutineFirstInstruction));
        }

        private static IEnumerable<Instruction> BinarySearch<T>((string name, T value)[] sortedArray, long minIncluded, long maxIncluded, Instruction elseRoutineFirstInstruction)
            where T : unmanaged, IComparable<T>
        {
            if (minIncluded == maxIncluded)
            {
                return InstructionUtility.LoadConstantGeneric(sortedArray[minIncluded].value).Concat(new[]
                {
                    Instruction.Create(OpCodes.Bne_Un, elseRoutineFirstInstruction),
                    Instruction.Create(OpCodes.Ldstr, sortedArray[minIncluded].name),
                    Instruction.Create(OpCodes.Ret),
                });
            }
            var neq = Instruction.Create(OpCodes.Ldloc_0);
            if (maxIncluded - minIncluded == 1)
            {
                return InstructionUtility.LoadConstantGeneric(sortedArray[minIncluded].value)
                    .Concat(new[]
                    {
                        Instruction.Create(OpCodes.Bne_Un, neq),
                        Instruction.Create(OpCodes.Ldstr, sortedArray[minIncluded].name),
                        Instruction.Create(OpCodes.Ret),
                        neq,
                    })
                    .Concat(InstructionUtility.LoadConstantGeneric(sortedArray[minIncluded].value))
                    .Concat(new[] {
                        InstructionUtility.Blt<T>(elseRoutineFirstInstruction),
                        Instruction.Create(OpCodes.Ldloc_0),
                    })
                    .Concat(InstructionUtility.LoadConstantGeneric(sortedArray[maxIncluded].value))
                    .Concat(new[] {
                        Instruction.Create(OpCodes.Bne_Un, elseRoutineFirstInstruction),
                        Instruction.Create(OpCodes.Ldstr, sortedArray[maxIncluded].name),
                        Instruction.Create(OpCodes.Ret),
                    });
            }
            var index = (minIncluded + maxIncluded) >> 1;
            var blt = Instruction.Create(OpCodes.Ldloc_0);
            return InstructionUtility.LoadConstantGeneric(sortedArray[index].value)
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Bne_Un, neq),
                    Instruction.Create(OpCodes.Ldstr, sortedArray[index].name),
                    Instruction.Create(OpCodes.Ret),
                    neq,
                    })
                .Concat(InstructionUtility.LoadConstantGeneric(sortedArray[index].value))
                .Concat(new[] {
                    InstructionUtility.Blt<T>(blt),
                    Instruction.Create(OpCodes.Ldloc_0),
                })
                .Concat(BinarySearch(sortedArray, index + 1, maxIncluded, elseRoutineFirstInstruction))
                .Append(blt)
                .Concat(BinarySearch(sortedArray, minIncluded, index - 1, elseRoutineFirstInstruction));
        }

        private static bool IsContinuous<T>((string name, T value)[] sortedArray)
            where T : unmanaged, IComparable<T>
        {
            for (var i = 1; i < sortedArray.Length; i++)
            {
                if (!NumberHelper.IsDifferenceOne(sortedArray[i - 1].value, sortedArray[i].value))
                    return false;
            }
            return true;
        }
    }
}
