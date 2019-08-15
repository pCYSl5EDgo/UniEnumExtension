using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class EnumGetValuesMethodProcessor : IMethodProcessor
    {
        public byte Stage => 16;
        public void Process(ModuleDefinition systemModuleDefinition, MethodDefinition methodDefinition)
        {
            var moduleDefinition = methodDefinition.Module;
            var processor = methodDefinition.Body.GetILProcessor();
            var instructions = methodDefinition.Body.Instructions;
            for (var index = instructions.Count - 1; index >= 0; index--)
            {
                var currentInstruction = instructions[index];
                if (currentInstruction.OpCode.Code != Code.Ldtoken || !(currentInstruction.Operand is TypeReference enumTypeReference) || !enumTypeReference.IsValueType || enumTypeReference.IsGenericInstance || enumTypeReference.IsGenericParameter)
                {
                    continue;
                }
                var enumTypeDefinition = enumTypeReference.ToDefinition();
                if (!enumTypeDefinition.IsEnum)
                {
                    continue;
                }
                var getTypeFromHandleInstruction = currentInstruction.Next;
                if (getTypeFromHandleInstruction is null || getTypeFromHandleInstruction.OpCode.Code != Code.Call || !(getTypeFromHandleInstruction.Operand is MethodReference getTypeFromHandleMethodReference) || getTypeFromHandleMethodReference.FullName != "System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)")
                {
                    continue;
                }
                var getValuesInstruction = getTypeFromHandleInstruction.Next;
                if (getValuesInstruction is null || getValuesInstruction.OpCode != OpCodes.Call || !(getValuesInstruction.Operand is MethodReference methodReference) || methodReference.FullName != "System.Array System.Enum::GetValues(System.Type)")
                {
                    continue;
                }
                var castClassOrIsInstInstruction = getValuesInstruction.Next;
                if (!(castClassOrIsInstInstruction is null) && (castClassOrIsInstInstruction.OpCode == OpCodes.Isinst || castClassOrIsInstInstruction.OpCode == OpCodes.Castclass))
                {
                    var castTargetClass = (TypeReference)castClassOrIsInstInstruction.Operand;
                    processor.Remove(getValuesInstruction);
                    processor.Remove(getTypeFromHandleInstruction);
                    var enumsGetValuesInstructions = moduleDefinition.EnumsGetValuesInstructions(enumTypeDefinition, systemModuleDefinition);
                    processor.Replace(currentInstruction, enumsGetValuesInstructions[0]);
                    for (var i = 1; i < enumsGetValuesInstructions.Length; i++)
                    {
                        processor.InsertAfter(enumsGetValuesInstructions[i - 1], enumsGetValuesInstructions[i]);
                    }
                    if (castTargetClass.GetElementType().FullName == enumTypeDefinition.FullName)
                    {
                        processor.Remove(castClassOrIsInstInstruction);
                    }
                }
                else
                {
                    var enumsGetValuesInstructions = moduleDefinition.EnumsGetValuesInstructions(enumTypeDefinition, systemModuleDefinition);
                    processor.Remove(getValuesInstruction);
                    processor.Remove(getTypeFromHandleInstruction);
                    processor.Replace(currentInstruction, enumsGetValuesInstructions[0]);
                    for (var i = 1; i < enumsGetValuesInstructions.Length; i++)
                    {
                        processor.InsertAfter(enumsGetValuesInstructions[i - 1], enumsGetValuesInstructions[i]);
                    }
                }
            }
        }
    }
}
