using System;
using System.Runtime.CompilerServices;

namespace UniEnumExtension
{
    public static class NumberHelper
    {
        public static bool EqualsZero<T>(T value)
            where T : unmanaged
        {
            switch (value)
            {
                case byte v:
                    return v == 0;
                case sbyte v:
                    return v == 0;
                case short v:
                    return v == 0;
                case ushort v:
                    return v == 0;
                case int v:
                    return v == 0;
                case uint v:
                    return v == 0;
                case long v:
                    return v == 0;
                case ulong v:
                    return v == 0;
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }

        public static bool IsDifferenceLessThanOrEqualsTo<T>(T min, T max, long value, out long actualCount)
        {
            switch (min)
            {
                case byte v0:
                    if (!(max is byte v1)) goto default;
                    return (actualCount = v1 - v0 + 1) <= value;
                case sbyte v0:
                    if (!(max is sbyte v2)) goto default;
                    return (actualCount = v2 - v0 + 1) <= value;
                case short v0:
                    if (!(max is short v3)) goto default;
                    return (actualCount = v3 - v0 + 1) <= value;
                case ushort v0:
                    if (!(max is ushort v4)) goto default;
                    return (actualCount = v4 - v0 + 1) <= value;
                case int v0:
                    if (!(max is int v5)) goto default;
                    return (actualCount = v5 - v0 + 1) <= value;
                case uint v0:
                    if (!(max is uint v6)) goto default;
                    return (actualCount = v6 - v0 + 1) <= value;
                case long v0:
                    if (!(max is long v7)) goto default;
                    try
                    {
                        checked
                        {
                            return (actualCount = v7 - v0 + 1) <= value;
                        }
                    }
                    catch
                    {
                        actualCount = 0;
                        return false;
                    }
                case ulong v0:
                    if (!(max is ulong v8)) goto default;
                    try
                    {
                        checked
                        {
                            return (actualCount = (long)(v8 - v0 + 1)) <= value;
                        }
                    }
                    catch
                    {
                        actualCount = 0;
                        return false;
                    }
                default:
                    throw new ArgumentException("Type mismatch!" + typeof(T).Name);
            }
        }

        public static bool IsDifferenceOne<T>(T min, T max)
            where T : unmanaged
        {
            switch (min)
            {
                case byte v0:
                    if (!(max is byte v1)) goto default;
                    return v1 - v0 == 1;
                case sbyte v0:
                    if (!(max is sbyte v2)) goto default;
                    return v2 - v0 == 1;
                case short v0:
                    if (!(max is short v3)) goto default;
                    return v3 - v0 == 1;
                case ushort v0:
                    if (!(max is ushort v4)) goto default;
                    return v4 - v0 == 1;
                case int v0:
                    if (!(max is int v5)) goto default;
                    return v5 - v0 == 1;
                case uint v0:
                    if (!(max is uint v6)) goto default;
                    return v6 - v0 == 1;
                case long v0:
                    if (!(max is long v7)) goto default;
                    return v7 - v0 == 1;
                case ulong v0:
                    if (!(max is ulong v8)) goto default;
                    return v8 - v0 == 1;
                default:
                    throw new ArgumentException("Type mismatch!" + typeof(T).Name);
            }
        }

        public static T Increment<T>(T value)
            where T : unmanaged
        {
            switch (value)
            {
                case byte v:
                    return (T)(object)(byte)(v + 1);
                case sbyte v:
                    return (T)(object)(sbyte)(v + 1);
                case short v:
                    return (T)(object)(short)(v + 1);
                case ushort v:
                    return (T)(object)(ushort)(v + 1);
                case int v:
                    return (T)(object)(v + 1);
                case uint v:
                    return (T)(object)(uint)(v + 1);
                case long v:
                    return (T)(object)(v + 1);
                case ulong v:
                    return (T)(object)(ulong)(v + 1);
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }

        public static T Decrement<T>(T value)
            where T : unmanaged
        {
            switch (value)
            {
                case byte v:
                    return (T)(object)(byte)(v - 1);
                case sbyte v:
                    return (T)(object)(sbyte)(v - 1);
                case short v:
                    return (T)(object)(short)(v - 1);
                case ushort v:
                    return (T)(object)(ushort)(v - 1);
                case int v:
                    return (T)(object)(v - 1);
                case uint v:
                    return (T)(object)(uint)(v - 1);
                case long v:
                    return (T)(object)(v - 1);
                case ulong v:
                    return (T)(object)(ulong)(v - 1);
            }
            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
        }

        public static bool Sub<T>(T min, T max, out T result)
            where T : unmanaged
        {
            try
            {
                checked
                {
                    switch (min)
                    {
                        case byte v0:
                            if (!(max is byte v1)) goto default;
                            result = (T)(object)(byte)(v1 - v0);
                            return true;
                        case sbyte v0:
                            if (!(max is sbyte v2)) goto default;
                            result = (T)(object)(sbyte)(v2 - v0);
                            return true;
                        case short v0:
                            if (!(max is short v3)) goto default;
                            result = (T)(object)(short)(v3 - v0);
                            return true;
                        case ushort v0:
                            if (!(max is ushort v4)) goto default;
                            result = (T)(object)(ushort)(v4 - v0);
                            return true;
                        case int v0:
                            if (!(max is int v5)) goto default;
                            result = (T)(object)(v5 - v0);
                            return true;
                        case uint v0:
                            if (!(max is uint v6)) goto default;
                            result = (T)(object)(v6 - v0);
                            return true;
                        case long v0:
                            if (!(max is long v7)) goto default;
                            result = (T)(object)(v7 - v0);
                            return true;
                        case ulong v0:
                            if (!(max is ulong v8)) goto default;
                            result = (T)(object)(v8 - v0);
                            return true;
                        default:
                            throw new ArgumentException("Type mismatch!" + typeof(T).Name);
                    }
                }
            }
            catch (OverflowException)
            {
                result = default;
                return false;
            }
        }
    }
}