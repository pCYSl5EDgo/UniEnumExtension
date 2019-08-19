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
            foreach (var instruction in processor.Body.Instructions)
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
            return this;
        }

        public ScopedProcessor InsertBefore(Instruction target, Instruction next)
        {
            IsAdded = true;
            Processor.InsertBefore(target, next);
            return this;
        }

        public void Dispose()
        {
            {
                var bodyInstructions = Processor.Body.Instructions;
                for (var i = bodyInstructions.Count - 1; i >= 0; i--)
                {
                    var instruction = bodyInstructions[i];
                    if (instruction.OpCode.Code != Code.Br && instruction.OpCode.Code != Code.Br_S) continue;
                    if (!ReferenceEquals(instruction.Next, (Instruction)instruction.Operand)) continue;
                    Remove(instruction);
                }
            }
            if (!IsAdded) return;
            HEAD:
            for (var i = branchInstructions.Count - 1; i >= 0; i--)
            {
                var instruction = branchInstructions[i];
                var offset0 = instruction.Offset;
                var targetInstruction = (Instruction)instruction.Operand;
                var offset1 = targetInstruction.Offset;
                var diff = offset1 - offset0;
                if (instruction.OpCode.OperandType == OperandType.ShortInlineBrTarget && (diff > sbyte.MaxValue || diff < sbyte.MinValue))
                {
                    var replaceInstruction = Instruction.Create(instruction.OpCode.FromShortToNormal(), targetInstruction);
                    Replace(instruction, replaceInstruction);
                    goto HEAD;
                }
                if (instruction.OpCode.OperandType != OperandType.InlineBrTarget || diff < sbyte.MinValue || diff > sbyte.MaxValue) continue;
                {
                    var replaceInstruction = Instruction.Create(instruction.OpCode.FromNormalToShort(), targetInstruction);
                    Replace(instruction, replaceInstruction);
                    goto HEAD;
                }
            }
        }
    }
}