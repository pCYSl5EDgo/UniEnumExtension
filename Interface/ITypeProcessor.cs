using Mono.Cecil;

namespace UniEnumExtension
{
    public interface ITypeProcessor : IProcessor
    {
        void Process(ModuleDefinition systemModuleDefinition, TypeDefinition typeDefinition);
    }
}
