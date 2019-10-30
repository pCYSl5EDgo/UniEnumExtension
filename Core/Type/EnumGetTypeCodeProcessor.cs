using Mono.Cecil;

namespace UniEnumExtension
{
    public sealed class EnumGetTypeCodeProcessor
        : ITypeProcessor
    {
        public byte Stage => 128;

        public void Process(ModuleDefinition systemModuleDefinition, TypeDefinition typeDefinition)
        {
            if (!typeDefinition.IsEnum)
            {
                return;
            }
            var module = typeDefinition.Module;
            var method = new MethodDefinition("GetUnderTypeCode",
                MethodAttributes.Final | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                module.ImportReference(systemModuleDefinition.GetType("System", "TypeCode")))
            {
                AggressiveInlining = true,
            };
            typeDefinition.Methods.Add(method);

            var processor = method.Body.GetILProcessor();

            switch (typeDefinition.Fields[0].FieldType.Name)
            {
                case "SByte":
                    processor.LdC(5);
                    break;
                case "Byte":
                    processor.LdC(6);
                    break;
                case "Int16":
                    processor.LdC(7);
                    break;
                case "UInt16":
                    processor.LdC(8);
                    break;
                case "Int32":
                    processor.LdC(9);
                    break;
                case "UInt32":
                    processor.LdC(10);
                    break;
                case "Int64":
                    processor.LdC(11);
                    break;
                case "UInt64":
                    processor.LdC(12);
                    break;
            }

            processor.Ret();
        }
    }
}