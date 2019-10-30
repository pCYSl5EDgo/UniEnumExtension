using System;

namespace UniEnumExtension
{
    public static unsafe class NumberHelper
    {
        public static bool IsInt32OnStack(this string name)
        {
            switch (name)
            {
                default:
                    return false;
                case "Byte":
                case "SByte":
                case "Int16":
                case "UInt16":
                case "Int32":
                case "UInt32":
                    return true;
            }
        }

        public static bool IsInt64OnStack(this string name)
        {
            switch (name)
            {
                default:
                    return false;
                case "Int64":
                case "UInt64":
                    return true;
            }
        }

        public static bool EqualsZero<T>(T value)
            where T : unmanaged
        {
            if (typeof(T) == typeof(byte))
            {
                return *(byte*)&value == 0;
            }
            if (typeof(T) == typeof(sbyte))
            {
                return *(sbyte*)&value == 0;
            }
            if (typeof(T) == typeof(short))
            {
                return *(short*)&value == 0;
            }
            if (typeof(T) == typeof(ushort))
            {
                return *(ushort*)&value == 0;
            }
            if (typeof(T) == typeof(int))
            {
                return *(int*)&value == 0;
            }
            if (typeof(T) == typeof(uint))
            {
                return *(uint*)&value == 0;
            }
            if (typeof(T) == typeof(long))
            {
                return *(long*)&value == 0;
            }
            if (typeof(T) == typeof(ulong))
            {
                return *(ulong*)&value == 0;
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }

        public static bool IsDifferenceLessThanOrEqualsTo<T>(T min, T max, long value, out long actualCount)
            where T : unmanaged
        {
            if (typeof(T) == typeof(byte))
            {
                return (actualCount = *(byte*)&max - *(byte*)&min + 1) <= value;
            }
            if (typeof(T) == typeof(sbyte))
            {
                return (actualCount = *(sbyte*)&max - *(sbyte*)&min + 1) <= value;
            }
            if (typeof(T) == typeof(short))
            {
                return (actualCount = *(short*)&max - *(short*)&min + 1) <= value;
            }
            if (typeof(T) == typeof(ushort))
            {
                return (actualCount = *(ushort*)&max - *(ushort*)&min + 1) <= value;
            }
            if (typeof(T) == typeof(int))
            {
                return (actualCount = *(int*)&max - *(int*)&min + 1) <= value;
            }
            if (typeof(T) == typeof(uint))
            {
                return (actualCount = *(uint*)&max - *(uint*)&min + 1) <= value;
            }
            if (typeof(T) == typeof(long))
            {
                actualCount = *(long*)&max - *(long*)&min + 1;
                return actualCount >= 1L && actualCount <= value;
            }
            if (typeof(T) == typeof(ulong))
            {
                actualCount = (long)(*(ulong*)&max - *(ulong*)&min + 1);
                return actualCount >= 1L && actualCount <= value;
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }

        public static bool IsDifferenceOne<T>(T min, T max)
            where T : unmanaged
        {
            if (typeof(T) == typeof(byte))
            {
                return *(byte*)&max - *(byte*)&min == 1;
            }
            if (typeof(T) == typeof(sbyte))
            {
                return *(sbyte*)&max - *(sbyte*)&min == 1;
            }
            if (typeof(T) == typeof(short))
            {
                return *(short*)&max - *(short*)&min == 1;
            }
            if (typeof(T) == typeof(ushort))
            {
                return *(ushort*)&max - *(ushort*)&min == 1;
            }
            if (typeof(T) == typeof(int))
            {
                return *(int*)&max - *(int*)&min == 1;
            }
            if (typeof(T) == typeof(uint))
            {
                return *(uint*)&max - *(uint*)&min == 1;
            }
            if (typeof(T) == typeof(long))
            {
                return *(long*)&max - *(long*)&min + 1 == 1;
            }
            if (typeof(T) == typeof(ulong))
            {
                return *(ulong*)&max - *(ulong*)&min == 1;
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }

        public static bool DifferenceEqualsTo<T>(T min, T max, int difference)
            where T : unmanaged
        {
            if (typeof(T) == typeof(byte))
            {
                return *(byte*)&max - *(byte*)&min == difference;
            }
            if (typeof(T) == typeof(sbyte))
            {
                return *(sbyte*)&max - *(sbyte*)&min == difference;
            }
            if (typeof(T) == typeof(short))
            {
                return *(short*)&max - *(short*)&min == difference;
            }
            if (typeof(T) == typeof(ushort))
            {
                return *(ushort*)&max - *(ushort*)&min == difference;
            }
            if (typeof(T) == typeof(int))
            {
                return *(int*)&max - *(int*)&min == difference;
            }
            if (typeof(T) == typeof(uint))
            {
                return *(uint*)&max - *(uint*)&min == difference;
            }
            if (typeof(T) == typeof(long))
            {
                return *(long*)&max - *(long*)&min == difference;
            }
            if (typeof(T) == typeof(ulong))
            {
                return *(ulong*)&max - *(ulong*)&min == (ulong)difference;
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }

        public static T Increment<T>(T value)
            where T : unmanaged
        {
            if (typeof(T) == typeof(byte))
            {
                var v = (byte)(*(byte*)&value + 1);
                return *(T*)&v;
            }
            if (typeof(T) == typeof(sbyte))
            {
                var v = (sbyte)(*(sbyte*)&value + 1);
                return *(T*)&v;
            }
            if (typeof(T) == typeof(short))
            {
                var v = (short)(*(short*)&value + 1);
                return *(T*)&v;
            }
            if (typeof(T) == typeof(ushort))
            {
                var v = (ushort)(*(ushort*)&value + 1);
                return *(T*)&v;
            }
            if (typeof(T) == typeof(int))
            {
                var v = *(int*)&value + 1;
                return *(T*)&v;
            }
            if (typeof(T) == typeof(uint))
            {
                var v = *(uint*)&value + 1u;
                return *(T*)&v;
            }
            if (typeof(T) == typeof(long))
            {
                var v = *(long*)&value + 1L;
                return *(T*)&v;
            }
            if (typeof(T) == typeof(ulong))
            {
                var v = *(ulong*)&value + 1UL;
                return *(T*)&v;
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }

        public static T Decrement<T>(T value)
            where T : unmanaged
        {
            if (typeof(T) == typeof(byte))
            {
                var v = (byte)(*(byte*)&value - 1);
                return *(T*)&v;
            }
            if (typeof(T) == typeof(sbyte))
            {
                var v = (sbyte)(*(sbyte*)&value - 1);
                return *(T*)&v;
            }
            if (typeof(T) == typeof(short))
            {
                var v = (short)(*(short*)&value - 1);
                return *(T*)&v;
            }
            if (typeof(T) == typeof(ushort))
            {
                var v = (ushort)(*(ushort*)&value - 1);
                return *(T*)&v;
            }
            if (typeof(T) == typeof(int))
            {
                var v = *(int*)&value - 1;
                return *(T*)&v;
            }
            if (typeof(T) == typeof(uint))
            {
                var v = *(uint*)&value - 1u;
                return *(T*)&v;
            }
            if (typeof(T) == typeof(long))
            {
                var v = *(long*)&value - 1L;
                return *(T*)&v;
            }
            if (typeof(T) == typeof(ulong))
            {
                var v = *(ulong*)&value - 1UL;
                return *(T*)&v;
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }
    }
}