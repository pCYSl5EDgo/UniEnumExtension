using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class EnumHasFlagMethodProcessor : IMethodProcessor
    {
        private void RewriteInner(ILProcessor processor, Instruction box1, Instruction box2, Instruction callInstruction)
        {
            processor.Remove(box1);
            processor.Remove(box2);
            var instruction = Instruction.Create(OpCodes.And);
            processor.Replace(callInstruction, instruction);
        }

        public byte Stage => 65;
        public void Process(ModuleDefinition systemModuleDefinition, MethodDefinition methodDefinition)
        {
            var processor = methodDefinition.Body.GetILProcessor();
            var instructions = methodDefinition.Body.Instructions;
            for (var index = instructions.Count - 1; index >= 0; index--)
            {
                var currentInstruction = instructions[index];
                if (currentInstruction.OpCode.Code != Code.Box || !(currentInstruction.Operand is TypeReference enumTypeReference) || !enumTypeReference.IsValueType || enumTypeReference.IsGenericInstance || enumTypeReference.IsGenericParameter)
                {
                    continue;
                }
                var enumTypeDefinition = enumTypeReference.ToDefinition();
                if (!enumTypeDefinition.IsEnum)
                {
                    continue;
                }
                var count = 0;
                var next = currentInstruction;
                while (count >= 0)
                {
                    next = next?.Next;
                    if (next is null) break;
                    if (count == 1 && next.OpCode == OpCodes.Box)
                    {
                        var next2 = next.Next;
                        if (next2 is null) break;
                        if (next2.OpCode == OpCodes.Call && ((MethodReference)next2.Operand).FullName == "System.Boolean System.Enum::HasFlag(System.Enum)")
                        {
                            var enumTypeDefinition2 = ((TypeReference)next.Operand).ToDefinition();
                            if (!enumTypeDefinition2.IsEnum || enumTypeDefinition2.FullName != enumTypeDefinition.FullName) break;
                            RewriteInner(processor, currentInstruction, next, next2);
                            break;
                        }
                    }
                    if (!next.OpCode.StackCountChange(out var changeCount)) break;
                    count += changeCount;
                }
            }
        }
    }
}
