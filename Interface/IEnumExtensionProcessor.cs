using System;
using Mono.Cecil;

namespace UniEnumExtension
{
    public interface IEnumExtensionProcessor<T>
        where T : unmanaged, IComparable<T>
    {
        void ProcessRewriteToString(TypeDefinition enumTypeDefinition, FieldDefinition valueFieldDefinition);
        void ProcessAddIEquatable(TypeDefinition enumTypeDefinition);
    }
}