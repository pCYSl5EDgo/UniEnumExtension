using System;
using Mono.Cecil;

namespace UniEnumExtension
{
    public interface IEnumExtensionProcessor<T>
        where T : unmanaged, IComparable<T>
    {
        void Process(TypeDefinition enumTypeDefinition, FieldDefinition valueFieldDefinition);
    }
}
