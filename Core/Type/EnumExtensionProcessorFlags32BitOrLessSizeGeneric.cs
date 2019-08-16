using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed unsafe class EnumExtensionProcessorFlags32BitOrLessSizeGeneric<T>
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
            var method = EnumExtensionUtility.MakeToString(enumTypeDefinition);
            var moduleDefinition = enumTypeDefinition.Module;
            if (EnumExtensionUtility.ImplementFlags32<T>(systemModuleDefinition, moduleDefinition, enumTypeDefinition, method))
            {
                enumTypeDefinition.Methods.Add(method);
            }
        }

        public void ProcessAddIEquatable(ModuleDefinition systemModuleDefinition, TypeDefinition enumTypeDefinition)
        {
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
            var valueFieldDefinition = typeDefinition.Fields[0];
            if (valueFieldDefinition.FieldType.FullName != FullName)
            {
                return;
            }
            ProcessRewriteToString(systemModuleDefinition, typeDefinition);
            ProcessAddIEquatable(systemModuleDefinition, typeDefinition);
        }
    }
}
