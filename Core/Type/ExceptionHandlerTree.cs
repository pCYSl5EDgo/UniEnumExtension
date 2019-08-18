using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil.Cil;
using UnityEngine;

namespace UniEnumExtension
{
    public sealed class ExceptionHandlerTree
    {
        public ExceptionHandlerTree Parent;
        public readonly ExceptionHandler Handler;
        public readonly List<ExceptionHandlerTree> Trees;

        public readonly List<LeaveDestination> RelayDestinations;
        public readonly List<Instruction> EndDestinations;
        public int VariableIndex;

        public (int constant, int variableIndex) FindIndex(Instruction destination)
        {
            for (var i = 0; i < EndDestinations.Count; i++)
            {
                if (ReferenceEquals(destination, EndDestinations[i]))
                    return (i, VariableIndex);
            }
            return Parent?.FindIndex(destination) ?? (-1, -1);
        }

        public override string ToString()
        {
            var buf = new StringBuilder();
            Append(buf, 0);
            return buf.ToString();
        }

        public Instruction[] ToArray() => EndDestinations.Concat(RelayDestinations.Select(x => x.Instruction)).ToArray();

        private void Append(StringBuilder buf, int indent)
        {
            buf.AppendLine().Append(' ', indent << 2);
            buf.Append(VariableIndex).Append(" : ").Append(RelayDestinations.Count).Append(" , ").Append(EndDestinations.Count);
            foreach (var tree in Trees)
            {
                tree.Append(buf, indent + 1);
            }
        }

        private ExceptionHandlerTree(ExceptionHandler handler, List<ExceptionHandlerTree> trees)
        {
            Parent = default;
            Handler = handler;
            Trees = trees;
            RelayDestinations = new List<LeaveDestination>();
            EndDestinations = new List<Instruction>();
            VariableIndex = -1;
        }

        private ExceptionHandlerTree(ExceptionHandler handler) : this(handler, new List<ExceptionHandlerTree>()) { }

        public static ExceptionHandlerTree Create(MethodBody body)
        {
            var handlers = body.ExceptionHandlers;
            switch (handlers.Count)
            {
                case 0:
                    throw new ArgumentOutOfRangeException();
                case 1:
                    return new ExceptionHandlerTree(handlers[0]);
            }
            var list = new List<ExceptionHandlerTree>(handlers.Count);
            foreach (var handler in handlers)
            {
                var tree = new ExceptionHandlerTree(handler);
                if (list.Count == 0)
                {
                    list.Add(tree);
                }
                else
                {
                    for (var j = 0; j < list.Count; j++)
                    {
                        if (list[j].TryLocateChild(tree)) break;
                        if (!tree.TryLocateChild(list[j])) continue;
                        list[j] = tree;
                        break;
                    }
                }
            }
            var answer = list.Count == 1 ? list[0] : new ExceptionHandlerTree(null, list);
            answer.ConstructDestinations();
            answer.CalculateVariableIndex(body);
            return answer;
        }

        private void CalculateVariableIndex(MethodBody body)
        {
            foreach (var tree in Trees)
            {
                tree.CalculateVariableIndex(body);
            }
            if (RelayDestinations.Count == 0 && EndDestinations.Count == 1)
            {
                return;
            }
            VariableIndex = body.Variables.Count;
            body.Variables.Add(new VariableDefinition(body.Method.Module.TypeSystem.Int32));
        }

        public bool IsInMyTryRange(Instruction instruction) => instruction.IsInRange(Handler.TryStart, Handler.TryEnd);

        private bool TryLocateChild(ExceptionHandlerTree childCandidate)
        {
            for (var index = 0; index < Trees.Count; index++)
            {
                var tree = Trees[index];
                if (tree.TryLocateChild(childCandidate)) return true;
                if (!childCandidate.TryLocateChild(tree)) continue;
                Trees[index] = tree;
                return true;
            }
            if (!childCandidate.Handler.TryStart.IsInRange(Handler.TryStart, Handler.TryEnd)) return false;
            childCandidate.Parent = this;
            Trees.Add(childCandidate);
            return true;
        }

        private void ConstructDestinations()
        {
            foreach (var tree in Trees)
            {
                tree.ConstructDestinations();
            }
            if (Handler is null) return;
            for (Instruction it = Handler.TryStart, end = Handler.TryEnd; !(it is null) && !ReferenceEquals(it, end);)
            {
                var duplicate = Trees.FirstOrDefault(x => x.IsInMyTryRange(it));
                if (!(duplicate is null))
                {
                    it = duplicate.Handler.TryEnd;
                    continue;
                }
                if (it.OpCode.Code == Code.Leave_S || it.OpCode.Code == Code.Leave)
                {
                    Register(new LeaveDestination((Instruction)it.Operand, this));
                }
                it = it.Next;
            }
        }

        private void Register(LeaveDestination destination)
        {
            if (IsInMyTryRange(destination.Instruction))
            {
                destination.ToTree = this;
                if (!EndDestinations.Contains(destination.Instruction))
                {
                    EndDestinations.Add(destination.Instruction);
                }
            }
            else if (Parent is null)
            {
                destination.ToTree = null;
                if (!EndDestinations.Contains(destination.Instruction))
                {
                    EndDestinations.Add(destination.Instruction);
                }
            }
            else
            {
                Parent.Register(destination);
                if (RelayDestinations.All(x => !ReferenceEquals(x.Instruction, destination.Instruction)))
                {
                    RelayDestinations.Add(destination);
                }
            }
        }
    }
}
