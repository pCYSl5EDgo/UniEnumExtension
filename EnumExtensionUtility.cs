using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                var fieldDefinition = enumerator.Current;
                min = max = fieldDefinition;
                var current = (T)fieldDefinition.Constant;
                dictionary[current] = fieldDefinition;
                while (enumerator.MoveNext())
                {
                    fieldDefinition = enumerator.Current;
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

        public static MethodDefinition MakeToString(TypeDefinition typeDefinition) => new MethodDefinition("ToString", MethodAttributes.Final | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, typeDefinition.Module.TypeSystem.String);

        public static void ProcessCount0(MethodDefinition methodToString, FieldDefinition valueFieldDefinition, MethodDefinition toStringMethodDefinition)
        {
            methodToString.Body.GetILProcessor()
            .LdArg(0)
                .LdFldA(valueFieldDefinition)
                .Call(valueFieldDefinition.Module.ImportReference(toStringMethodDefinition))
                .Ret();
        }

        public static void ProcessCount1<T>(MethodDefinition methodToString, FieldDefinition valueFieldDefinition, MethodDefinition toStringMethodDefinition, FieldDefinition minFieldDefinition, T minValue) where T : unmanaged, IComparable<T>
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

        public static void ProcessCountGreaterThan2<T>(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodDefinition baseToStringMethodDefinition, (string name, T value)[] sortedArray) where T : unmanaged, IComparable<T>
        {
            var processor = method.Body.GetILProcessor();
            var elseRoutineFirst = Instruction.Create(OpCodes.Ldarg_0);
            processor.LdArg(0).LdFld(valueFieldDefinition);
            ref var minValue = ref sortedArray[0].value;
            if (IsContinuous(sortedArray))
            {
                ProcessContinuous(sortedArray, processor, minValue, elseRoutineFirst);
            }
            else
            {
                elseRoutineFirst = ProcessDiscontinuous(method, valueFieldDefinition, sortedArray, ref minValue, processor);
            }
            processor
                .Add(elseRoutineFirst)
                .Call(valueFieldDefinition.Module.ImportReference(baseToStringMethodDefinition))
                .Ret();
        }

        private static Instruction ProcessDiscontinuous<T>(MethodDefinition method, FieldDefinition valueFieldDefinition, (string name, T value)[] sortedArray, ref T minValue, ILProcessor processor)
            where T : unmanaged, IComparable<T>
        {
            var variableDefinition = new VariableDefinition(valueFieldDefinition.FieldType);
            method.Body.Variables.Add(variableDefinition);
            var elseRoutineFirst = Instruction.Create(OpCodes.Ldloca_S, variableDefinition);

            if (NumberHelper.Sub(minValue, sortedArray[sortedArray.LongLength - 1L].value, out var difference))
            {
                processor
                    .LdArg(0)
                    .LdFld(valueFieldDefinition)
                    .Sub(minValue)
                    .Dup()
                    .StLoc(0)
                    .LdC(default(T))
                    .Blt(elseRoutineFirst)
                    .LdLoc(0)
                    .LdC(difference)
                    .Bgt(elseRoutineFirst);

                for (var i = 1; i < sortedArray.Length; i++)
                {
                    ref var item = ref sortedArray[i];
                    NumberHelper.Sub(minValue, item.value, out item.value);
                }
                minValue = default;

                processor.AddRange(BinarySearch(sortedArray, elseRoutineFirst));
            }
            else
            {
                throw new ArgumentException("too much items!");
            }
            return elseRoutineFirst;
        }

        private static void ProcessContinuous<T>((string name, T value)[] sortedArray, ILProcessor processor, T minValue, Instruction elseRoutineFirst)
            where T : unmanaged, IComparable<T>
        {
            var destinations = new Instruction[sortedArray.Length];
            processor
                .Sub(minValue)
                .Switch(destinations)
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
            var constantArray = InstructionUtility.LoadConstantGeneric(sortedArray[index].value);
            return new[]
                {
                    Instruction.Create(OpCodes.Ldloc_0),
                }
                .Concat(constantArray)
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Bne_Un_S, neq),
                    Instruction.Create(OpCodes.Ldstr, sortedArray[index].name),
                    Instruction.Create(OpCodes.Ret),
                    neq,
                })
                .Concat(constantArray)
                .Concat(new[]
                {
                    Instruction.Create(OpCodes.Blt, blt),
                    Instruction.Create(OpCodes.Ldloc_0),
                })
                .Concat(BinarySearch(sortedArray, index + 1L, sortedArray.LongLength - 1L, elseRoutineFirstInstruction))
                .Append(blt)
                .Concat(BinarySearch(sortedArray, 0L, index - 1L, elseRoutineFirstInstruction));
        }

        private static IEnumerable<Instruction> BinarySearch<T>((string name, T value)[] sortedArray, long minIncluded, long maxIncluded, Instruction elseRoutineFirstInstruction)
            where T : unmanaged, IComparable<T>
        {
            var minLoadConstantGeneric = InstructionUtility.LoadConstantGeneric(sortedArray[minIncluded].value);
            if (minIncluded == maxIncluded)
            {
                return minLoadConstantGeneric.Concat(new[]
                {
                    Instruction.Create(OpCodes.Bne_Un, elseRoutineFirstInstruction),
                    Instruction.Create(OpCodes.Ldstr, sortedArray[minIncluded].name),
                    Instruction.Create(OpCodes.Ret),
                });
            }
            var neq = Instruction.Create(OpCodes.Ldloc_0);
            var maxLoadConstantGeneric = InstructionUtility.LoadConstantGeneric(sortedArray[maxIncluded].value);
            if (maxIncluded - minIncluded == 1)
            {
                return minLoadConstantGeneric.Concat(new[]
                {
                    Instruction.Create(OpCodes.Bne_Un, neq),
                    Instruction.Create(OpCodes.Ldstr, sortedArray[minIncluded].name),
                    Instruction.Create(OpCodes.Ret),
                    neq,
                })
                    .Concat(minLoadConstantGeneric)
                    .Concat(new[] {
                        Instruction.Create(OpCodes.Blt, elseRoutineFirstInstruction),
                        Instruction.Create(OpCodes.Ldloc_0),
                    })
                    .Concat(maxLoadConstantGeneric)
                    .Concat(new[] {
                        Instruction.Create(OpCodes.Bne_Un, elseRoutineFirstInstruction),
                        Instruction.Create(OpCodes.Ldstr, sortedArray[maxIncluded].name),
                        Instruction.Create(OpCodes.Ret),
                    });
            }
            var index = (minIncluded + maxIncluded) >> 1;
            var blt = Instruction.Create(OpCodes.Ldloc_0);
            var indexLoadConstantGeneric = InstructionUtility.LoadConstantGeneric(sortedArray[index].value);
            return indexLoadConstantGeneric.Concat(new[]
                {
                    Instruction.Create(OpCodes.Bne_Un, neq),
                    Instruction.Create(OpCodes.Ldstr, sortedArray[index].name),
                    Instruction.Create(OpCodes.Ret),
                    neq,
                    })
                .Concat(indexLoadConstantGeneric)
                .Concat(new[] {
                    Instruction.Create(OpCodes.Blt, blt),
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
