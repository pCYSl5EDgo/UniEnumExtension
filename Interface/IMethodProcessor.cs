using Mono.Cecil;

namespace UniEnumExtension
{
    public interface IMethodProcessor : IProcessor
    {
        bool ShouldProcess(TypeDefinition typeDefinition);
        void Process(ModuleDefinition systemModuleDefinition, MethodDefinition methodDefinition);
    }
}
