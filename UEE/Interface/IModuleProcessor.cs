using Mono.Cecil;

namespace UniEnumExtension
{
    public interface IModuleProcessor : IProcessor
    {
        void Process(ModuleDefinition moduleDefinition);
    }
}