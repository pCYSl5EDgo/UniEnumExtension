using Mono.Cecil;

namespace UniEnumExtension
{
    public static class DefinitionUtility
    {
        public static TypeDefinition ToDefinition(this TypeReference reference) => reference as TypeDefinition ?? reference.Resolve();
    }
}
