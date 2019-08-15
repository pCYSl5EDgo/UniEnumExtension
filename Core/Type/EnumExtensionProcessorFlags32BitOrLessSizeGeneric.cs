using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed unsafe class EnumExtensionProcessorFlags32BitOrLessSizeGeneric<T>
        : ITypeProcessor
        where T : unmanaged, IComparable<T>, IEquatable<T>
    {
        private static readonly string FullName;

        static EnumExtensionProcessorFlags32BitOrLessSizeGeneric()
        {
            FullName = typeof(T).FullName;
        }

        public void ProcessRewriteToString(ModuleDefinition systemModuleDefinition, TypeDefinition enumTypeDefinition, FieldDefinition valueFieldDefinition)
        {
            var dictionary = EnumExtensionUtility.ToDictionary<T>(enumTypeDefinition, valueFieldDefinition, out var minFieldDefinition, out _, out var minValue, out _);
            var method = EnumExtensionUtility.MakeToString(enumTypeDefinition);
            var baseToStringMethodDefinition = systemModuleDefinition.GetType("System", valueFieldDefinition.FieldType.Name).Methods.Single(x => x.IsPublic && !x.IsStatic && !x.HasParameters && x.Name == "ToString");
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
                    if (typeof(T) == typeof(byte) || typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort) || typeof(T) == typeof(int) || typeof(T) == typeof(uint))
                        shouldImplement = ProcessGreaterThanOrEqualsTo2(method, valueFieldDefinition, baseToStringMethodDefinition, dictionary);
                    else throw new ArgumentException("Type Mismatch! " + typeof(T).Name);
                    break;
            }
            if (shouldImplement)
            {
                enumTypeDefinition.Methods.Add(method);
            }
        }

        public void ProcessAddIEquatable(ModuleDefinition systemModuleDefinition, TypeDefinition enumTypeDefinition)
        {
            enumTypeDefinition.Methods.Add(EnumExtensionUtility.MakeIEquatable(enumTypeDefinition, systemModuleDefinition));
        }

        private bool ProcessGreaterThanOrEqualsTo2(MethodDefinition method, FieldDefinition valueFieldDefinition, MethodDefinition baseToStringMethodDefinition, Dictionary<T, FieldDefinition> dictionary)
        {
            SortedList<uint, string> sortedList = InitializeSortedList(dictionary);

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
            processor.LdArg(0).LdObj(valueFieldDefinition.FieldType);
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
                .Call(valueFieldDefinition.Module.ImportReference(baseToStringMethodDefinition))
                .Ret();
        }

        private static SortedList<uint, string> InitializeSortedList(Dictionary<T, FieldDefinition> dictionary)
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

        public byte Stage => (byte)(65 + sizeof(T));
        public void Process(ModuleDefinition systemModuleDefinition, TypeDefinition typeDefinition)
        {
            if (!typeDefinition.IsEnum)
            {
                return;
            }
            if (!typeDefinition.HasCustomAttributes || typeDefinition.CustomAttributes.All(customAttribute => customAttribute.AttributeType.FullName != "System.FlagsAttribute"))
            {
                return;
            }
            var valueFieldDefinition = typeDefinition.Fields[0];
            if (valueFieldDefinition.FieldType.FullName != FullName)
            {
                return;
            }
            ProcessRewriteToString(systemModuleDefinition, typeDefinition, valueFieldDefinition);
            ProcessAddIEquatable(systemModuleDefinition, typeDefinition);
        }
    }
}
