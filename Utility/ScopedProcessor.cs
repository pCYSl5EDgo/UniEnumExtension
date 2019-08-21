using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace UniEnumExtension
{
    public sealed class ScopedProcessor : IDisposable
    {
        public readonly ILProcessor Processor;
        private readonly List<Instruction> branchInstructions;
        private readonly List<Instruction> switchInstructions;
        private readonly Collection<ExceptionHandler> exceptionHandlers;
        public bool IsAdded { get; private set; }

        public ScopedProcessor(ILProcessor processor)
        {
            Processor = processor;
            IsAdded = false;
            branchInstructions = new List<Instruction>();
            switchInstructions = new List<Instruction>();
            exceptionHandlers = processor.Body.ExceptionHandlers;
            var instructions = processor.Body.Instructions;
            foreach (var instruction in instructions)
            {
                if (instruction.OpCode.Code == Code.Switch)
                {
                    switchInstructions.Add(instruction);
                }
                else if (instruction.OpCode.FlowControl == FlowControl.Branch || instruction.OpCode.FlowControl == FlowControl.Cond_Branch)
                {
                    branchInstructions.Add(instruction);
                }
            }
        }

        public void Simplify()
        {
            IsAdded = true;
            var instructions = Processor.Body.Instructions;
            for (int i = 0; i < instructions.Count; i++)
            {
                switch (instructions[i].OpCode.Code)
                {
                    case Code.Br_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Br, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Brtrue_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Brtrue, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Brfalse_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Brfalse, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Blt_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Blt, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Blt_Un_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Blt_Un, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Bgt_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Bgt, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Bgt_Un_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Bgt_Un, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Ble_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Ble, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Ble_Un_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Ble_Un, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Bge_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Bge, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Bge_Un_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Bge_Un, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Beq_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Beq, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Bne_Un_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Bne_Un, (Instruction)instructions[i].Operand));
                        break;
                    case Code.Leave_S:
                        Replace(instructions[i], Instruction.Create(OpCodes.Leave, (Instruction)instructions[i].Operand));
                        break;
                }
            }
            RecalculateOffset();
        }

        public static implicit operator ScopedProcessor(ILProcessor processor) => new ScopedProcessor(processor);

        public ScopedProcessor Remove(Instruction target)
        {
            Instruction next;
            if (target.OpCode.FlowControl == FlowControl.Branch)
            {
                next = (Instruction)target.Operand;
            }
            else
            {
                next = target.Next;
            }
            for (var index = branchInstructions.Count - 1; index >= 0; index--)
            {
                var branchInstruction = branchInstructions[index];
                if (Equals(branchInstruction, target))
                {
                    branchInstructions.RemoveAt(index);
                    continue;
                }
                if (branchInstruction.Operand as Instruction == target)
                {
                    branchInstruction.Operand = next;
                }
            }
            for (var index = switchInstructions.Count - 1; index >= 0; index--)
            {
                var switchInstruction = switchInstructions[index];
                if (Equals(switchInstruction, target))
                {
                    branchInstructions.RemoveAt(index);
                    continue;
                }
                var instructions = (Instruction[])switchInstruction.Operand;
                for (var i = 0; i < instructions.Length; i++)
                {
                    ref var destination = ref instructions[i];
                    if (destination == target)
                        destination = next;
                }
            }
            foreach (var exceptionHandler in exceptionHandlers)
            {
                if (ReferenceEquals(exceptionHandler.HandlerStart, target))
                    exceptionHandler.HandlerStart = next;
                if (ReferenceEquals(exceptionHandler.HandlerEnd, target))
                    exceptionHandler.HandlerEnd = next;
                if (ReferenceEquals(exceptionHandler.TryStart, target))
                    exceptionHandler.TryStart = next;
                if (ReferenceEquals(exceptionHandler.TryEnd, target))
                    exceptionHandler.TryEnd = next;
            }
            Processor.Remove(target);
            return this;
        }

        public ScopedProcessor Replace(Instruction target, Instruction next)
        {
            IsAdded |= target.OpCode.Size < next.OpCode.Size;
            for (var index = branchInstructions.Count - 1; index >= 0; index--)
            {
                var branchInstruction = branchInstructions[index];
                if (Equals(branchInstruction, target))
                {
                    branchInstructions.RemoveAt(index);
                    continue;
                }
                if (branchInstruction.Operand as Instruction == target)
                {
                    branchInstruction.Operand = next;
                }
            }
            for (var index = switchInstructions.Count - 1; index >= 0; index--)
            {
                var switchInstruction = switchInstructions[index];
                if (Equals(switchInstruction, target))
                {
                    branchInstructions.RemoveAt(index);
                    continue;
                }
                var instructions = (Instruction[])switchInstruction.Operand;
                for (var i = 0; i < instructions.Length; i++)
                {
                    ref var destination = ref instructions[i];
                    if (destination == target)
                        destination = next;
                }
            }
            if (next.OpCode.Code == Code.Switch)
            {
                switchInstructions.Add(next);
            }
            else if (next.OpCode.FlowControl == FlowControl.Branch || next.OpCode.FlowControl == FlowControl.Cond_Branch)
            {
                branchInstructions.Add(next);
            }
            foreach (var exceptionHandler in exceptionHandlers)
            {
                if (ReferenceEquals(exceptionHandler.HandlerStart, target))
                    exceptionHandler.HandlerStart = next;
                if (ReferenceEquals(exceptionHandler.HandlerEnd, target))
                    exceptionHandler.HandlerEnd = next;
                if (ReferenceEquals(exceptionHandler.TryStart, target))
                    exceptionHandler.TryStart = next;
                if (ReferenceEquals(exceptionHandler.TryEnd, target))
                    exceptionHandler.TryEnd = next;
            }
            Processor.Replace(target, next);
            return this;
        }

        public ScopedProcessor InsertAfter(Instruction target, Instruction next)
        {
            IsAdded = true;
            Processor.InsertAfter(target, next);
            if (next.OpCode == OpCodes.Switch)
            {
                switchInstructions.Add(next);
            }
            else if (next.OpCode.FlowControl == FlowControl.Branch || next.OpCode.FlowControl == FlowControl.Cond_Branch)
            {
                branchInstructions.Add(next);
            }
            return this;
        }

        public ScopedProcessor InsertBefore(Instruction target, Instruction next)
        {
            IsAdded = true;
            Processor.InsertBefore(target, next);
            if (next.OpCode == OpCodes.Switch)
            {
                switchInstructions.Add(next);
            }
            else if (next.OpCode.FlowControl == FlowControl.Branch || next.OpCode.FlowControl == FlowControl.Cond_Branch)
            {
                branchInstructions.Add(next);
            }
            return this;
        }

        public void Dispose()
        {
            PreProcessOptimization();
            if (!IsAdded) return;
            Optimize();
        }

        public void PreProcessOptimization()
        {
            var bodyInstructions = Processor.Body.Instructions;
            while (true)
            {
                var notChanged = true;
                for (var i = bodyInstructions.Count - 1; i >= 0; i--)
                {
                    var instruction = bodyInstructions[i];
                    switch (instruction.OpCode.Code)
                    {
                        case Code.Nop:
                            Remove(instruction);
                            notChanged = false;
                            break;
                        case Code.Br:
                        case Code.Br_S:
                            {
                                if (!ReferenceEquals(instruction.Next, (Instruction)instruction.Operand)) continue;
                                Remove(instruction);
                                notChanged = false;
                                break;
                            }
                        case Code.Switch:
                            {
                                var destinationInstructions = (Instruction[])instruction.Operand;
                                switch (destinationInstructions.Length)
                                {
                                    case 0:
                                        InsertBefore(instruction, Instruction.Create(OpCodes.Pop));
                                        notChanged = false;
                                        Remove(instruction);
                                        break;
                                    case 1 when ReferenceEquals(destinationInstructions[0], instruction.Next):
                                        InsertBefore(instruction, Instruction.Create(OpCodes.Pop));
                                        notChanged = false;
                                        Remove(instruction);
                                        break;
                                }
                                break;
                            }
                    }
                }
                if (notChanged) break;
            }
            RecalculateOffset();
        }

        private void Optimize()
        {
            var instructions = Processor.Body.Instructions;
            bool changed;
            do
            {
                var offset = 0;
                changed = false;
                for (var i = 0; i < instructions.Count; i++)
                {
                    instructions[i].Offset = offset;
                    Instruction destinationInstruction;
                    switch (instructions[i].OpCode.Code)
                    {
                        case Code.Br:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Br_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Brtrue:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Brtrue_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Brfalse:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Brfalse_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Blt:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Blt_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Blt_Un:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Blt_Un_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Bgt:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Bgt_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Bgt_Un:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Bgt_Un_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Ble:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Ble_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Ble_Un:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Ble_Un_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Bge:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Bge_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Bge_Un:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Bge_Un_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Beq:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Beq_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Bne_Un:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Bne_Un_S, destinationInstruction));
                            changed = true;
                            break;
                        case Code.Leave:
                            if (ShouldContinue(instructions, i, out destinationInstruction)) { offset += instructions[i].GetSize(); continue; }
                            Replace(instructions[i], Instruction.Create(OpCodes.Leave_S, destinationInstruction));
                            changed = true;
                            break;
                    }
                    offset += instructions[i].GetSize();
                }
            } while (changed);
        }

        private static bool ShouldContinue(Collection<Instruction> instructions, int i, out Instruction destinationInstruction)
        {
            destinationInstruction = (Instruction)instructions[i].Operand;
            var diff = destinationInstruction.Offset - instructions[i].Offset;
            if (diff > sbyte.MaxValue || diff < sbyte.MinValue) return true;
            return false;
        }

        private void RecalculateOffset()
        {
            var offset = 0;
            foreach (var instruction in Processor.Body.Instructions)
            {
                instruction.Offset = offset;
                offset += instruction.GetSize();
            }
        }
    }
}