using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class EnumHasFlagMethodProcessor : IMethodProcessor
    {
        private void RewriteInner(ScopedProcessor processor, Instruction box1, Instruction box2, Instruction callInstruction)
        {
            processor
                .Remove(box1)
                .Remove(box2)
                .Replace(callInstruction, Instruction.Create(OpCodes.And));
        }

        public byte Stage => 65;
        public bool ShouldProcess(TypeDefinition typeDefinition)
        {
            return !typeDefinition.IsEnum;
        }

        public void Process(ModuleDefinition systemModuleDefinition, MethodDefinition methodDefinition)
        {
            using (ScopedProcessor processor = methodDefinition.Body.GetILProcessor())
            {
                var instructions = methodDefinition.Body.Instructions;
                for (var index = instructions.Count - 1; index >= 0; index--)
                {
                    TryProcessEachInstruction(instructions[index], processor);
                }
            }
        }

        private void TryProcessEachInstruction(Instruction currentInstruction, ScopedProcessor processor)
        {
            if (!IsValidBoxInstruction(currentInstruction, out var enumTypeReference) || !enumTypeReference.TryToDefinition(out var enumTypeDefinition) || !enumTypeDefinition.IsEnum)
            {
                return;
            }
            TryRewriteFlowAnalysis(currentInstruction, processor, enumTypeDefinition);
        }

        private void TryRewriteFlowAnalysis(Instruction currentInstruction, ScopedProcessor processor, TypeDefinition enumTypeDefinition)
        {
            var count = 0;
            var next = currentInstruction;
            while (count >= 0)
            {
                next = next.Next;
                if (next is null) break;
                if (count == 1 && next.OpCode == OpCodes.Box)
                {
                    var next2 = next.Next;
                    if (next2 is null) break;
                    if (next2.OpCode == OpCodes.Call && ((MethodReference)next2.Operand).FullName == "System.Boolean System.Enum::HasFlag(System.Enum)")
                    {
                        if (!(next.Operand as TypeReference).TryToDefinition(out var enumTypeDefinition2))
                        {
                            break;
                        }
                        if (!enumTypeDefinition2.IsEnum || enumTypeDefinition2.FullName != enumTypeDefinition.FullName) break;
                        RewriteInner(processor, currentInstruction, next, next2);
                        break;
                    }
                }
                if (!next.OpCode.StackCountChange(out var changeCount)) break;
                count += changeCount;
            }
        }

        private static bool IsValidBoxInstruction(Instruction currentInstruction, out TypeReference enumTypeReference)
        {
            if (currentInstruction.OpCode.Code == Code.Box && currentInstruction.Operand is TypeReference reference)
            {
                enumTypeReference = reference;
                return !enumTypeReference.IsGenericInstance && !enumTypeReference.IsGenericParameter;
            }
            enumTypeReference = default;
            return false;
        }
    }
}
