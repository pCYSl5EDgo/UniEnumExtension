using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public static class IlUtility
    {
        public static ILProcessor Add(this ILProcessor processor, Instruction instruction)
        {
            processor.Append(instruction);
            return processor;
        }

        public static ILProcessor AddRange(this ILProcessor processor, IEnumerable<Instruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                processor.Append(instruction);
            }
            return processor;
        }

        public static ILProcessor Add(this ILProcessor processor) => processor.Add(Instruction.Create(OpCodes.Add));
        public static ILProcessor Sub(this ILProcessor processor) => processor.Add(Instruction.Create(OpCodes.Sub));

        public static ILProcessor Sub<T>(this ILProcessor processor, T value) where T : unmanaged, IComparable<T>
        {
            switch (value)
            {
                case byte v:
                    return processor.Sub((int)v);
                case sbyte v:
                    return processor.Sub(v);
                case short v:
                    return processor.Sub((int)v);
                case ushort v:
                    return processor.Sub((int)v);
                case int v:
                    return processor.Sub(v);
                case uint v:
                    return processor.Sub((long)v);
                case long v:
                    return processor.Sub(v);
                case ulong v:
                    return processor.Sub((long)v);
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }
        public static ILProcessor Sub(this ILProcessor processor, sbyte value)
        {
            if (value == 0) return processor;
            if (value < 0 && value >= -7)
            {
                return processor.Add(InstructionUtility.LoadConstant(-value)).Add();
            }
            return processor.Add(InstructionUtility.LoadConstant(value)).Sub();
        }
        public static ILProcessor Sub(this ILProcessor processor, int value)
        {
            if (value == 0) return processor;
            if (value < 0 && value >= -7)
            {
                return processor.Add(InstructionUtility.LoadConstant(-value)).Add();
            }
            return processor.Add(InstructionUtility.LoadConstant(value)).Sub();
        }
        public static ILProcessor Sub(this ILProcessor processor, long value)
        {
            if (value == 0L) return processor;
            if (value < 0L && value >= -7L)
            {
                return processor.AddRange(InstructionUtility.LoadConstant(-value)).Add();
            }
            return processor.AddRange(InstructionUtility.LoadConstant(value)).Sub();
        }

        public static ILProcessor Dup(this ILProcessor processor) => processor.Add(Instruction.Create(OpCodes.Dup));
        public static ILProcessor Pop(this ILProcessor processor) => processor.Add(Instruction.Create(OpCodes.Pop));

        public static ILProcessor LdC<T>(this ILProcessor processor, T value) where T : unmanaged
        {
            switch (value)
            {
                case byte v:
                    return processor.Add(InstructionUtility.LoadConstant((sbyte)v));
                case sbyte v:
                    return processor.Add(InstructionUtility.LoadConstant(v));
                case short v:
                    return processor.Add(InstructionUtility.LoadConstant(v));
                case ushort v:
                    return processor.Add(InstructionUtility.LoadConstant(v));
                case int v:
                    return processor.Add(InstructionUtility.LoadConstant(v));
                case uint v:
                    return processor.Add(InstructionUtility.LoadConstant((int)v));
                case long v:
                    return processor.AddRange(InstructionUtility.LoadConstant(v));
                case ulong v:
                    return processor.AddRange(InstructionUtility.LoadConstant((long)v));
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }

        public static ILProcessor LdFldA(this ILProcessor processor, FieldReference fieldReference) => processor.Add(Instruction.Create(OpCodes.Ldflda, fieldReference));
        public static ILProcessor LdFld(this ILProcessor processor, FieldReference fieldReference) => processor.Add(Instruction.Create(OpCodes.Ldfld, fieldReference));
        public static ILProcessor LdLocA(this ILProcessor processor, int index) => processor.Add(Instruction.Create(index <= 255 ? OpCodes.Ldloca_S : OpCodes.Ldloca, processor.Body.Variables[index]));
        public static ILProcessor LdLoc(this ILProcessor processor, int index)
        {
            switch (index)
            {
                case 0:
                    return processor.Add(Instruction.Create(OpCodes.Ldloc_0));
                case 1:
                    return processor.Add(Instruction.Create(OpCodes.Ldloc_1));
                case 2:
                    return processor.Add(Instruction.Create(OpCodes.Ldloc_2));
                case 3:
                    return processor.Add(Instruction.Create(OpCodes.Ldloc_3));
            }
            return processor.Add(Instruction.Create(index <= 255 ? OpCodes.Ldloc_S : OpCodes.Ldloc, processor.Body.Variables[index]));
        }
        public static ILProcessor LdStr(this ILProcessor processor, string value) => processor.Add(Instruction.Create(OpCodes.Ldstr, value));

        public static ILProcessor LdArg(this ILProcessor processor, int index)
        {
            switch (index)
            {
                case 0:
                    return processor.Add(Instruction.Create(OpCodes.Ldarg_0));
                case 1:
                    return processor.Add(Instruction.Create(OpCodes.Ldarg_1));
                case 2:
                    return processor.Add(Instruction.Create(OpCodes.Ldarg_2));
                case 3:
                    return processor.Add(Instruction.Create(OpCodes.Ldarg_3));
                default:
                    return processor.Add(Instruction.Create(index <= sbyte.MaxValue ? OpCodes.Ldarg_S : OpCodes.Ldarg, processor.Body.Method.Parameters[index - 1]));
            }
        }

        public static ILProcessor StLoc(this ILProcessor processor, int index)
        {
            switch (index)
            {
                case 0:
                    return processor.Add(Instruction.Create(OpCodes.Stloc_0));
                case 1:
                    return processor.Add(Instruction.Create(OpCodes.Stloc_1));
                case 2:
                    return processor.Add(Instruction.Create(OpCodes.Stloc_2));
                case 3:
                    return processor.Add(Instruction.Create(OpCodes.Stloc_3));
            }
            if (index <= byte.MaxValue)
                return processor.Add(Instruction.Create(OpCodes.Stloc_S, (byte)index));
            return processor.Add(Instruction.Create(OpCodes.Stloc, index));
        }

        public static ILProcessor Switch(this ILProcessor processor, Instruction[] instructions) => processor.Add(Instruction.Create(OpCodes.Switch, instructions));
        public static ILProcessor Switch<T>(this ILProcessor processor, Instruction[] instructions) => processor.AddRange(InstructionUtility.Switch<T>(instructions));
        public static ILProcessor Br(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Br, instruction));
        public static ILProcessor BrS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Br_S, instruction));
        public static ILProcessor BrTrue(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Brtrue, instruction));
        public static ILProcessor BrTrueS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Brtrue_S, instruction));
        public static ILProcessor BrFalse(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Brfalse, instruction));
        public static ILProcessor BrFalseS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Brfalse_S, instruction));
        public static ILProcessor Beq(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Beq, instruction));
        public static ILProcessor BeqS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Beq_S, instruction));
        public static ILProcessor Bne(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Bne_Un, instruction));
        public static ILProcessor BneS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Bne_Un_S, instruction));
        public static ILProcessor Bge(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Bge, instruction));
        public static ILProcessor BgeS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Bge_S, instruction));
        public static ILProcessor Bge<T>(this ILProcessor processor, Instruction instruction) => processor.Add(InstructionUtility.Bge<T>(instruction));
        public static ILProcessor BgeS<T>(this ILProcessor processor, Instruction instruction) => processor.Add(InstructionUtility.BgeS<T>(instruction));
        public static ILProcessor BgeUn(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Bge_Un, instruction));
        public static ILProcessor BgeUnS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Bge_Un_S, instruction));
        public static ILProcessor Bgt(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Bgt, instruction));
        public static ILProcessor BgtS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Bgt_S, instruction));
        public static ILProcessor Bgt<T>(this ILProcessor processor, Instruction instruction) => processor.Add(InstructionUtility.Bgt<T>(instruction));
        public static ILProcessor BgtS<T>(this ILProcessor processor, Instruction instruction) => processor.Add(InstructionUtility.BgtS<T>(instruction));
        public static ILProcessor BgtUn(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Bgt_Un, instruction));
        public static ILProcessor BgtUnS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Bgt_Un_S, instruction));
        public static ILProcessor Ble(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Ble, instruction));
        public static ILProcessor BleS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Ble_S, instruction));
        public static ILProcessor Ble<T>(this ILProcessor processor, Instruction instruction) => processor.Add(InstructionUtility.Ble<T>(instruction));
        public static ILProcessor BleS<T>(this ILProcessor processor, Instruction instruction) => processor.Add(InstructionUtility.BleS<T>(instruction));
        public static ILProcessor BleUn(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Ble_Un, instruction));
        public static ILProcessor BleUnS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Ble_Un_S, instruction));
        public static ILProcessor Blt(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Blt, instruction));
        public static ILProcessor BltS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Blt_S, instruction));
        public static ILProcessor Blt<T>(this ILProcessor processor, Instruction instruction) => processor.Add(InstructionUtility.Blt<T>(instruction));
        public static ILProcessor BltS<T>(this ILProcessor processor, Instruction instruction) => processor.Add(InstructionUtility.BltS<T>(instruction));
        public static ILProcessor BltUn(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Blt_Un, instruction));
        public static ILProcessor BltUnS(this ILProcessor processor, Instruction instruction) => processor.Add(Instruction.Create(OpCodes.Blt_Un_S, instruction));

        public static ILProcessor Call(this ILProcessor processor, MethodReference methodReference) => processor.Add(Instruction.Create(OpCodes.Call, methodReference));
        public static ILProcessor CallVirtual(this ILProcessor processor, MethodReference methodReference) => processor.Add(Instruction.Create(OpCodes.Callvirt, methodReference));
        public static ILProcessor CallVirtual(this ILProcessor processor, MethodReference methodReference, TypeReference constrained)
            => processor.Add(Instruction.Create(OpCodes.Constrained, constrained)).Add(Instruction.Create(OpCodes.Callvirt, methodReference));

        public static ILProcessor Ret(this ILProcessor processor) => processor.Add(Instruction.Create(OpCodes.Ret));
    }
}
