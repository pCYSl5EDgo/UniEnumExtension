using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public static class ArrayInitializerUtility
    {
        private const string Prefix = "pcysl5edgo_";
        public const string PrivateImplementationTypeName = "<PrivateImplementationDetails>";
        public static TypeDefinition GetOrCreatePrivateImplementationDetails(this ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition)
        {
            var answer = moduleDefinition.GetType(PrivateImplementationTypeName);
            if (!(answer is null)) return answer;
            answer = new TypeDefinition("", PrivateImplementationTypeName, TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed, moduleDefinition.TypeSystem.Object);
            var compilerGeneratedAttributeTypeDefinition = systemModuleDefinition.GetType("System.Runtime.CompilerServices", "CompilerGeneratedAttribute");
            answer.CustomAttributes.Add(new CustomAttribute(moduleDefinition.ImportReference(compilerGeneratedAttributeTypeDefinition.Methods.Single())));
            moduleDefinition.Types.Add(answer);
            return answer;
        }

        public static TypeDefinition GetOrCreateStaticArrayInitType(this ModuleDefinition moduleDefinition, TypeDefinition privateImplementationDetails, ModuleDefinition systemModuleDefinition, int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(size.ToString());
            var answer = privateImplementationDetails.NestedTypes.FirstOrDefault(x => x.IsValueType && x.IsExplicitLayout && x.IsSealed && x.PackingSize == 1 && x.ClassSize == size);
            if (!(answer is null))
            {
                return answer;
            }
            answer = new TypeDefinition("", "__StaticArrayInitTypeSize=" + size, TypeAttributes.NestedPublic | TypeAttributes.ExplicitLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed, moduleDefinition.ImportReference(systemModuleDefinition.GetType("System", "ValueType")))
            {
                PackingSize = 1,
                ClassSize = size,
            };
            privateImplementationDetails.NestedTypes.Add(answer);
            return answer;
        }

        public static int CalculateEnumSize(this TypeDefinition enumTypeDefinition)
        {
            switch (enumTypeDefinition.Fields[0].FieldType.Name)
            {
                case "Byte":
                case "SByte":
                    return 1;
                case "Int16":
                case "UInt16":
                    return 2;
                case "Int32":
                case "UInt32":
                    return 4;
                case "Int64":
                case "UInt64":
                    return 8;
                default:
                    throw new ArgumentException("Type mismatch");
            }
        }

        public static int CalculateEnumCount(this TypeDefinition enumTypeDefinition) => enumTypeDefinition.Fields.Count - 1;

        public static FieldDefinition GetOrCreateEnumValues(this ModuleDefinition moduleDefinition, TypeDefinition privateImplementationDetails, ModuleDefinition systemModuleDefinition, TypeDefinition enumTypeDefinition)
        {
            var count = enumTypeDefinition.CalculateEnumCount();
            var elementSize = enumTypeDefinition.CalculateEnumSize();
            var size = elementSize * count;
            var type = moduleDefinition.GetOrCreateStaticArrayInitType(privateImplementationDetails, systemModuleDefinition, size);
            var name = Prefix + "EnumType_Initialize_Array_" + enumTypeDefinition.FullName;

            var answer = privateImplementationDetails.Fields.FirstOrDefault(x => x.Name == name);
            if (!(answer is null)) return answer;
            answer = new FieldDefinition(name, FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRVA, type)
            {
                InitialValue = CalculateInitialValues(enumTypeDefinition, size),
            };
            privateImplementationDetails.Fields.Add(answer);
            return answer;
        }

        public static unsafe byte[] CalculateInitialValues(TypeDefinition enumTypeDefinition, int size)
        {
            var answer = new byte[size];
            var fields = enumTypeDefinition.Fields.Where(x => x.IsLiteral && x.IsStatic);
            fixed (byte* ptr = &answer[0])
            {
                switch (enumTypeDefinition.Fields[0].FieldType.Name)
                {
                    case "Byte":
                        {
                            var destination = ptr;
                            foreach (var fieldDefinition in fields)
                            {
                                *destination++ = (byte)fieldDefinition.Constant;
                            }
                        }
                        break;
                    case "SByte":
                        {
                            var destination = (sbyte*)ptr;
                            foreach (var fieldDefinition in fields)
                            {
                                *destination++ = (sbyte)fieldDefinition.Constant;
                            }
                        }
                        break;
                    case "Int16":
                        {
                            var destination = (short*)ptr;
                            foreach (var fieldDefinition in fields)
                            {
                                *destination++ = (short)fieldDefinition.Constant;
                            }
                        }
                        break;
                    case "UInt16":
                        {
                            var destination = (ushort*)ptr;
                            foreach (var fieldDefinition in fields)
                            {
                                *destination++ = (ushort)fieldDefinition.Constant;
                            }
                        }
                        break;
                    case "Int32":
                        {
                            var destination = (int*)ptr;
                            foreach (var fieldDefinition in fields)
                            {
                                *destination++ = (int)fieldDefinition.Constant;
                            }
                        }
                        break;
                    case "UInt32":
                        {
                            var destination = (uint*)ptr;
                            foreach (var fieldDefinition in fields)
                            {
                                *destination++ = (uint)fieldDefinition.Constant;
                            }
                        }
                        break;
                    case "Int64":
                        {
                            var destination = (long*)ptr;
                            foreach (var fieldDefinition in fields)
                            {
                                *destination++ = (long)fieldDefinition.Constant;
                            }
                        }
                        break;
                    case "UInt64":
                        {
                            var destination = (ulong*)ptr;
                            foreach (var fieldDefinition in fields)
                            {
                                *destination++ = (ulong)fieldDefinition.Constant;
                            }
                        }
                        break;
                }
            }
            return answer;
        }

        public static Instruction[] EnumGetValuesInstructions(this ModuleDefinition moduleDefinition, TypeDefinition enumTypeDefinition, ModuleDefinition systemModuleDefinition)
            => enumTypeDefinition.Fields.Count <= 1 ? ArrayEmptyInstructions(moduleDefinition, enumTypeDefinition, systemModuleDefinition) : InitializeArrayInstructions(moduleDefinition, enumTypeDefinition, systemModuleDefinition, enumTypeDefinition.Fields.Count - 1);

        private static Instruction[] InitializeArrayInstructions(ModuleDefinition moduleDefinition, TypeDefinition enumTypeDefinition, ModuleDefinition systemModuleDefinition, int count)
        {
            var privateImplementationDetails = moduleDefinition.GetOrCreatePrivateImplementationDetails(systemModuleDefinition);
            var dataFieldDefinition = moduleDefinition.GetOrCreateEnumValues(privateImplementationDetails, systemModuleDefinition, enumTypeDefinition);
            var initializeArrayMethodDefinition = systemModuleDefinition.GetRuntimeHelpers_InitializeArray();
            return new[]
            {
                InstructionUtility.LoadConstant(count),
                Instruction.Create(OpCodes.Newarr, moduleDefinition.ImportReference(enumTypeDefinition)),
                Instruction.Create(OpCodes.Dup),
                Instruction.Create(OpCodes.Ldtoken, moduleDefinition.ImportReference(dataFieldDefinition)),
                Instruction.Create(OpCodes.Call, moduleDefinition.ImportReference(initializeArrayMethodDefinition)),
            };
        }

        public static Instruction[] ArrayEmptyInstructions(this ModuleDefinition moduleDefinition, TypeDefinition enumTypeDefinition, ModuleDefinition systemModuleDefinition)
        {
            var arrayEmptyMethod = new GenericInstanceMethod(moduleDefinition.ImportReference(systemModuleDefinition.GetArray_Empty()))
            {
                GenericArguments = { moduleDefinition.ImportReference(enumTypeDefinition) }
            };
            return new[]
            {
                Instruction.Create(OpCodes.Call, arrayEmptyMethod),
            };
        }
    }
}