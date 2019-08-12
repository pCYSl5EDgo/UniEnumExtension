using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class GetNamesVisitor : IMethodVisitor
    {
        public OpCode TargetOpCode => OpCodes.Ldtoken;
        public int Order => 64;
        public bool Rewrite(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition, ILProcessor processor, ref Instruction currentInstruction)
        {
            var currentInstructionOperandEnumTypeReference = currentInstruction.Operand as TypeReference;
            if (currentInstructionOperandEnumTypeReference is null || !currentInstructionOperandEnumTypeReference.IsValueType || currentInstructionOperandEnumTypeReference.IsGenericInstance || currentInstructionOperandEnumTypeReference.IsGenericParameter)
            {
                return false;
            }
            var enumTypeDefinition = currentInstructionOperandEnumTypeReference as TypeDefinition ?? currentInstructionOperandEnumTypeReference.Resolve();
            if (!enumTypeDefinition.IsEnum)
            {
                return false;
            }
            var getTypeFromHandleInstruction = currentInstruction.Next;
            if (getTypeFromHandleInstruction is null || getTypeFromHandleInstruction.OpCode != OpCodes.Call || !(getTypeFromHandleInstruction.Operand is MethodReference getTypeFromHandleMethodReference) || getTypeFromHandleMethodReference.FullName != "System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)")
            {
                return false;
            }
            var getNamesInstruction = getTypeFromHandleInstruction.Next;
            if (getNamesInstruction is null || getNamesInstruction.OpCode != OpCodes.Call || !(getNamesInstruction.Operand is MethodReference methodReference) || methodReference.FullName != "System.String[] System.Enum::GetNames(System.Type)")
            {
                return false;
            }
            var count = enumTypeDefinition.CalculateEnumCount();
            return ReplaceGetNames(moduleDefinition, enumTypeDefinition, processor, ref currentInstruction, getTypeFromHandleInstruction, getNamesInstruction, count);
        }

        private bool ReplaceGetNames(ModuleDefinition moduleDefinition, TypeDefinition enumTypeDefinition, ILProcessor processor, ref Instruction currentInstruction, Instruction getTypeFromHandleInstruction, Instruction getNamesInstruction, int count)
        {
            processor.Remove(getNamesInstruction);
            processor.Remove(getTypeFromHandleInstruction);
            var instruction0 = InstructionUtility.LoadConstant(count);
            processor.Replace(currentInstruction, instruction0);
            currentInstruction = instruction0;
            var instruction1 = Instruction.Create(OpCodes.Newarr, moduleDefinition.TypeSystem.String);
            processor.InsertAfter(instruction0, instruction1);
            var fields = enumTypeDefinition.Fields;
            for (var i = 0; i < count;)
            {
                instruction0 = instruction1;
                instruction1 = Instruction.Create(OpCodes.Dup);
                processor.InsertAfter(instruction0, instruction1);
                instruction0 = instruction1;
                instruction1 = InstructionUtility.LoadConstant(i);
                processor.InsertAfter(instruction0, instruction1);
                instruction0 = instruction1;
                instruction1 = Instruction.Create(OpCodes.Ldstr, fields[++i].Name);
                processor.InsertAfter(instruction0, instruction1);
                instruction0 = instruction1;
                instruction1 = Instruction.Create(OpCodes.Stelem_Ref);
                processor.InsertAfter(instruction0, instruction1);
            }
            JumpRewriteUtility.RewriteJump(processor, processor.Body.Instructions);
            return true;
        }
    }
}
