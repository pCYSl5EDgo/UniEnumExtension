using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public interface IMethodVisitor
    {
        OpCode TargetOpCode { get; }
        int Order { get; }
        bool Rewrite(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition, ILProcessor processor, ref Instruction currentInstruction);
    }
}
