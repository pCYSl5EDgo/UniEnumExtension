using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class LeaveDestination
    {
        public Instruction Instruction;
        public ExceptionHandlerTree FromTree;
        public ExceptionHandlerTree ToTree;

        public LeaveDestination(Instruction instruction, ExceptionHandlerTree fromTree)
        {
            Instruction = instruction;
            FromTree = fromTree;
        }
    }
}
