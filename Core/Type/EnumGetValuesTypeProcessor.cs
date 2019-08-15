using Mono.Cecil;

namespace UniEnumExtension
{
    public class EnumGetValuesTypeProcessor : ITypeProcessor
    {
        public byte Stage => 0;
        public void Process(ModuleDefinition systemModuleDefinition, TypeDefinition typeDefinition)
        {
            if (!typeDefinition.IsEnum) return;
            var method = new MethodDefinition("GetValues", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, new ArrayType(typeDefinition))
            {
                AggressiveInlining = true
            };
            typeDefinition.Methods.Add(method);
            var processor = method.Body.GetILProcessor();

            var module = typeDefinition.Module;

            var fieldsCount = typeDefinition.Fields.Count - 1;

            if (fieldsCount == 0)
            {
                processor
                    .Call(new GenericInstanceMethod(module.ImportReference(systemModuleDefinition.GetArray_Empty()))
                    {
                        GenericArguments = { typeDefinition }
                    })
                    .Ret();
                return;
            }
            var privateImplementationDetails = module.GetOrCreatePrivateImplementationDetails(systemModuleDefinition);
            var dataFieldDefinition = module.GetOrCreateEnumValues(privateImplementationDetails, systemModuleDefinition, typeDefinition);
            processor
                .LdC(fieldsCount)
                .NewArr(typeDefinition)
                .Dup()
                .LdToken(dataFieldDefinition)
                .Call(module.ImportReference(systemModuleDefinition.GetRuntimeHelpers_InitializeArray()))
                .Ret();
        }
    }
}