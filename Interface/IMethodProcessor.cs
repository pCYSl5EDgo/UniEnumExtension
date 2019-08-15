using Mono.Cecil;

namespace UniEnumExtension
{
    public interface IMethodProcessor : IProcessor
    {
        void Process(ModuleDefinition systemModuleDefinition, MethodDefinition methodDefinition);
    }
}
