﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed unsafe class EnumExtensionProcessorFlagsGeneric<T>
        : IEnumExtensionProcessor<T>
        where T : unmanaged, IComparable<T>, IEquatable<T>
    {
        private readonly Dictionary<string, MethodDefinition> typeToStringDictionary;

        public EnumExtensionProcessorFlagsGeneric(Dictionary<string, MethodDefinition> typeToStringDictionary)
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
            var dictionary = EnumExtensionUtility.ToDictionary<T>(enumTypeDefinition, valueFieldDefinition, out var minFieldDefinition, out _, out var minValue, out _);
            var method = EnumExtensionUtility.MakeToString(enumTypeDefinition);
            var baseToStringMethodDefinition = typeToStringDictionary[valueFieldDefinition.FieldType.Name];
            bool shouldImplement;
            switch (dictionary.Count)
            {
                case 0:
                    shouldImplement = EnumExtensionUtility.ProcessCount0(method, valueFieldDefinition, baseToStringMethodDefinition);
                    break;
                case 1:
                    shouldImplement = EnumExtensionUtility.ProcessCount1(method, valueFieldDefinition, baseToStringMethodDefinition, minFieldDefinition, minValue);
                    break;
                default:
                    if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
                        shouldImplement = ProcessGreaterThanOrEqualsTo2_UInt64(method, valueFieldDefinition, baseToStringMethodDefinition, dictionary);
                    else if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort) || typeof(T) == typeof(int) || typeof(T) == typeof(uint))
                        shouldImplement = ProcessGreaterThanOrEqualsTo2_UInt32(method, valueFieldDefinition, baseToStringMethodDefinition, dictionary);
                    else throw new ArgumentException("Type Mismatch! " + typeof(T).Name);
                    break;
            }
            if (shouldImplement)
            {
                enumTypeDefinition.Methods.Add(method);
            }
        }

        private bool ProcessGreaterThanOrEqualsTo2_UInt32(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodDefinition baseToStringMethodDefinition, Dictionary<T, FieldDefinition> dictionary)
        {
            SortedList<uint, string> sortedList = InitializeSortedListUInt32(dictionary);

            (string name, uint value)[] sortedArray;
            if (sortedList.ContainsKey(0))
            {
                var zeroName = sortedList[0];
                sortedList.RemoveAt(0);
                sortedArray = PrepareAllFlags(sortedList, zeroName);
            }
            else
            {
                sortedArray = PrepareAllFlags(sortedList);
            }
            ProcessRoutine(method, valueFieldDefinition, baseToStringMethodDefinition, sortedArray);
            return true;
        }

        private static void ProcessRoutine(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodDefinition baseToStringMethodDefinition, (string name, uint value)[] sortedArray)
        {
            var processor = method.Body.GetILProcessor();
            var elseRoutineFirst = Instruction.Create(OpCodes.Ldarg_0);
            processor.LdArg(0).LdFld(valueFieldDefinition);
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
                .LdFldA(valueFieldDefinition)
                .Call(valueFieldDefinition.Module.ImportReference(baseToStringMethodDefinition))
                .Ret();
        }

        private static void ProcessRoutine(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodDefinition baseToStringMethodDefinition, (string name, ulong value)[] sortedArray)
        {
            var processor = method.Body.GetILProcessor();
            var elseRoutineFirst = Instruction.Create(OpCodes.Ldarg_0);
            processor.LdArg(0).LdFld(valueFieldDefinition);
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
                .LdFldA(valueFieldDefinition)
                .Call(valueFieldDefinition.Module.ImportReference(baseToStringMethodDefinition))
                .Ret();
        }

        private static SortedList<uint, string> InitializeSortedListUInt32(Dictionary<T, FieldDefinition> dictionary)
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
        private static SortedList<ulong, string> InitializeSortedListUInt64(Dictionary<T, FieldDefinition> dictionary)
        {
            var sortedList = new SortedList<ulong, string>(dictionary.Count);

            foreach (var tuple in dictionary)
            {
                var value = tuple.Key;
                sortedList.Add(*(ulong*)&value, tuple.Value.Name);
            }

            return sortedList;
        }

        private static (string name, uint value)[] PrepareAllFlags(SortedList<uint, string> sortedList, string zeroName = null)
        {
            var dst = new List<(string name, uint value)>(sortedList.Select(pair => (pair.Value, pair.Key)));
            var sortedListKeys = sortedList.Keys.ToArray();
            var sortedListValues = sortedList.Values.ToArray();
            for (int i0 = sortedList.Count, count = sortedList.Count, loopCount = (8 * sizeof(T) < sortedListKeys.Length ? 8 * sizeof(T) : sortedListKeys.Length) - 1; --i0 >= 0;)
                Loop(i0, sortedListKeys[i0], sortedListValues[i0], loopCount, count, sortedListKeys, sortedListValues, dst);
            if (zeroName != null)
                dst.Insert(0, (zeroName, default));
            return dst.ToArray();
        }

        private static (string name, ulong value)[] PrepareAllFlags(SortedList<ulong, string> sortedList, string zeroName = null)
        {
            var dst = new List<(string name, ulong value)>(sortedList.Select(pair => (pair.Value, pair.Key)));
            var sortedListKeys = sortedList.Keys.ToArray();
            var sortedListValues = sortedList.Values.ToArray();
            for (int i0 = sortedList.Count, count = sortedList.Count, loopCount = (8 * sizeof(T) < sortedListKeys.Length ? 8 * sizeof(T) : sortedListKeys.Length) - 1; --i0 >= 0;)
                Loop(i0, sortedListKeys[i0], sortedListValues[i0], loopCount, count, sortedListKeys, sortedListValues, dst);
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

        private static void Loop(int outerIndex, uint outerValue, string outerName, int loopCount, int count, uint[] sortedListValues, string[] sortedListNames, List<(string name, uint value)> dst)
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

                Loop(innerIndex, innerValue, innerName, loopCount, count, sortedListValues, sortedListNames, dst);
            }
        }

        private static void Loop(int outerIndex, ulong outerValue, string outerName, int loopCount, int count, ulong[] sortedListValues, string[] sortedListNames, List<(string name, ulong value)> dst)
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

                Loop(innerIndex, innerValue, innerName, loopCount, count, sortedListValues, sortedListNames, dst);
            }
        }


        private bool ProcessGreaterThanOrEqualsTo2_UInt64(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodDefinition baseToStringMethodDefinition, Dictionary<T, FieldDefinition> dictionary)
        {
            var sortedList = InitializeSortedListUInt64(dictionary);

            (string name, ulong value)[] sortedArray;
            if (sortedList.ContainsKey(0))
            {
                var zeroName = sortedList[0];
                sortedList.RemoveAt(0);
                sortedArray = PrepareAllFlags(sortedList, zeroName);
            }
            else
            {
                sortedArray = PrepareAllFlags(sortedList);
            }
            ProcessRoutine(method, valueFieldDefinition, baseToStringMethodDefinition, sortedArray);
            return true;
        }
    }
}
