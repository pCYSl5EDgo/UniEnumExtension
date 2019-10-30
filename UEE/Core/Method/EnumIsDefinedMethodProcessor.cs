using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class EnumIsDefinedMethodProcessor : IMethodProcessor
    {
        private const string EnumIsDefinedMethodFullName = "System.Boolean System.Enum::IsDefined(System.Type,System.Object)";

        public byte Stage => 32;
        public bool ShouldProcess(TypeDefinition typeDefinition)
        {
            return !typeDefinition.IsEnum;
        }

        public void Process(ModuleDefinition systemModuleDefinition, MethodDefinition methodDefinition)
        {
            var parameters = methodDefinition.Parameters;
            var variables = methodDefinition.Body.Variables;
            var instructions = methodDefinition.Body.Instructions;
            var moduleDefinition = methodDefinition.Module;
            using (ScopedProcessor processor = methodDefinition.Body.GetILProcessor())
            {
                for (var index = instructions.Count - 1; index >= 0; index--)
                {
                    var loadTokenInstruction = instructions[index];
                    if (!IsValidLoadTokenInstanceType(loadTokenInstruction, out var enumTypeReference) || !enumTypeReference.TryToDefinition(out var enumTypeDefinition) || !enumTypeDefinition.IsEnum)
                    {
                        continue;
                    }
                    var enumBaseTypeName = enumTypeDefinition.Fields[0].FieldType.Name;
                    var getTypeFromHandleInstruction = loadTokenInstruction.Next;
                    if (!IsValidGetTypeFromHandleInstruction(getTypeFromHandleInstruction))
                    {
                        continue;
                    }
                    var nextInstruction = getTypeFromHandleInstruction.Next;
                    if (nextInstruction is null)
                    {
                        continue;
                    }
                    switch (nextInstruction.OpCode.Code)
                    {
                        case Code.Ldstr:
                            TryRewriteLoadStringConstant(processor, enumTypeDefinition, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldc_I4_0:
                        case Code.Ldc_I4_1:
                        case Code.Ldc_I4_2:
                        case Code.Ldc_I4_3:
                        case Code.Ldc_I4_4:
                        case Code.Ldc_I4_5:
                        case Code.Ldc_I4_6:
                        case Code.Ldc_I4_7:
                        case Code.Ldc_I4_8:
                        case Code.Ldc_I4_M1:
                        case Code.Ldc_I4_S:
                        case Code.Ldc_I4:
                            TryRewriteLoadInt32Constant(processor, enumTypeDefinition, enumBaseTypeName, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldc_I8:
                            TryRewriteLoadInt64Constant(enumBaseTypeName, nextInstruction, enumTypeDefinition, processor, loadTokenInstruction, getTypeFromHandleInstruction);
                            continue;
                        case Code.Ldloc_0:
                            TryRewriteLoadVariable(variables[0].VariableType, enumTypeDefinition, moduleDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldloc_1:
                            TryRewriteLoadVariable(variables[1].VariableType, enumTypeDefinition, moduleDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldloc_2:
                            TryRewriteLoadVariable(variables[2].VariableType, enumTypeDefinition, moduleDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldloc_3:
                            TryRewriteLoadVariable(variables[3].VariableType, enumTypeDefinition, moduleDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldloc_S:
                        case Code.Ldloc:
                            TryRewriteLoadVariable(((VariableDefinition)nextInstruction.Operand).VariableType, enumTypeDefinition, moduleDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldarg_0:
                            TryRewriteLoadVariable(methodDefinition.HasThis ? methodDefinition.Body.ThisParameter.ParameterType : parameters[0].ParameterType, enumTypeDefinition, moduleDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldarg_1:
                            TryRewriteLoadVariable(parameters[methodDefinition.HasThis ? 0 : 1].ParameterType, enumTypeDefinition, moduleDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldarg_2:
                            TryRewriteLoadVariable(parameters[methodDefinition.HasThis ? 1 : 2].ParameterType, enumTypeDefinition, moduleDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldarg_3:
                            TryRewriteLoadVariable(parameters[methodDefinition.HasThis ? 2 : 3].ParameterType, enumTypeDefinition, moduleDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldarg_S:
                        case Code.Ldarg:
                            TryRewriteLoadVariable(((ParameterDefinition)nextInstruction.Operand).ParameterType, enumTypeDefinition, moduleDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                        case Code.Ldfld:
                        case Code.Ldsfld:
                            TryRewriteLoadVariable(((FieldDefinition)nextInstruction.Operand).FieldType, enumTypeDefinition, moduleDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction, nextInstruction);
                            continue;
                    }
                }
            }
        }

        private static void RewriteConstantString(ScopedProcessor processor, TypeDefinition enumTypeDefinition, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction, Instruction loadStrInstruction, Instruction isDefinedInstruction)
        {
            var name = (string)loadStrInstruction.Operand;
            var constExpr = enumTypeDefinition.Fields.Skip(1).Any(x => x.Name == name);
            processor
                .Remove(loadTokenInstruction)
                .Remove(getTypeFromHandleInstruction)
                .Remove(loadStrInstruction)
                .Replace(isDefinedInstruction, InstructionUtility.LoadConstant(constExpr));
        }


        private static void TryRewriteLoadVariable(TypeReference targetType, TypeDefinition enumTypeDefinition, ModuleDefinition moduleDefinition, string enumBaseTypeName, ScopedProcessor processor, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction, Instruction nextInstruction)
        {
            if (targetType.IsArray || targetType.IsGenericParameter || targetType.IsGenericInstance)
            {
                return;
            }
            if (targetType.FullName == "System.String")
            {
                TryRewriteLoadStringVariable(enumTypeDefinition, moduleDefinition, processor, loadTokenInstruction, getTypeFromHandleInstruction, loadInstruction: nextInstruction);
            }
        }

        private static void TryRewriteLoadStringVariable(TypeDefinition enumTypeDefinition, ModuleDefinition moduleDefinition, ScopedProcessor processor, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction, Instruction loadInstruction)
        {
            var method = enumTypeDefinition.Methods.FirstOrDefault(x => x.Name == "IsDefinedString");
            if (method is null) return;
            var isDefinedInstruction = loadInstruction.Next;
            if (!IsValidIsDefinedInstruction(isDefinedInstruction))
            {
                return;
            }
            processor
                .Remove(loadTokenInstruction)
                .Remove(getTypeFromHandleInstruction)
                .Replace(isDefinedInstruction, Instruction.Create(OpCodes.Call, moduleDefinition.ImportReference(method)));
        }

        private static void TryRewriteLoadInt64Constant(string enumBaseTypeName, Instruction loadNumberInstruction, TypeDefinition enumTypeDefinition, ScopedProcessor processor, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction)
        {
            if (enumBaseTypeName.IsInt32OnStack())
            {
                TryRewriteLoadInt64ConstantWhenBaseInt32(loadNumberInstruction, enumTypeDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction);
            }
            else if (enumBaseTypeName.IsInt64OnStack())
            {
                TryRewriteLoadInt64ConstantWhenBaseInt64(loadNumberInstruction, enumTypeDefinition, enumBaseTypeName, processor, loadTokenInstruction, getTypeFromHandleInstruction);
            }
        }

        private static void TryRewriteLoadInt64ConstantWhenBaseInt64(Instruction loadNumberInstruction, TypeDefinition enumTypeDefinition, string enumBaseTypeName, ScopedProcessor processor, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction)
        {
            var boxInstruction = loadNumberInstruction.Next;
            if (!IsValidBoxInstruction(boxInstruction, enumTypeDefinition, enumBaseTypeName))
            {
                return;
            }
            var isDefinedInstruction = boxInstruction.Next;
            if (!IsValidIsDefinedInstruction(isDefinedInstruction))
            {
                return;
            }
            var number = (long)loadNumberInstruction.Operand;
            bool answer;
            switch (enumBaseTypeName)
            {
                case "Int64":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (long)x.Constant == number);
                    break;
                case "UInt64":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (ulong)x.Constant == (ulong)number);
                    break;
                default: throw new ArgumentOutOfRangeException(enumBaseTypeName);
            }
            processor
                .Remove(loadTokenInstruction)
                .Remove(getTypeFromHandleInstruction)
                .Remove(loadNumberInstruction)
                .Remove(boxInstruction)
                .Replace(isDefinedInstruction, InstructionUtility.LoadConstant(answer));
        }

        private static void TryRewriteLoadInt64ConstantWhenBaseInt32(Instruction loadNumberInstruction, TypeDefinition enumTypeDefinition, string enumBaseTypeName, ScopedProcessor processor, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction)
        {
            var convInstruction = loadNumberInstruction.Next;
            if (convInstruction is null)
            {
                return;
            }
            switch (convInstruction.OpCode.Code)
            {
                case Code.Conv_I1:
                case Code.Conv_I2:
                case Code.Conv_I4:
                case Code.Conv_U1:
                case Code.Conv_U2:
                case Code.Conv_U4:
                    break;
                default:
                    return;
            }
            var boxInstruction = convInstruction.Next;
            if (!IsValidBoxInstruction(boxInstruction, enumTypeDefinition, enumBaseTypeName))
            {
                return;
            }
            var isDefinedInstruction = boxInstruction.Next;
            if (!IsValidIsDefinedInstruction(isDefinedInstruction))
            {
                return;
            }
            var number = (long)loadNumberInstruction.Operand;
            bool answer;
            switch (enumBaseTypeName)
            {
                case "Byte":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (byte)x.Constant == number);
                    break;
                case "SByte":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (sbyte)x.Constant == number);
                    break;
                case "Int16":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (short)x.Constant == number);
                    break;
                case "UInt16":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (ushort)x.Constant == number);
                    break;
                case "Int32":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (int)x.Constant == number);
                    break;
                case "UInt32":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (uint)x.Constant == number);
                    break;
                default: throw new ArgumentException(enumBaseTypeName);
            }
            processor
                .Remove(loadTokenInstruction)
                .Remove(getTypeFromHandleInstruction)
                .Remove(loadNumberInstruction)
                .Remove(convInstruction)
                .Remove(boxInstruction)
                .Replace(isDefinedInstruction, InstructionUtility.LoadConstant(answer));
        }

        private static bool IsValidGetTypeFromHandleInstruction(Instruction getTypeFromHandleInstruction)
            => !(getTypeFromHandleInstruction is null) && getTypeFromHandleInstruction.OpCode.Code == Code.Call && getTypeFromHandleInstruction.Operand is MethodReference getTypeFromHandleMethodReference && getTypeFromHandleMethodReference.FullName == "System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)";

        private static bool IsValidLoadTokenInstanceType(Instruction loadTokenInstruction, out TypeReference enumTypeReference)
        {
            if (loadTokenInstruction.OpCode.Code != Code.Ldtoken || !(loadTokenInstruction.Operand is TypeReference reference))
            {
                enumTypeReference = null;
                return false;
            }
            enumTypeReference = reference;
            return !enumTypeReference.IsGenericInstance && !enumTypeReference.IsGenericParameter;
        }

        private static void TryRewriteLoadInt32Constant(ScopedProcessor processor, TypeDefinition enumTypeDefinition, string enumBaseTypeName, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction, Instruction loadNumberInstruction)
        {
            if (enumBaseTypeName.IsInt32OnStack())
            {
                TryRewriteLoadInt32ConstantWhenBaseInt32(enumBaseTypeName, enumTypeDefinition, processor, loadTokenInstruction, getTypeFromHandleInstruction, loadNumberInstruction);
            }
            else if (enumBaseTypeName.IsInt64OnStack())
            {
                TryRewriteLoadInt32ConstantWhenBaseInt64(enumBaseTypeName, enumTypeDefinition, processor, loadTokenInstruction, getTypeFromHandleInstruction, loadNumberInstruction);
            }
        }

        private static void TryRewriteLoadInt32ConstantWhenBaseInt64(string enumBaseTypeName, TypeDefinition enumTypeDefinition, ScopedProcessor processor, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction, Instruction loadNumberInstruction)
        {
            var convInstruction = loadNumberInstruction.Next;
            if (convInstruction is null || (convInstruction.OpCode.Code != Code.Conv_I8 && convInstruction.OpCode.Code != Code.Conv_U8))
            {
                return;
            }
            var boxInstruction = convInstruction.Next;
            if (!IsValidBoxInstruction(boxInstruction, enumTypeDefinition, enumBaseTypeName))
            {
                return;
            }
            var isDefinedInstruction = boxInstruction.Next;
            if (!IsValidIsDefinedInstruction(isDefinedInstruction))
            {
                return;
            }
            RewriteConstantInt64(processor, enumTypeDefinition, loadTokenInstruction, getTypeFromHandleInstruction, loadNumberInstruction, convInstruction, boxInstruction, isDefinedInstruction, enumBaseTypeName);
        }

        private static void RewriteConstantInt64(ScopedProcessor processor, TypeDefinition enumTypeDefinition, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction, Instruction loadNumberInstruction, Instruction convInstruction, Instruction boxInstruction, Instruction isDefinedInstruction, string enumBaseTypeName)
        {
            bool answer;
            switch (enumBaseTypeName)
            {
                case "Int64":
                    {
                        long number = loadNumberInstruction.GetInt32();
                        answer = enumTypeDefinition.Fields.Skip(1).Any(x => (long)x.Constant == number);
                    }
                    break;
                case "UInt64":
                    {
                        var number = (ulong)loadNumberInstruction.GetInt32();
                        answer = enumTypeDefinition.Fields.Skip(1).Any(x => (ulong)x.Constant == number);
                    }
                    break;
                default: throw new ArgumentException(enumBaseTypeName);
            }
            processor
                .Remove(loadTokenInstruction)
                .Remove(getTypeFromHandleInstruction)
                .Remove(loadNumberInstruction)
                .Remove(convInstruction)
                .Remove(boxInstruction)
                .Replace(isDefinedInstruction, InstructionUtility.LoadConstant(answer));
        }

        private static void TryRewriteLoadInt32ConstantWhenBaseInt32(string enumBaseTypeName, TypeDefinition enumTypeDefinition, ScopedProcessor processor, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction, Instruction loadNumberInstruction)
        {
            var instructionNext = loadNumberInstruction.Next;
            if (instructionNext is null)
            {
                return;
            }
            switch (instructionNext.OpCode.Code)
            {
                case Code.Box:
                    {
                        var boxInstruction = instructionNext;
                        if (!IsValidBoxInstruction(boxInstruction, enumTypeDefinition, enumBaseTypeName))
                        {
                            return;
                        }
                        var isDefinedInstruction = boxInstruction.Next;
                        if (!IsValidIsDefinedInstruction(isDefinedInstruction))
                        {
                            return;
                        }
                        RewriteConstantInt32(processor, enumTypeDefinition, loadTokenInstruction, getTypeFromHandleInstruction, loadNumberInstruction, boxInstruction, isDefinedInstruction, enumBaseTypeName);
                    }
                    break;
                case Code.Conv_U1:
                case Code.Conv_U2:
                case Code.Conv_U4:
                case Code.Conv_I1:
                case Code.Conv_I2:
                case Code.Conv_I4:
                    {
                        var convInstruction = instructionNext;
                        var boxInstruction = convInstruction.Next;
                        if (!IsValidBoxInstruction(boxInstruction, enumTypeDefinition, enumBaseTypeName))
                        {
                            return;
                        }
                        var isDefinedInstruction = boxInstruction.Next;
                        if (!IsValidIsDefinedInstruction(isDefinedInstruction))
                        {
                            return;
                        }
                        processor.Remove(convInstruction);
                        RewriteConstantInt32(processor, enumTypeDefinition, loadTokenInstruction, getTypeFromHandleInstruction, loadNumberInstruction, boxInstruction, isDefinedInstruction, enumBaseTypeName);
                        return;
                    }
                default:
                    return;
            }
        }

        private static void TryRewriteLoadStringConstant(ScopedProcessor processor, TypeDefinition enumTypeDefinition, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction, Instruction loadStrInstruction)
        {
            var isDefinedInstruction = loadStrInstruction.Next;
            if (!IsValidIsDefinedInstruction(isDefinedInstruction))
            {
                return;
            }
            RewriteConstantString(processor, enumTypeDefinition, loadTokenInstruction, getTypeFromHandleInstruction, loadStrInstruction, isDefinedInstruction);
        }

        private static void RewriteConstantInt32(ScopedProcessor processor, TypeDefinition enumTypeDefinition, Instruction loadTokenInstruction, Instruction getTypeFromHandleInstruction, Instruction loadNumberInstruction, Instruction boxInstruction, Instruction isDefinedInstruction, string enumBaseTypeName)
        {
            processor
                .Remove(loadTokenInstruction)
                .Remove(getTypeFromHandleInstruction)
                .Remove(boxInstruction)
                .Remove(isDefinedInstruction);
            var number = loadNumberInstruction.GetInt32();
            bool answer;
            switch (enumBaseTypeName)
            {
                case "Byte":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (byte)x.Constant == number);
                    break;
                case "SByte":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (sbyte)x.Constant == number);
                    break;
                case "Int16":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (short)x.Constant == number);
                    break;
                case "UInt16":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (ushort)x.Constant == number);
                    break;
                case "Int32":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (int)x.Constant == number);
                    break;
                case "UInt32":
                    answer = enumTypeDefinition.Fields.Skip(1).Any(x => (uint)x.Constant == number);
                    break;
                default: throw new ArgumentException(enumBaseTypeName);
            }
            processor.Replace(loadNumberInstruction, InstructionUtility.LoadConstant(answer));
        }

        private static bool IsValidBoxInstruction(Instruction boxInstruction, TypeDefinition enumTypeDefinition, string enumBaseTypeName)
            => boxInstruction?.Operand is TypeReference typeReference && boxInstruction.OpCode.Code == Code.Box && (typeReference.FullName == enumTypeDefinition.FullName || typeReference.Name == enumBaseTypeName);

        private static bool IsValidIsDefinedInstruction(Instruction isDefinedInstruction)
            => isDefinedInstruction?.Operand is MethodReference methodReference && isDefinedInstruction.OpCode.Code == Code.Call && methodReference.FullName == EnumIsDefinedMethodFullName;
    }
}
