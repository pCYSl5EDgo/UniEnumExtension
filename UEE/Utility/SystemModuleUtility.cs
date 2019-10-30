using System.Linq;
using Mono.Cecil;

namespace UniEnumExtension
{
    public static class SystemModuleUtility
    {
        public static MethodDefinition GetRuntimeHelpers_InitializeArray(this ModuleDefinition systemModuleDefinition)
        {
            return systemModuleDefinition.GetType("System.Runtime.CompilerServices", "RuntimeHelpers").Methods.Single(x => x.Name == "InitializeArray");
        }
        public static MethodDefinition GetArray_Empty(this ModuleDefinition systemModuleDefinition)
        {
            return systemModuleDefinition.GetType("System", "Array").Methods.Single(x => x.Name == "Empty");
        }
    }
}