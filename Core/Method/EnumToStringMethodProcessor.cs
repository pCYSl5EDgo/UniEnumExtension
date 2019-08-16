using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

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
        }
    }
}