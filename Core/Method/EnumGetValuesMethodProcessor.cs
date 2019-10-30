using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class EnumGetValuesMethodProcessor : IMethodProcessor
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
            var getValuesInstruction = getTypeFromHandleInstruction.Next;
            if (getValuesInstruction is null || getValuesInstruction.OpCode != OpCodes.Call || !(getValuesInstruction.Operand is MethodReference methodReference) || methodReference.FullName != "System.Array System.Enum::GetValues(System.Type)")
            {
                return;
            }
            var castClassOrIsInstInstruction = getValuesInstruction.Next;
            var enumsGetValuesInstructions = moduleDefinition.EnumGetValuesInstructions(enumTypeDefinition, systemModuleDefinition);
            if (!(castClassOrIsInstInstruction is null) && (castClassOrIsInstInstruction.OpCode == OpCodes.Isinst || castClassOrIsInstInstruction.OpCode == OpCodes.Castclass))
            {
                var castTargetClass = (TypeReference)castClassOrIsInstInstruction.Operand;
                switch (enumsGetValuesInstructions.Length)
                {
                    case 0: return;
                    case 1:
                        processor
                            .Replace(loadTokenInstruction, enumsGetValuesInstructions[0])
                            .Remove(getTypeFromHandleInstruction)
                            .Remove(getValuesInstruction);
                        if (castTargetClass.GetElementType().FullName == enumTypeDefinition.FullName)
                        {
                            processor.Remove(castClassOrIsInstInstruction);
                        }
                        break;
                    case 2:
                        processor
                            .Replace(loadTokenInstruction, enumsGetValuesInstructions[0])
                            .Replace(getTypeFromHandleInstruction, enumsGetValuesInstructions[1])
                            .Remove(getValuesInstruction);
                        if (castTargetClass.GetElementType().FullName == enumTypeDefinition.FullName)
                        {
                            processor.Remove(castClassOrIsInstInstruction);
                        }
                        break;
                    case 3:
                        processor
                            .Replace(loadTokenInstruction, enumsGetValuesInstructions[0])
                            .Replace(getTypeFromHandleInstruction, enumsGetValuesInstructions[1])
                            .Replace(getValuesInstruction, enumsGetValuesInstructions[2]);
                        if (castTargetClass.GetElementType().FullName == enumTypeDefinition.FullName)
                        {
                            processor.Remove(castClassOrIsInstInstruction);
                        }
                        break;
                    default:
                        processor
                            .Replace(loadTokenInstruction, enumsGetValuesInstructions[0])
                            .Replace(getTypeFromHandleInstruction, enumsGetValuesInstructions[1])
                            .Replace(getValuesInstruction, enumsGetValuesInstructions[2]);
                        if (castTargetClass.GetElementType().FullName == enumTypeDefinition.FullName)
                        {
                            processor.Replace(castClassOrIsInstInstruction, enumsGetValuesInstructions[3]);
                            for (var i = 4; i < enumsGetValuesInstructions.Length; i++)
                            {
                                processor.InsertAfter(enumsGetValuesInstructions[i - 1], enumsGetValuesInstructions[i]);
                            }
                        }
                        else
                        {
                            for (var i = 3; i < enumsGetValuesInstructions.Length; i++)
                            {
                                processor.InsertAfter(enumsGetValuesInstructions[i - 1], enumsGetValuesInstructions[i]);
                            }
                        }
                        break;
                }
            }
            else
            {
                switch (enumsGetValuesInstructions.Length)
                {
                    case 0: return;
                    case 1:
                        processor
                            .Replace(loadTokenInstruction, enumsGetValuesInstructions[0])
                            .Remove(getTypeFromHandleInstruction)
                            .Remove(getValuesInstruction);
                        break;
                    case 2:
                        processor
                            .Replace(loadTokenInstruction, enumsGetValuesInstructions[0])
                            .Replace(getTypeFromHandleInstruction, enumsGetValuesInstructions[1])
                            .Remove(getValuesInstruction);
                        break;
                    case 3:
                        processor
                            .Replace(loadTokenInstruction, enumsGetValuesInstructions[0])
                            .Replace(getTypeFromHandleInstruction, enumsGetValuesInstructions[1])
                            .Replace(getValuesInstruction, enumsGetValuesInstructions[2]);
                        break;
                    default:
                        processor
                            .Replace(loadTokenInstruction, enumsGetValuesInstructions[0])
                            .Replace(getTypeFromHandleInstruction, enumsGetValuesInstructions[1])
                            .Replace(getValuesInstruction, enumsGetValuesInstructions[2]);
                        for (var i = 3; i < enumsGetValuesInstructions.Length; i++)
                        {
                            processor.InsertAfter(enumsGetValuesInstructions[i - 1], enumsGetValuesInstructions[i]);
                        }
                        break;
                }
            }
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
