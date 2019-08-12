using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class GetValuesVisitor : IMethodVisitor
    {
        public OpCode TargetOpCode => OpCodes.Ldtoken;
        public int Order => 32;
        public bool Rewrite(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition, ILProcessor processor, ref Instruction currentInstruction)
            => DecideWhetherToReplace(ref currentInstruction, out var tokenDefinition, out var getTypeFromHandleInstruction, out var getValuesInstruction, out var castClassOrIsInstInstruction)
               &&
               ReplaceGetValues(moduleDefinition, systemModuleDefinition, processor, ref currentInstruction, tokenDefinition, getTypeFromHandleInstruction, getValuesInstruction, castClassOrIsInstInstruction);

        private static bool DecideWhetherToReplace(ref Instruction currentInstruction, out TypeDefinition tokenDefinition, out Instruction getTypeFromHandleInstruction, out Instruction getValuesInstruction, out Instruction castClassOrIsInstInstruction)
        {
            var typeToken = currentInstruction.Operand as TypeReference;
            if (typeToken is null || !typeToken.IsValueType || typeToken.IsGenericInstance || typeToken.IsGenericParameter)
            {
                tokenDefinition = default;
                getTypeFromHandleInstruction = default;
                getValuesInstruction = default;
                castClassOrIsInstInstruction = default;
                return false;
            }
            tokenDefinition = typeToken as TypeDefinition ?? typeToken.Resolve();
            if (!tokenDefinition.IsEnum)
            {
                getTypeFromHandleInstruction = default;
                getValuesInstruction = default;
                castClassOrIsInstInstruction = default;
                return false;
            }
            getTypeFromHandleInstruction = currentInstruction.Next;
            if (getTypeFromHandleInstruction is null || getTypeFromHandleInstruction.OpCode != OpCodes.Call || !(getTypeFromHandleInstruction.Operand is MethodReference getTypeFromHandleMethodReference) || getTypeFromHandleMethodReference.FullName != "System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)")
            {
                getValuesInstruction = default;
                castClassOrIsInstInstruction = default;
                return false;
            }
            getValuesInstruction = getTypeFromHandleInstruction.Next;
            if (getValuesInstruction is null || getValuesInstruction.OpCode != OpCodes.Call || !(getValuesInstruction.Operand is MethodReference methodReference) || methodReference.FullName != "System.Array System.Enum::GetValues(System.Type)")
            {
                getValuesInstruction = default;
                castClassOrIsInstInstruction = default;
                return false;
            }
            castClassOrIsInstInstruction = getValuesInstruction.Next;
            return true;
        }

        private static bool ReplaceGetValues(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition, ILProcessor processor, ref Instruction currentInstruction, TypeDefinition enumTypeDefinition, Instruction getTypeFromHandleInstruction, Instruction getValuesInstruction, Instruction castClassOrIsInstInstruction)
        {
            if (castClassOrIsInstInstruction is null)
            {
                var instructions = moduleDefinition.EnumsGetValuesInstructions(enumTypeDefinition, systemModuleDefinition);
                processor.Remove(getValuesInstruction);
                processor.Remove(getTypeFromHandleInstruction);
                processor.Replace(currentInstruction, instructions[0]);
                currentInstruction = instructions[0];
                for (var i = 1; i < instructions.Length; i++)
                {
                    processor.InsertAfter(instructions[i - 1], instructions[i]);
                }
            }
            else if (castClassOrIsInstInstruction.OpCode == OpCodes.Isinst || castClassOrIsInstInstruction.OpCode == OpCodes.Castclass)
            {
                var castTargetClass = (TypeReference)castClassOrIsInstInstruction.Operand;
                processor.Remove(getValuesInstruction);
                processor.Remove(getTypeFromHandleInstruction);
                var instructions = moduleDefinition.EnumsGetValuesInstructions(enumTypeDefinition, systemModuleDefinition);
                processor.Replace(currentInstruction, instructions[0]);
                currentInstruction = instructions[0];
                for (var i = 1; i < instructions.Length; i++)
                {
                    processor.InsertAfter(instructions[i - 1], instructions[i]);
                }
                if (castTargetClass.GetElementType().FullName == enumTypeDefinition.FullName)
                {
                    processor.Remove(castClassOrIsInstInstruction);
                }
            }
            return true;
        }
    }
}
