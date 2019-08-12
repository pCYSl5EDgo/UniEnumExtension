using Mono.Cecil;

namespace UniEnumExtension
{
    public interface IEnumsReplacer
    {
        void Replace(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition);
    }
}
