using System;
using System.Linq;
using Mono.Cecil;

namespace UniEnumExtension
{
    public sealed class EnumExtensionProcessorGeneric<T>
        : ITypeProcessor
        where T : unmanaged, IComparable<T>, IEquatable<T>
    {
        private static readonly string FullName;

        static EnumExtensionProcessorGeneric()
        {
            FullName = typeof(T).FullName;
        }

        public void ProcessRewriteToString(ModuleDefinition systemModuleDefinition, TypeDefinition enumTypeDefinition)
        {
            if (enumTypeDefinition.Methods.Any(x => x.Name == "ToString"))
            {
                return;
            }
            var method = EnumExtensionUtility.MakeToString(enumTypeDefinition);
            enumTypeDefinition.Methods.Add(method);
            var moduleDefinition = enumTypeDefinition.Module;
            EnumExtensionUtility.ImplementNoFlag<T>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, method);
        }

        public void ProcessAddIEquatable(ModuleDefinition systemModuleDefinition, TypeDefinition enumTypeDefinition)
        {
            if (enumTypeDefinition.Methods.Any(x => x.Name == "Equals"))
            {
                return;
            }
            enumTypeDefinition.Methods.Add(EnumExtensionUtility.MakeIEquatable(enumTypeDefinition, systemModuleDefinition));
        }

        public byte Stage => 64;

        public void Process(ModuleDefinition systemModuleDefinition, TypeDefinition typeDefinition)
        {
            if (!typeDefinition.IsEnum)
            {
                return;
            }
            if (typeDefinition.CustomAttributes.Any(customAttribute => customAttribute.AttributeType.FullName == "System.FlagsAttribute"))
            {
                return;
            }
            if (typeDefinition.Fields[0].FieldType.FullName != FullName)
            {
                return;
            }
            ProcessRewriteToString(systemModuleDefinition, typeDefinition);
            ProcessAddIEquatable(systemModuleDefinition, typeDefinition);
        }
    }
}
