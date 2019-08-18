using System;
using System.Collections;
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

        public Instruction[] EndDestinations;
        public int VariableIndex;
        public bool IsOnlyRelay;

        public (int constant, ExceptionHandlerTree tree) FindIndex(Instruction destination)
        {
            for (var i = 0; i < EndDestinations.Length; i++)
            {
                if (ReferenceEquals(destination, EndDestinations[i]))
                    return (i, this);
            }
            return Parent.FindIndex(destination);
        }

        public override string ToString()
        {
            var buf = new StringBuilder();
            Append(buf, 0);
            return buf.ToString();
        }

        public Instruction[] ToArray() => EndDestinations;


        private void Append(StringBuilder buf, int indent)
        {
            buf.AppendLine().Append(' ', indent << 2);
            buf.Append(VariableIndex).Append(" : ").Append(EndDestinations.Length);
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
            EndDestinations = Array.Empty<Instruction>();
            VariableIndex = -1;
            IsOnlyRelay = false;
        }

        private ExceptionHandlerTree(ExceptionHandler handler) : this(handler, new List<ExceptionHandlerTree>()) { }

        public static ExceptionHandlerTree Create(MethodBody body)
        {
            var handlers = body.ExceptionHandlers;
            if (handlers.Count == 0) throw new ArgumentOutOfRangeException();
            var list = new List<ExceptionHandlerTree>(handlers.Count);
            foreach (var handler in handlers)
            {
                var tree = new ExceptionHandlerTree(handler);

                for (var j = 0; j < list.Count; j++)
                {
                    if (list[j].TryLocateChild(tree))
                    {
                        goto TAIL;
                    }

                    if (tree.TryLocateChild(list[j]))
                    {
                        list[j] = tree;
                        goto TAIL;
                    }
                }
                list.Add(tree);
            TAIL:;
            }
            ExceptionHandlerTree answer;
            if (list.Count == 1)
            {
                Debug.Log("Single");
                answer = list[0];
            }
            else
            {
                Debug.Log("Multiple");
                answer = new ExceptionHandlerTree(null, list);
            }
            Debug.Log(answer.Trees.Count);
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
            var hasEndDestinations = EndDestinations.Length > 0;
            if (IsOnlyRelay | hasEndDestinations)
            {
                VariableIndex = body.Variables.Count;
            }
            if (IsOnlyRelay)
            {
                if (hasEndDestinations)
                {
                    IsOnlyRelay = false;
                }
                else
                {
                    body.Variables.Add(new VariableDefinition(body.Method.Module.TypeSystem.Boolean));
                }
            }
            if (hasEndDestinations)
            {
                body.Variables.Add(new VariableDefinition(body.Method.Module.TypeSystem.Int32));
            }
        }

        public bool IsInMyTryRange(Instruction instruction) => instruction.IsInRange(Handler.TryStart, Handler.TryEnd);

        private bool TryLocateChild(ExceptionHandlerTree childCandidate)
        {
            for (var index = 0; index < Trees.Count; index++)
            {
                var tree = Trees[index];
                if (tree.TryLocateChild(childCandidate))
                {
                    return true;
                }
                if (childCandidate.TryLocateChild(tree))
                {
                    Trees[index] = tree;
                    return true;
                }
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
                if (duplicate is null)
                {
                    if (it.OpCode.Code == Code.Leave_S || it.OpCode.Code == Code.Leave)
                    {
                        var leaveDestination = new LeaveDestination((Instruction)it.Operand, this);
                        if (Parent is null)
                        {
                            Array.Resize(ref EndDestinations, EndDestinations.Length + 1);
                            EndDestinations[EndDestinations.Length - 1] = leaveDestination.Instruction;
                        }
                        else
                        {
                            Parent.Register(leaveDestination);
                        }
                    }
                    it = it.Next;
                }
                else
                {
                    it = duplicate.Handler.TryEnd;
                }
            }
        }

        private void Register(LeaveDestination destination)
        {
            if (IsInMyTryRange(destination.Instruction))
            {
                destination.ToTree = this;
                if (!EndDestinations.Contains(destination.Instruction))
                {
                    Array.Resize(ref EndDestinations, EndDestinations.Length + 1);
                    EndDestinations[EndDestinations.Length - 1] = destination.Instruction;
                }
            }
            else if (Parent is null)
            {
                destination.ToTree = null;
                if (!EndDestinations.Contains(destination.Instruction))
                {
                    Array.Resize(ref EndDestinations, EndDestinations.Length + 1);
                    EndDestinations[EndDestinations.Length - 1] = destination.Instruction;
                }
            }
            else
            {
                IsOnlyRelay = true;
                Parent.Register(destination);
            }
        }
    }
}
