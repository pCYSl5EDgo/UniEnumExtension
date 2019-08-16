using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public static class OpCodeUtility
    {
        public static OpCode FromShortToNormal(this OpCode opCode)
        {
            PseudoOpCode p = opCode;
            p.OperandTypeProperty = OperandType.ShortInlineBrTarget;
            p.CodeProperty = p.CodeProperty.FromShortToNormal();
            p.Op2 += 13;
            return p;
        }

        public static Code FromShortToNormal(this Code opCode)
        {
            switch (opCode)
            {
                case Code.Blt_S:
                    return Code.Blt;
                case Code.Blt_Un_S:
                    return Code.Blt_Un;
                case Code.Ble_S:
                    return Code.Ble;
                case Code.Ble_Un_S:
                    return Code.Ble_Un;
                case Code.Bgt_S:
                    return Code.Bgt;
                case Code.Bgt_Un_S:
                    return Code.Bgt_Un;
                case Code.Bge_S:
                    return Code.Bge;
                case Code.Bge_Un_S:
                    return Code.Bge_Un;
                case Code.Beq_S:
                    return Code.Beq;
                case Code.Bne_Un_S:
                    return Code.Bne_Un;
                case Code.Br_S:
                    return Code.Br;
                case Code.Brtrue_S:
                    return Code.Brtrue;
                case Code.Brfalse_S:
                    return Code.Brfalse;
                default:
                    return opCode;
            }
        }

        public static OpCode FromNormalToShort(this OpCode opCode)
        {
            PseudoOpCode p = opCode;
            p.OperandTypeProperty = OperandType.InlineBrTarget;
            p.CodeProperty = p.CodeProperty.FromNormalToShort();
            p.Op2 -= 13;
            return p;
        }

        public static Code FromNormalToShort(this Code opCode)
        {
            switch (opCode)
            {
                case Code.Blt:
                    return Code.Blt_S;
                case Code.Blt_Un:
                    return Code.Blt_Un_S;
                case Code.Ble:
                    return Code.Ble_S;
                case Code.Ble_Un:
                    return Code.Ble_Un_S;
                case Code.Bgt:
                    return Code.Bgt_S;
                case Code.Bgt_Un:
                    return Code.Bgt_Un_S;
                case Code.Bge:
                    return Code.Bge_S;
                case Code.Bge_Un:
                    return Code.Bge_Un_S;
                case Code.Beq:
                    return Code.Beq_S;
                case Code.Bne_Un:
                    return Code.Bne_Un_S;
                case Code.Br:
                    return Code.Br_S;
                case Code.Brtrue:
                    return Code.Brtrue_S;
                case Code.Brfalse:
                    return Code.Brfalse_S;
                default:
                    return opCode;
            }
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
