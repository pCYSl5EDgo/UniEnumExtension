using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class EnumGetUnderlyingTypeProcessor
        : IMethodProcessor
    {
        public byte Stage => 16;
        public bool ShouldProcess(TypeDefinition typeDefinition)
        {
            return !typeDefinition.IsEnum;
        }
        
        public void Process(ModuleDefinition systemModuleDefinition, MethodDefinition methodDefinition)
        {
            var moduleDefinition = methodDefinition.Module;
            using (ScopedProcessor processor = methodDefinition.Body.GetILProcessor())
            {
                var instructions = methodDefinition.Body.Instructions;
                for (var index = instructions.Count - 1; index >= 0; index--)
                {
                    var currentInstruction = instructions[index];
                    ProcessEachInstruction(systemModuleDefinition, currentInstruction, processor, moduleDefinition);
                }
            }
        }

        private static void ProcessEachInstruction(ModuleDefinition systemModuleDefinition, Instruction loadTokenInstruction, ScopedProcessor processor, ModuleDefinition moduleDefinition)
        {
            if (!IsValidLoadTokenInstruction(loadTokenInstruction, out var enumTypeReference) || !enumTypeReference.TryToDefinition(out var enumTypeDefinition) || !enumTypeDefinition.IsEnum)
            {
                return;
            }
            var getTypeFromHandleInstruction = loadTokenInstruction.Next;
            if (getTypeFromHandleInstruction is null || getTypeFromHandleInstruction.OpCode.Code != Code.Call || !(getTypeFromHandleInstruction.Operand is MethodReference getTypeFromHandleMethodReference) || getTypeFromHandleMethodReference.FullName != "System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)")
            {
                return;
            }
            var getUnderLyingTypeInstruction = getTypeFromHandleInstruction.Next;
            if (getUnderLyingTypeInstruction is null || getUnderLyingTypeInstruction.OpCode != OpCodes.Call || !(getUnderLyingTypeInstruction.Operand is MethodReference methodReference) || methodReference.FullName != "System.Type System.Enum::GetUnderlyingType(System.Type)")
            {
                return;
            }
            processor.Replace(loadTokenInstruction, Instruction.Create(OpCodes.Ldtoken, processor.Processor.Body.Method.Module.ImportReference(enumTypeDefinition.Fields[0].FieldType)));
            processor.Remove(getUnderLyingTypeInstruction);
        }
        
        private static bool IsValidLoadTokenInstruction(Instruction currentInstruction, out TypeReference enumTypeReference)
        {
            if (currentInstruction.OpCode.Code == Code.Ldtoken && currentInstruction.Operand is TypeReference typeReference)
            {
                enumTypeReference = typeReference;
                return !enumTypeReference.IsGenericInstance && !enumTypeReference.IsGenericParameter;
            }
            enumTypeReference = default;
            return false;
        }
    }
}