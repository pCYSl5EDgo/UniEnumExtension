using System;
using System.Linq;
using Mono.Cecil;

namespace UniEnumExtension
{
    public sealed class EnumExtensionProcessorFlags32BitOrLessSizeGeneric<T>
        : ITypeProcessor
        where T : unmanaged, IComparable<T>, IEquatable<T>
    {
        private static readonly string FullName;

        static EnumExtensionProcessorFlags32BitOrLessSizeGeneric()
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
            var moduleDefinition = enumTypeDefinition.Module;
            enumTypeDefinition.Methods.Add(method);
            if (!EnumExtensionUtility.ImplementFlags32<T>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, method))
            {
                enumTypeDefinition.Methods.Remove(method);
            }
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
            if (!typeDefinition.HasCustomAttributes || typeDefinition.CustomAttributes.All(customAttribute => customAttribute.AttributeType.FullName != "System.FlagsAttribute"))
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
