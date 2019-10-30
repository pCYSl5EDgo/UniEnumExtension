using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public static unsafe class EnumExtensionUtility
    {
        public static bool ImplementNoFlag<T>(ModuleDefinition systemModuleDefinition, ModuleDefinition moduleDefinition, TypeDefinition enumTypeDefinition, MethodDefinition method)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            var valueFieldDefinition = enumTypeDefinition.Fields[0];
            var toStringMethodReference = moduleDefinition.ImportReference(systemModuleDefinition.GetType("System", valueFieldDefinition.FieldType.Name).Methods.Single(x => x.IsPublic && !x.IsStatic && !x.HasParameters && x.Name == "ToString"));
            var dictionary = ToDictionary<T>(enumTypeDefinition, valueFieldDefinition, out var minFieldDefinition, out var maxFieldDefinition, out var minValue, out var maxValue);
            switch (dictionary.Count)
            {
                case 0:
                    ProcessCount0(method, toStringMethodReference);
                    break;
                case 1:
                    ProcessCount1(method, valueFieldDefinition, toStringMethodReference, minFieldDefinition, minValue);
                    break;
                case 2:
                    ProcessCount2(method, valueFieldDefinition, toStringMethodReference, minFieldDefinition, maxFieldDefinition, minValue, maxValue);
                    break;
                default:
                    ProcessCountGreaterThan2(method, valueFieldDefinition, toStringMethodReference, new SortedList<T, FieldDefinition>(dictionary).Select(pair => (pair.Value.Name, pair.Key)).ToArray());
                    break;
            }
            return true;
        }

        private static SortedList<ulong, string> InitializeSortedList64<T>(Dictionary<T, FieldDefinition> dictionary)
            where T : unmanaged
        {
            var sortedList = new SortedList<ulong, string>(dictionary.Count);

            foreach (var tuple in dictionary)
            {
                var value = tuple.Key;
                sortedList.Add(*(ulong*)&value, tuple.Value.Name);
            }

            return sortedList;
        }

        public static bool ImplementFlags64<T>(ModuleDefinition systemModuleDefinition, ModuleDefinition moduleDefinition, TypeDefinition enumTypeDefinition, MethodDefinition method)
           where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            bool shouldImplement;
            var valueFieldDefinition = enumTypeDefinition.Fields[0];
            var baseToStringMethodReference = moduleDefinition.ImportReference(systemModuleDefinition.GetType("System", valueFieldDefinition.FieldType.Name).Methods.Single(x => x.IsPublic && !x.IsStatic && !x.HasParameters && x.Name == "ToString"));
            var enumToStringMethodReference = moduleDefinition.ImportReference(systemModuleDefinition.GetType("System", "Enum").Methods.Single(x => x.Name == "ToString" && !x.HasParameters));
            var dictionary = ToDictionary<T>(enumTypeDefinition, valueFieldDefinition, out var minFieldDefinition, out _, out var minValue, out _);
            switch (dictionary.Count)
            {
                case 0:
                    shouldImplement = ProcessCount0(method, baseToStringMethodReference);
                    break;
                case 1:
                    shouldImplement = ProcessCount1(method, valueFieldDefinition, baseToStringMethodReference, minFieldDefinition, minValue);
                    break;
                default:
                    if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
                        shouldImplement = ProcessGreaterThanOrEqualsTo2Flags64(method, valueFieldDefinition, baseToStringMethodReference, enumToStringMethodReference, dictionary);
                    else throw new ArgumentException("Type Mismatch! " + typeof(T).Name);
                    break;
            }
            return shouldImplement;
        }


        private static void ProcessRoutine64(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodReference toStringMethodReference, (string name, ulong value)[] sortedArray)
        {
            var processor = method.Body.GetILProcessor();
            var elseRoutineFirst = Instruction.Create(OpCodes.Ldarg_0);
            var moduleDefinition = method.Module;
            processor.LdArg(0).LdObj(moduleDefinition.ImportReference(valueFieldDefinition.FieldType));
            ref var minValue = ref sortedArray[0].value;
            var actualCount = sortedArray[sortedArray.Length - 1].value - minValue + 1UL;
            if (actualCount == (ulong)sortedArray.Length)
            {
                processor.ProcessContinuous(sortedArray, minValue, elseRoutineFirst);
            }
            else
            {
                processor.ProcessDiscontinuous(method, valueFieldDefinition, sortedArray, ref minValue, elseRoutineFirst);
            }

            processor
                .Add(elseRoutineFirst)
                .Call(toStringMethodReference)
                .Ret();
        }

        private static (string name, ulong value)[] PrepareAllFlags64<T>(SortedList<ulong, string> sortedList, out bool all, string zeroName = null)
            where T : unmanaged
        {
            var dst = new List<(string name, ulong value)>(sortedList.Select(pair => (pair.Value, pair.Key)));
            var sortedListKeys = sortedList.Keys.ToArray();
            var sortedListValues = sortedList.Values.ToArray();
            var loopMax = 128 / sortedListKeys.Length;
            all = sortedListKeys.Length <= loopMax;
            if (all)
            {
                for (int i0 = sortedList.Count, count = sortedList.Count, loopCount = (8 * sizeof(T) < sortedListKeys.Length ? 8 * sizeof(T) : sortedListKeys.Length) - 1; --i0 >= 0;)
                    Loop64(i0, sortedListKeys[i0], sortedListValues[i0], loopCount, count, sortedListKeys, sortedListValues, dst);
                if (zeroName != null)
                    dst.Insert(0, (zeroName, default));
                return dst.ToArray();
            }
            for (int i0 = sortedList.Count, count = sortedList.Count, loopCount = (8 * sizeof(T) < loopMax ? 8 * sizeof(T) : loopMax) - 1; --i0 >= 0;)
                Loop64(i0, sortedListKeys[i0], sortedListValues[i0], loopCount, count, sortedListKeys, sortedListValues, dst);
            if (zeroName != null)
                dst.Insert(0, (zeroName, default));
            return dst.ToArray();
        }

        private sealed class PairComparer<TComparable>
            : IComparer<(string name, TComparable value)>
            where TComparable : unmanaged, IComparable<TComparable>, IEquatable<TComparable>
        {
            public static readonly PairComparer<TComparable> Default = new PairComparer<TComparable>();
            public int Compare((string name, TComparable value) x, (string name, TComparable value) y)
            {
                return x.value.CompareTo(y.value);
            }
        }

        private static void Loop64(int outerIndex, ulong outerValue, string outerName, int loopCount, int count, ulong[] sortedListValues, string[] sortedListNames, List<(string name, ulong value)> dst)
        {
            if (--loopCount < 0) return;
            for (var innerIndex = count - 1; innerIndex > outerIndex; innerIndex--)
            {
                var innerValue = outerValue | sortedListValues[innerIndex];
                if (dst.BinarySearch(("", innerValue), PairComparer<ulong>.Default) >= 0) continue;
                var innerName = outerName + ", " + sortedListNames[innerIndex];

                for (var i = dst.Count; --i >= 0;)
                {
                    if (innerValue < dst[i].value) continue;
                    dst.Insert(i + 1, (innerName, innerValue));
                    break;
                }

                Loop64(innerIndex, innerValue, innerName, loopCount, count, sortedListValues, sortedListNames, dst);
            }
        }

        private static bool ProcessGreaterThanOrEqualsTo2Flags64<T>(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodReference baseToStringMethodReference, MethodReference enumToStringMethodReference, Dictionary<T, FieldDefinition> dictionary)
            where T : unmanaged
        {
            var sortedList = InitializeSortedList64(dictionary);

            (string name, ulong value)[] sortedArray;
            bool all;
            if (sortedList.ContainsKey(0))
            {
                var zeroName = sortedList[0];
                sortedList.RemoveAt(0);
                sortedArray = PrepareAllFlags64<T>(sortedList, out all, zeroName);
            }
            else
            {
                sortedArray = PrepareAllFlags64<T>(sortedList, out all);
            }
            ProcessRoutine64(method, valueFieldDefinition, all ? baseToStringMethodReference : enumToStringMethodReference, sortedArray);
            return true;
        }

        public static bool ImplementFlags32<T>(ModuleDefinition systemModuleDefinition, ModuleDefinition moduleDefinition, TypeDefinition enumTypeDefinition, MethodDefinition method)
            where T : unmanaged, IEquatable<T>, IComparable<T>
        {
            bool shouldImplement;
            var valueFieldDefinition = enumTypeDefinition.Fields[0];
            var dictionary = ToDictionary<T>(enumTypeDefinition, valueFieldDefinition, out var minFieldDefinition, out _, out var minValue, out _);
            var toStringMethodReference = moduleDefinition.ImportReference(systemModuleDefinition.GetType("System", valueFieldDefinition.FieldType.Name).Methods.Single(x => x.IsPublic && !x.IsStatic && !x.HasParameters && x.Name == "ToString"));
            var enumToStringMethodReference = moduleDefinition.ImportReference(systemModuleDefinition.GetType("System", "Enum").Methods.Single(x => x.Name == "ToString" && !x.HasParameters));
            switch (dictionary.Count)
            {
                case 0:
                    shouldImplement = ProcessCount0(method, toStringMethodReference);
                    break;
                case 1:
                    shouldImplement = ProcessCount1(method, valueFieldDefinition, toStringMethodReference, minFieldDefinition, minValue);
                    break;
                default:
                    if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort) || typeof(T) == typeof(int) || typeof(T) == typeof(uint))
                        shouldImplement = ProcessGreaterThanOrEqualsTo2Flags32(method, valueFieldDefinition, toStringMethodReference, enumToStringMethodReference, dictionary);
                    else throw new ArgumentException("Type Mismatch! " + typeof(T).Name);
                    break;
            }
            return shouldImplement;
        }

        private static bool ProcessGreaterThanOrEqualsTo2Flags32<T>(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodReference baseToStringMethodReference, MethodReference enumToStringMethodReference, Dictionary<T, FieldDefinition> dictionary)
            where T : unmanaged
        {
            var sortedList = InitializeSortedList32(dictionary);

            (string name, uint value)[] sortedArray;
            bool all;
            if (sortedList.ContainsKey(0))
            {
                var zeroName = sortedList[0];
                sortedList.RemoveAt(0);
                sortedArray = PrepareAllFlags32<T>(sortedList, out all, zeroName);
            }
            else
            {
                sortedArray = PrepareAllFlags32<T>(sortedList, out all);
            }
            ProcessRoutine32(method, valueFieldDefinition, all ? baseToStringMethodReference : enumToStringMethodReference, sortedArray);
            return true;
        }

        private static void ProcessRoutine32(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodReference toStringMethodReference, (string name, uint value)[] sortedArray)
        {
            var processor = method.Body.GetILProcessor();
            var elseRoutineFirst = Instruction.Create(OpCodes.Ldarg_0);
            var moduleDefinition = method.Module;
            processor.LdArg(0).LdObj(moduleDefinition.ImportReference(valueFieldDefinition.FieldType));
            ref var minValue = ref sortedArray[0].value;
            var actualCount = sortedArray[sortedArray.Length - 1].value - minValue + 1;
            if (actualCount == sortedArray.Length)
            {
                processor.ProcessContinuous(sortedArray, minValue, elseRoutineFirst);
            }
            else
            {
                processor.ProcessDiscontinuous(method, valueFieldDefinition, sortedArray, ref minValue, elseRoutineFirst);
            }

            processor
                .Add(elseRoutineFirst)
                .Call(toStringMethodReference)
                .Ret();
        }

        private static SortedList<uint, string> InitializeSortedList32<T>(Dictionary<T, FieldDefinition> dictionary)
            where T : unmanaged
        {
            var sortedList = new SortedList<uint, string>(dictionary.Count);
            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(byte))
            {
                foreach (var tuple in dictionary)
                {
                    var value = tuple.Key;
                    sortedList.Add(*(byte*)&value, tuple.Value.Name);
                }
            }
            else if (typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
            {
                foreach (var tuple in dictionary)
                {
                    var value = tuple.Key;
                    sortedList.Add(*(ushort*)&value, tuple.Value.Name);
                }
            }
            else
            {
                foreach (var tuple in dictionary)
                {
                    var value = tuple.Key;
                    sortedList.Add(*(uint*)&value, tuple.Value.Name);
                }
            }

            return sortedList;
        }

        private static (string name, uint value)[] PrepareAllFlags32<T>(SortedList<uint, string> sortedList, out bool all, string zeroName = null)
            where T : unmanaged
        {
            var dst = new List<(string name, uint value)>(sortedList.Select(pair => (pair.Value, pair.Key)));
            var sortedListKeys = sortedList.Keys.ToArray();
            var sortedListValues = sortedList.Values.ToArray();
            var loopMax = 128 / sortedListKeys.Length;
            all = sortedListKeys.Length <= loopMax;
            if (all)
            {
                for (int i0 = sortedList.Count, count = sortedList.Count, loopCount = (8 * sizeof(T) < sortedListKeys.Length ? 8 * sizeof(T) : sortedListKeys.Length) - 1; --i0 >= 0;)
                    Loop32(i0, sortedListKeys[i0], sortedListValues[i0], loopCount, count, sortedListKeys, sortedListValues, dst);
                if (zeroName != null)
                    dst.Insert(0, (zeroName, default));
                return dst.ToArray();
            }
            for (int i0 = sortedList.Count, count = sortedList.Count, loopCount = (8 * sizeof(T) < loopMax ? 8 * sizeof(T) : loopMax) - 1; --i0 >= 0;)
                Loop32(i0, sortedListKeys[i0], sortedListValues[i0], loopCount, count, sortedListKeys, sortedListValues, dst);
            if (zeroName != null)
                dst.Insert(0, (zeroName, default));
            return dst.ToArray();
        }

        private static void Loop32(int outerIndex, uint outerValue, string outerName, int loopCount, int count, uint[] sortedListValues, string[] sortedListNames, List<(string name, uint value)> dst)
        {
            if (--loopCount < 0) return;
            for (var innerIndex = count - 1; innerIndex > outerIndex; innerIndex--)
            {
                var innerValue = outerValue | sortedListValues[innerIndex];
                if (dst.BinarySearch(("", innerValue), PairComparer<uint>.Default) >= 0) continue;
                var innerName = outerName + ", " + sortedListNames[innerIndex];

                for (var i = dst.Count; --i >= 0;)
                {
                    if (innerValue < dst[i].value) continue;
                    dst.Insert(i + 1, (innerName, innerValue));
                    break;
                }

                Loop32(innerIndex, innerValue, innerName, loopCount, count, sortedListValues, sortedListNames, dst);
            }
        }


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

        public static MethodDefinition MakeIEquatable(TypeDefinition enumTypeDefinition, ModuleDefinition systemModule)
        {
            var interfaceIEquatable = systemModule.GetType("System", "IEquatable`1");
            var moduleDefinition = enumTypeDefinition.Module;
            enumTypeDefinition.Interfaces.Add(new InterfaceImplementation(new GenericInstanceType(moduleDefinition.ImportReference(interfaceIEquatable)) { GenericArguments = { enumTypeDefinition } }));
            var equals = new MethodDefinition("Equals", MethodAttributes.Final | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig, moduleDefinition.TypeSystem.Boolean)
            {
                Parameters = { new ParameterDefinition(enumTypeDefinition) },
                AggressiveInlining = true,
            };
            var processor = equals.Body.GetILProcessor();
            processor
                .LdArg(0)
                .LdObj(enumTypeDefinition.Fields[0].FieldType)
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

        public static bool ProcessCount0(MethodDefinition methodToString, MethodReference toStringMethodReference)
        {
            methodToString.Body.GetILProcessor()
                .LdArg(0)
                .Call(toStringMethodReference)
                .Ret();
            return true;
        }

        public static bool ProcessCount1<T>(MethodDefinition methodToString, FieldDefinition valueFieldDefinition, MethodReference toStringMethodDefinition, FieldDefinition minFieldDefinition, T minValue) where T : unmanaged, IComparable<T>
        {
            var moduleDefinition = methodToString.Module;
            var processor = methodToString.Body.GetILProcessor();
            var notSame = Instruction.Create(OpCodes.Ldarg_0);
            processor
                .LdArg(0);
            if (NumberHelper.EqualsZero(minValue))
            {
                processor.BrTrueS(notSame);
            }
            else
            {
                processor
                    .LdObj(moduleDefinition.ImportReference(moduleDefinition.ImportReference(valueFieldDefinition.FieldType)))
                    .LdC(minValue)
                    .Add(Instruction.Create(OpCodes.Bne_Un_S, notSame));
            }
            processor
                .LdStr(minFieldDefinition.Name)
                .Ret()
                .Add(notSame)
                .Call(toStringMethodDefinition)
                .Ret();
            return true;
        }

        public static void ProcessCount2<T>(MethodDefinition methodToString, FieldDefinition valueFieldDefinition, MethodReference toStringMethodReference, FieldDefinition minFieldDefinition, FieldDefinition maxFieldDefinition, T minValue, T maxValue)
            where T : unmanaged, IComparable<T>
        {
            var moduleDefinition = methodToString.Module;
            var processor = methodToString.Body.GetILProcessor();
            var notSame = Instruction.Create(OpCodes.Ldarg_0);
            if (NumberHelper.EqualsZero(minValue))
            {
                var shortJump = InstructionUtility.LoadConstantGeneric(maxValue);
                processor
                    .LdArg(0)
                    .LdObj(moduleDefinition.ImportReference(valueFieldDefinition.FieldType))
                    .Dup()
                    .BrTrueS(shortJump[0])
                    .Pop()
                    .LdStr(minFieldDefinition.Name)
                    .Ret()
                    .AddRange(shortJump)
                    .BneS(notSame)
                    .LdStr(maxFieldDefinition.Name)
                    .Ret();
            }
            else if (NumberHelper.EqualsZero(maxValue))
            {
                var shortJump = InstructionUtility.LoadConstantGeneric(minValue);
                processor
                    .LdArg(0)
                    .LdObj(moduleDefinition.ImportReference(valueFieldDefinition.FieldType))
                    .Dup()
                    .BrTrueS(shortJump[0])
                    .Pop()
                    .LdStr(maxFieldDefinition.Name)
                    .Ret()
                    .AddRange(shortJump)
                    .BneS(notSame)
                    .LdStr(minFieldDefinition.Name)
                    .Ret();
            }
            else
            {
                var shortJump = InstructionUtility.LoadConstantGeneric(maxValue);
                processor
                    .LdArg(0)
                    .LdObj(moduleDefinition.ImportReference(valueFieldDefinition.FieldType))
                    .Dup()
                    .LdC(minValue)
                    .BneS(shortJump[0])
                    .Pop()
                    .LdStr(minFieldDefinition.Name)
                    .Ret()
                    .AddRange(shortJump)
                    .BneS(notSame)
                    .LdStr(maxFieldDefinition.Name)
                    .Ret();
            }
            processor
                .Add(notSame)
                .Call(toStringMethodReference)
                .Ret();
        }

        public static void ProcessCountGreaterThan2<T>(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodReference baseToStringMethodReference, (string name, T value)[] sortedArray)
            where T : unmanaged, IComparable<T>, IEquatable<T>
        {
            var processor = method.Body.GetILProcessor();
            var moduleDefinition = method.Module;
            var elseRoutineFirst = Instruction.Create(OpCodes.Ldarg_0);
            processor.LdArg(0).LdObj(moduleDefinition.ImportReference(valueFieldDefinition.FieldType));
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
                .Call(baseToStringMethodReference)
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
            var moduleDefinition = method.Module;
            var variableDefinition = new VariableDefinition(moduleDefinition.ImportReference(valueFieldDefinition.FieldType));
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
