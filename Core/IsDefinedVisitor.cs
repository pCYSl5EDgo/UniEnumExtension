using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class IsDefinedVisitor : IMethodVisitor
    {
        List<Instruction> instructions = new List<Instruction>();
        public OpCode TargetOpCode => OpCodes.Ldtoken;
        public int Order => 128;
        public bool Rewrite(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition, ILProcessor processor, ref Instruction currentInstruction)
        {
            var enumTypeReference = currentInstruction.Operand as TypeReference;
            if (enumTypeReference is null || !enumTypeReference.IsValueType || enumTypeReference.IsGenericInstance || enumTypeReference.IsGenericParameter)
            {
                return false;
            }
            var enumTypeDefinition = enumTypeReference.ToDefinition();
            if (!enumTypeDefinition.IsEnum)
            {
                return false;
            }
            var getTypeFromHandleInstruction = currentInstruction.Next;
            if (getTypeFromHandleInstruction is null || getTypeFromHandleInstruction.OpCode != OpCodes.Call || !(getTypeFromHandleInstruction.Operand is MethodReference getTypeFromHandleMethodReference) || getTypeFromHandleMethodReference.FullName != "System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)")
            {
                return false;
            }
            instructions.Clear();
            var methodCallInstruction = getTypeFromHandleInstruction;
            while (true)
            {
                methodCallInstruction = methodCallInstruction.Next;
                if (methodCallInstruction is null)
                {
                    return false;
                }
                switch (methodCallInstruction.OpCode.FlowControl)
                {
                    case FlowControl.Next:
                        instructions.Add(methodCallInstruction);
                        break;
                    case FlowControl.Call:
                        var methodReference = ((MethodReference)methodCallInstruction.Operand);
                        if (methodReference.FullName != "System.Boolean System.Enum::IsDefined(System.Type,System.Object)")
                        {
                            return false;
                        }
                        goto AFTER;
                    default:
                        return false;
                }
            }
        AFTER:
            var methodDefinition = processor.Body.Method;
            if (instructions.Count == 0) throw new Exception("Invalid stack at " + methodDefinition.FullName);
            var first = instructions[0];
            var variables = processor.Body.Variables;
            TypeReference typeReference;
            bool hasThis = methodDefinition.HasThis;
            if (instructions.Count == 1)
            {
                switch (first.OpCode.Code)
                {
                    case Code.Ldstr:
                        return RewriteConstantString(processor, ref currentInstruction, getTypeFromHandleInstruction, methodCallInstruction, first, enumTypeDefinition);
                    case Code.Ldloc_0:
                        typeReference = variables[0].VariableType;
                        break;
                    case Code.Ldloc_1:
                        typeReference = variables[1].VariableType;
                        break;
                    case Code.Ldloc_2:
                        typeReference = variables[2].VariableType;
                        break;
                    case Code.Ldloc_3:
                        typeReference = variables[3].VariableType;
                        break;
                    case Code.Ldloc_S:
                    case Code.Ldloc:
                        typeReference = ((VariableDefinition)first.Operand).VariableType;
                        break;
                    case Code.Ldarg_0:
                        typeReference = hasThis ? processor.Body.ThisParameter.ParameterType : methodDefinition.Parameters[0].ParameterType;
                        break;
                    case Code.Ldarg_1:
                        typeReference = methodDefinition.Parameters[hasThis ? 0 : 1].ParameterType;
                        break;
                    case Code.Ldarg_2:
                        typeReference = methodDefinition.Parameters[hasThis ? 1 : 2].ParameterType;
                        break;
                    case Code.Ldarg_3:
                        typeReference = methodDefinition.Parameters[hasThis ? 2 : 3].ParameterType;
                        break;
                    case Code.Ldarg_S:
                    case Code.Ldarg:
                        typeReference = ((ParameterDefinition)first.Operand).ParameterType;
                        break;
                    case Code.Ldsfld:
                    case Code.Ldfld:
                        typeReference = ((FieldReference)first.Operand).FieldType;
                        break;
                    default:
                        return false;
                }
                if (typeReference.FullName == "System.String")
                    return RewriteString(moduleDefinition, systemModuleDefinition, processor, ref currentInstruction, enumTypeDefinition, getTypeFromHandleInstruction, methodCallInstruction);
                return false;
            }
            return true;
        }

        private bool RewriteString(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition, ILProcessor processor, ref Instruction currentInstruction, TypeDefinition enumTypeDefinition, Instruction getTypeFromHandleInstruction, Instruction methodCallInstruction)
        {
            return false;
            throw new NotImplementedException();
        }

        private bool RewriteConstantString(ILProcessor processor, ref Instruction currentInstruction, Instruction getTypeFromHandleInstruction, Instruction methodCallInstruction, Instruction loadStringInstruction, TypeDefinition enumTypeDefinition)
        {
            var constantString = (string)loadStringInstruction.Operand;
            var result = InstructionUtility.LoadConstant(enumTypeDefinition.Fields.Any(x => x.IsLiteral && x.IsStatic && x.Name == constantString));
            processor.Replace(currentInstruction, result);
            currentInstruction = result;
            processor.Remove(getTypeFromHandleInstruction);
            processor.Remove(loadStringInstruction);
            processor.Remove(methodCallInstruction);
            return true;
        }
    }
}
