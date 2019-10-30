using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public class EnumToStringMethodProcessor : IMethodProcessor
    {
        public byte Stage => 65;
        public bool ShouldProcess(TypeDefinition typeDefinition)
        {
            return !typeDefinition.IsEnum;
        }

        public void Process(ModuleDefinition systemModuleDefinition, MethodDefinition methodDefinition)
        {
            var instructions = methodDefinition.Body.Instructions;
            var moduleDefinition = methodDefinition.Module;
            using (ScopedProcessor processor = methodDefinition.Body.GetILProcessor())
            {
                for (var index = instructions.Count - 1; index >= 0; index--)
                {
                    TryProcessEachInstruction(instructions[index], processor, moduleDefinition, systemModuleDefinition);
                }
            }
        }

        private void TryProcessEachInstruction(Instruction constrainedInstruction, ScopedProcessor processor, ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition)
        {
            if (!(constrainedInstruction?.Operand is TypeReference enumTypeReference) || constrainedInstruction.OpCode.Code != Code.Constrained || !enumTypeReference.TryToDefinition(out var enumTypeDefinition) || enumTypeDefinition.HasMethods)
            {
                return;
            }
            var callVirtualInstruction = constrainedInstruction.Next;
            if (!(callVirtualInstruction?.Operand is MethodReference baseToStringMethodReference) || callVirtualInstruction.OpCode.Code != Code.Callvirt || baseToStringMethodReference.FullName != "System.String System.Object::ToString()")
            {
                return;
            }
            var utilityTypeDefinition = moduleDefinition.GetOrCreatePrivateImplementationDetails(systemModuleDefinition);
            var methodName = "pcysl5edgo_EnumSpecific_ToString_" + enumTypeDefinition.FullName.Replace('.', '/');
            var toStringMethodDefinition = utilityTypeDefinition.Methods.FirstOrDefault(x => x.IsStatic && x.Name == methodName) ?? Implement(moduleDefinition, systemModuleDefinition, methodName, enumTypeDefinition, utilityTypeDefinition);
            processor
                .Remove(constrainedInstruction)
                .Replace(callVirtualInstruction, Instruction.Create(OpCodes.Call, toStringMethodDefinition));
        }

        private static MethodDefinition Implement(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition, string methodName, TypeDefinition enumTypeDefinition, TypeDefinition privateImplementationDetails)
        {
            var toStringMethodDefinition = new MethodDefinition(methodName, MethodAttributes.Static | MethodAttributes.Final | MethodAttributes.Public | MethodAttributes.HideBySig, moduleDefinition.TypeSystem.String)
            {
                AggressiveInlining = true,
                Parameters = { new ParameterDefinition("value", ParameterAttributes.None, new ByReferenceType(moduleDefinition.ImportReference(enumTypeDefinition))) }
            };
            privateImplementationDetails.Methods.Add(toStringMethodDefinition);
            var hasFlag = enumTypeDefinition.HasCustomAttributes && enumTypeDefinition.CustomAttributes.Any(customAttribute => customAttribute.AttributeType.FullName == "System.FlagsAttribute");
            switch (enumTypeDefinition.Fields[0].FieldType.Name)
            {
                case "Byte":
                    if (hasFlag)
                    {
                        EnumExtensionUtility.ImplementFlags32<byte>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    else
                    {
                        EnumExtensionUtility.ImplementNoFlag<byte>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    break;
                case "SByte":
                    if (hasFlag)
                    {
                        EnumExtensionUtility.ImplementFlags32<sbyte>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    else
                    {
                        EnumExtensionUtility.ImplementNoFlag<sbyte>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    break;
                case "Int16":
                    if (hasFlag)
                    {
                        EnumExtensionUtility.ImplementFlags32<short>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    else
                    {
                        EnumExtensionUtility.ImplementNoFlag<short>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    break;
                case "UInt16":
                    if (hasFlag)
                    {
                        EnumExtensionUtility.ImplementFlags32<ushort>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    else
                    {
                        EnumExtensionUtility.ImplementNoFlag<ushort>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    break;
                case "Int32":
                    if (hasFlag)
                    {
                        EnumExtensionUtility.ImplementFlags32<int>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    else
                    {
                        EnumExtensionUtility.ImplementNoFlag<int>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    break;
                case "UInt32":
                    if (hasFlag)
                    {
                        EnumExtensionUtility.ImplementFlags32<uint>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    else
                    {
                        EnumExtensionUtility.ImplementNoFlag<uint>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    break;
                case "Int64":
                    if (hasFlag)
                    {
                        EnumExtensionUtility.ImplementFlags64<long>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    else
                    {
                        EnumExtensionUtility.ImplementNoFlag<long>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    break;
                case "UInt64":
                    if (hasFlag)
                    {
                        EnumExtensionUtility.ImplementFlags64<ulong>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    else
                    {
                        EnumExtensionUtility.ImplementNoFlag<ulong>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, toStringMethodDefinition);
                    }
                    break;
            }
            return toStringMethodDefinition;
        }
    }
}