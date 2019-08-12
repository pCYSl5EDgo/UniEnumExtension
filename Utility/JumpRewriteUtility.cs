using Mono.Cecil.Cil;
using UnityEngine;

namespace UniEnumExtension
{
    public static class JumpRewriteUtility
    {
        public static void RewriteJump(ILProcessor processor, Mono.Collections.Generic.Collection<Instruction> instructions)
        {
            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];
                var opCode = instruction.OpCode;
                if (opCode.OperandType != OperandType.ShortInlineBrTarget || !(instruction.Operand is Instruction destination)) continue;
                opCode = FromShortToNormalVersionOpCode(opCode);
                processor.Replace(instruction, Instruction.Create(opCode, destination));
            }
        }

        private static OpCode FromShortToNormalVersionOpCode(OpCode opCode)
        {
            if (opCode == OpCodes.Blt_S)
            {
                return OpCodes.Blt;
            }
            if (opCode == OpCodes.Blt_Un_S)
            {
                return OpCodes.Blt_Un;
            }
            if (opCode == OpCodes.Ble_S)
            {
                return OpCodes.Ble;
            }
            if (opCode == OpCodes.Ble_Un_S)
            {
                return OpCodes.Ble_Un;
            }
            if (opCode == OpCodes.Bgt_S)
            {
                return OpCodes.Bgt;
            }
            if (opCode == OpCodes.Bgt_Un_S)
            {
                return OpCodes.Bgt_Un;
            }
            if (opCode == OpCodes.Bge_S)
            {
                return OpCodes.Bge;
            }
            if (opCode == OpCodes.Bge_Un_S)
            {
                return OpCodes.Bge_Un;
            }
            if (opCode == OpCodes.Beq_S)
            {
                return OpCodes.Beq;
            }
            if (opCode == OpCodes.Bne_Un_S)
            {
                return OpCodes.Bne_Un;
            }
            if (opCode == OpCodes.Br_S)
            {
                return OpCodes.Br;
            }
            if (opCode == OpCodes.Brtrue_S)
            {
                return OpCodes.Brtrue;
            }
            if (opCode == OpCodes.Brfalse_S)
            {
                return OpCodes.Brfalse;
            }
            return opCode;
        }
    }
}
