using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public static class OpCodeUtility
    {
        public static OpCode FromShortToNormal(this OpCode opCode)
        {
            if (opCode == OpCodes.Blt_S)
                return OpCodes.Blt;
            if (opCode == OpCodes.Blt_Un_S)
                return OpCodes.Blt_Un;
            if (opCode == OpCodes.Ble_S)
                return OpCodes.Ble;
            if (opCode == OpCodes.Ble_Un_S)
                return OpCodes.Ble_Un;
            if (opCode == OpCodes.Bgt_S)
                return OpCodes.Bgt;
            if (opCode == OpCodes.Bgt_Un_S)
                return OpCodes.Bgt_Un;
            if (opCode == OpCodes.Bge_S)
                return OpCodes.Bge;
            if (opCode == OpCodes.Bge_Un_S)
                return OpCodes.Bge_Un;
            if (opCode == OpCodes.Beq_S)
                return OpCodes.Beq;
            if (opCode == OpCodes.Bne_Un_S)
                return OpCodes.Bne_Un;
            if (opCode == OpCodes.Br_S)
                return OpCodes.Br;
            if (opCode == OpCodes.Brtrue_S)
                return OpCodes.Brtrue;
            if (opCode == OpCodes.Brfalse_S)
                return OpCodes.Brfalse;
            if (opCode == OpCodes.Leave_S)
                return OpCodes.Leave;
            return opCode;
        }

        public static OpCode FromNormalToShort(this OpCode opCode)
        {
            if (opCode == OpCodes.Blt)
                return OpCodes.Blt_S;
            if (opCode == OpCodes.Blt_Un)
                return OpCodes.Blt_Un_S;
            if (opCode == OpCodes.Ble)
                return OpCodes.Ble_S;
            if (opCode == OpCodes.Ble_Un)
                return OpCodes.Ble_Un_S;
            if (opCode == OpCodes.Bgt)
                return OpCodes.Bgt_S;
            if (opCode == OpCodes.Bgt_Un)
                return OpCodes.Bgt_Un_S;
            if (opCode == OpCodes.Bge)
                return OpCodes.Bge_S;
            if (opCode == OpCodes.Bge_Un)
                return OpCodes.Bge_Un_S;
            if (opCode == OpCodes.Beq)
                return OpCodes.Beq_S;
            if (opCode == OpCodes.Bne_Un)
                return OpCodes.Bne_Un_S;
            if (opCode == OpCodes.Br)
                return OpCodes.Br_S;
            if (opCode == OpCodes.Brtrue)
                return OpCodes.Brtrue_S;
            if (opCode == OpCodes.Brfalse)
                return OpCodes.Brfalse_S;
            if (opCode == OpCodes.Leave)
                return OpCodes.Leave_S;
            return opCode;
        }

        public static bool StackCountChange(this OpCode code, out int count)
        {
            if (!code.StackBehaviourPop.StackCountChangeImpl(out count) || !code.StackBehaviourPush.StackCountChangeImpl(out var count2)) return false;
            count += count2;
            return true;
        }

        private static bool StackCountChangeImpl(this StackBehaviour behaviour, out int count)
        {
            switch (behaviour)
            {
                case StackBehaviour.Pop0:
                case StackBehaviour.Push0:
                    count = 0;
                    break;
                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                    count = -1;
                    break;
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                    count = -2;
                    break;
                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    count = -3;
                    break;
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Pushref:
                    count = 1;
                    break;
                case StackBehaviour.Push1_push1:
                    count = 2;
                    break;
                default:
                    count = 0;
                    return false;
            }
            return true;
        }
    }

    public unsafe struct PseudoOpCode
    {
        public byte Op1;
        public byte Op2;
        public byte Code;
        public byte FlowControl;
        public byte OpCodeType;
        public byte OperandType;
        public byte StackBehaviorPop;
        public byte StackBehaviorPush;

        public PseudoOpCode(byte op1, byte op2, byte code, byte flowControl, byte opCodeType, byte operandType, byte stackBehaviorPop, byte stackBehaviorPush)
        {
            Op1 = op1;
            Op2 = op2;
            Code = code;
            FlowControl = flowControl;
            OpCodeType = opCodeType;
            OperandType = operandType;
            StackBehaviorPop = stackBehaviorPop;
            StackBehaviorPush = stackBehaviorPush;
        }

        public static implicit operator PseudoOpCode(OpCode opCode) => *(PseudoOpCode*)&opCode;
        public static implicit operator OpCode(PseudoOpCode opCode) => *(OpCode*)&opCode;

        public Code CodeProperty
        {
            get => (Code)Code;
            set => Code = (byte)value;
        }

        public FlowControl FlowControlProperty
        {
            get => (FlowControl)FlowControl;
            set => FlowControl = (byte)value;
        }

        public StackBehaviour StackBehaviorPopProperty
        {
            get => (StackBehaviour)StackBehaviorPop;
            set => StackBehaviorPop = (byte)value;
        }

        public StackBehaviour StackBehaviorPushProperty
        {
            get => (StackBehaviour)StackBehaviorPush;
            set => StackBehaviorPush = (byte)value;
        }

        public OperandType OperandTypeProperty
        {
            get => (OperandType)OperandType;
            set => OperandType = (byte)value;
        }

        public override string ToString()
        {
            return Op1 + " " + Op2 + " " + CodeProperty + " " + FlowControlProperty + "\n" + OpCodeType + " " + OperandTypeProperty + " " + StackBehaviorPopProperty + " " + StackBehaviorPushProperty;
        }
    }
}
