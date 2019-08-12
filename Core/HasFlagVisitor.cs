using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class HasFlagVisitor : IMethodVisitor
    {
        public OpCode TargetOpCode => OpCodes.Box;
        public int Order => 96;

        public bool Rewrite(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition, ILProcessor processor, ref Instruction currentInstruction)
        {
            TypeDefinition enumTypeDefinition;
            if (!(currentInstruction.Operand is TypeReference enumTypeReference) || !enumTypeReference.IsValueType || enumTypeReference.IsGenericInstance || enumTypeReference.IsGenericParameter || !(enumTypeDefinition = enumTypeReference.ToDefinition()).IsEnum) return false;
            var count = 0;
            var next = currentInstruction;
            while (true)
            {
                next = next.Next;
                if (next is null) return false;
                if (count == 1 && next.OpCode == OpCodes.Box)
                {
                    var next2 = next.Next;
                    if (next2 is null) return false;
                    if (next2.OpCode == OpCodes.Call && ((MethodReference)next2.Operand).FullName == "System.Boolean System.Enum::HasFlag(System.Enum)")
                    {
                        var enumTypeDefinition2 = ((TypeReference)next.Operand).ToDefinition();
                        if (!enumTypeDefinition2.IsEnum || enumTypeDefinition2.FullName != enumTypeDefinition.FullName) return false;
                        RewriteInner(processor, ref currentInstruction, next, next2);
                        return true;
                    }
                }
                if (!next.OpCode.StackCountChange(out var changeCount)) return false;
                count += changeCount;
                if (count < 0) return false;
            }
        }

        private void RewriteInner(ILProcessor processor, ref Instruction box1, Instruction box2, Instruction callInstruction)
        {
            var next = box1.Next;
            processor.Remove(box1);
            box1 = next;
            processor.Remove(box2);
            var instruction = Instruction.Create(OpCodes.And);
            processor.Replace(callInstruction, instruction);
        }
    }
}
