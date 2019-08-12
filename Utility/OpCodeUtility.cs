using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public static class OpCodeUtility
    {
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
}
