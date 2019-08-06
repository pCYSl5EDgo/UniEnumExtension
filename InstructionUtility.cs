using System;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public static class InstructionUtility
    {
        public static Instruction[] LoadConstantGeneric<T>(T value) where T : unmanaged, IComparable<T>
        {
            switch (value)
            {
                case byte v:
                    return new[] { LoadConstant((sbyte)v) };
                case sbyte v:
                    return new[] { LoadConstant(v) };
                case short v:
                    return new[] { LoadConstant(v) };
                case ushort v:
                    return new[] { LoadConstant(v) };
                case int v:
                    return new[] { LoadConstant(v) };
                case uint v:
                    return new[] { LoadConstant((int)v) };
                case long v:
                    return LoadConstant(v);
                case ulong v:
                    return LoadConstant((long)v);
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }

        public static Instruction LoadConstant(int value)
        {
            switch (value)
            {
                case 0: return Instruction.Create(OpCodes.Ldc_I4_0);
                case 1: return Instruction.Create(OpCodes.Ldc_I4_1);
                case 2: return Instruction.Create(OpCodes.Ldc_I4_2);
                case 3: return Instruction.Create(OpCodes.Ldc_I4_3);
                case 4: return Instruction.Create(OpCodes.Ldc_I4_4);
                case 5: return Instruction.Create(OpCodes.Ldc_I4_5);
                case 6: return Instruction.Create(OpCodes.Ldc_I4_6);
                case 7: return Instruction.Create(OpCodes.Ldc_I4_7);
                case -1: return Instruction.Create(OpCodes.Ldc_I4_M1);
            }
            if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                return Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)value);
            return Instruction.Create(OpCodes.Ldc_I4, value);
        }

        public static Instruction LoadConstant(sbyte value)
        {
            switch (value)
            {
                case 0: return Instruction.Create(OpCodes.Ldc_I4_0);
                case 1: return Instruction.Create(OpCodes.Ldc_I4_1);
                case 2: return Instruction.Create(OpCodes.Ldc_I4_2);
                case 3: return Instruction.Create(OpCodes.Ldc_I4_3);
                case 4: return Instruction.Create(OpCodes.Ldc_I4_4);
                case 5: return Instruction.Create(OpCodes.Ldc_I4_5);
                case 6: return Instruction.Create(OpCodes.Ldc_I4_6);
                case 7: return Instruction.Create(OpCodes.Ldc_I4_7);
                case -1: return Instruction.Create(OpCodes.Ldc_I4_M1);
            }
            return Instruction.Create(OpCodes.Ldc_I4_S, value);
        }

        public static Instruction[] LoadConstant(long value)
        {
            Instruction[] answer;
            switch (value)
            {
                case -1:
                    answer = new[]
                    {
                        Instruction.Create(OpCodes.Ldc_I4_M1),
                        Instruction.Create(OpCodes.Conv_I8)
                    };
                    break;
                case 0:
                    answer = new[]
                    {
                        Instruction.Create(OpCodes.Ldc_I4_0),
                        Instruction.Create(OpCodes.Conv_I8)
                    };
                    break;
                case 1:
                    answer = new[]
                    {
                        Instruction.Create(OpCodes.Ldc_I4_1),
                        Instruction.Create(OpCodes.Conv_I8),
                    };
                    break;
                case 2:
                    answer = new[]
                    {
                        Instruction.Create(OpCodes.Ldc_I4_2),
                        Instruction.Create(OpCodes.Conv_I8),
                    };
                    break;
                case 3:
                    answer = new[]
                    {
                        Instruction.Create(OpCodes.Ldc_I4_3),
                        Instruction.Create(OpCodes.Conv_I8),
                    };
                    break;
                case 4:
                    answer = new[]
                    {
                        Instruction.Create(OpCodes.Ldc_I4_4),
                        Instruction.Create(OpCodes.Conv_I8),
                    };
                    break;
                case 5:
                    answer = new[]
                    {
                        Instruction.Create(OpCodes.Ldc_I4_5),
                        Instruction.Create(OpCodes.Conv_I8),
                    };
                    break;
                case 6:
                    answer = new[]
                    {
                        Instruction.Create(OpCodes.Ldc_I4_6),
                        Instruction.Create(OpCodes.Conv_I8),
                    };
                    break;
                case 7:
                    answer = new[]
                    {
                        Instruction.Create(OpCodes.Ldc_I4_7),
                        Instruction.Create(OpCodes.Conv_I8),
                    };
                    break;
                case 8:
                    answer = new[]
                    {
                        Instruction.Create(OpCodes.Ldc_I4_8),
                        Instruction.Create(OpCodes.Conv_I8),
                    };
                    break;
                default:
                    if (value <= sbyte.MaxValue && value >= sbyte.MinValue)
                    {
                        answer = new[]
                        {
                            Instruction.Create(OpCodes.Ldc_I4_S, (sbyte)value),
                            Instruction.Create(OpCodes.Conv_I8),
                        };
                    }
                    else if (value <= int.MaxValue && value >= int.MinValue)
                    {
                        answer = new[]
                        {
                            Instruction.Create(OpCodes.Ldc_I4, (int)value),
                            Instruction.Create(OpCodes.Conv_I8),
                        };
                    }
                    else
                    {
                        answer = new[]
                        {
                            Instruction.Create(OpCodes.Ldc_I8, value),
                        };
                    }
                    break;
            }
            return answer;
        }

        public static Instruction Bge<T>(Instruction instruction)
        {
            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long))
                return Instruction.Create(OpCodes.Bge, instruction);
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
                return Instruction.Create(OpCodes.Bge_Un, instruction);
            throw new ArgumentException("type mismatch!");
        }

        public static Instruction BgeS<T>(Instruction instruction)
        {
            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long))
                return Instruction.Create(OpCodes.Bge_S, instruction);
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
                return Instruction.Create(OpCodes.Bge_Un_S, instruction);
            throw new ArgumentException("type mismatch!");
        }
        public static Instruction Bgt<T>(Instruction instruction)
        {
            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long))
                return Instruction.Create(OpCodes.Bgt, instruction);
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
                return Instruction.Create(OpCodes.Bgt_Un, instruction);
            throw new ArgumentException("type mismatch!");
        }
        public static Instruction BgtS<T>(Instruction instruction)
        {
            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long))
                return Instruction.Create(OpCodes.Bgt_S, instruction);
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
                return Instruction.Create(OpCodes.Bgt_Un_S, instruction);
            throw new ArgumentException("type mismatch!");
        }
        public static Instruction Ble<T>(Instruction instruction)
        {
            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long))
                return Instruction.Create(OpCodes.Ble, instruction);
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
                return Instruction.Create(OpCodes.Ble_Un, instruction);
            throw new ArgumentException("type mismatch!");
        }
        public static Instruction BleS<T>(Instruction instruction)
        {
            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long))
                return Instruction.Create(OpCodes.Ble_S, instruction);
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
                return Instruction.Create(OpCodes.Ble_Un_S, instruction);
            throw new ArgumentException("type mismatch!");
        }
        public static Instruction Blt<T>(Instruction instruction)
        {
            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long))
                return Instruction.Create(OpCodes.Blt, instruction);
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
                return Instruction.Create(OpCodes.Blt_Un, instruction);
            throw new ArgumentException("type mismatch!");
        }

        public static Instruction BltS<T>(Instruction instruction)
        {
            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(long))
                return Instruction.Create(OpCodes.Blt_S, instruction);
            if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong))
                return Instruction.Create(OpCodes.Blt_Un_S, instruction);
            throw new ArgumentException("type mismatch!");
        }

        public static Instruction[] Switch<T>(Instruction[] instructions)
        {
            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint))
                return new[] { Instruction.Create(OpCodes.Switch, instructions) };
            if (typeof(T) == typeof(long))
                return new[]
                {
                    Instruction.Create(OpCodes.Conv_I4),
                    Instruction.Create(OpCodes.Switch, instructions),
                };
            if (typeof(T) == typeof(ulong))
                return new[]
                {
                    Instruction.Create(OpCodes.Conv_U4),
                    Instruction.Create(OpCodes.Switch, instructions),
                };
            throw new ArgumentException("type mismatch!");
        }

        public static int SwitchCount<T>()
        {
            if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(short) || typeof(T) == typeof(int) || typeof(T) == typeof(byte) || typeof(T) == typeof(ushort) || typeof(T) == typeof(uint))
                return 1;
            if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong))
                return 2;
            throw new ArgumentException("type mismatch!");
        }
    }
}
