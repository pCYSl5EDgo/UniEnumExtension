using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace UniEnumExtension
{
    public sealed class ExceptionHandlerTree
    {
        public ExceptionHandlerTree Parent;
        public readonly ExceptionHandler Handler;
        public readonly List<ExceptionHandlerTree> Trees;
        public readonly List<(Instruction fromInstruction, Instruction toInstruction, ExceptionHandlerTree toTree)> LeaveTupleList;

        public Instruction[] EndDestinations;
        public bool IsRelay;

        public int TotalDepth => 1 + (Trees.Count != 0 ? Trees.Max(x => x.TotalDepth) : 0);

        public int CurrentDepth
        {
            get
            {
                var answer = 0;
                for (var itr = Parent; !(itr is null); itr = itr.Parent)
                {
                    ++answer;
                }
                return answer;
            }
        }

        private void AddDestination(Instruction instruction)
        {
            Array.Resize(ref EndDestinations, EndDestinations.Length + 1);
            EndDestinations[EndDestinations.Length - 1] = instruction;
        }

        public bool HasChild => Trees.Count != 0;

        public override string ToString()
        {
            var buf = new StringBuilder();
            Append(buf, 0);
            return buf.ToString();
        }

        private void Append(StringBuilder buf, int indent)
        {
            buf.AppendLine().Append(' ', indent << 2);
            buf.Append(IsRelay).Append(" : ").Append(EndDestinations.Length).Append(" with ").Append(Trees.Count);
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
            IsRelay = false;
            LeaveTupleList = new List<(Instruction, Instruction, ExceptionHandlerTree)>();
        }

        private ExceptionHandlerTree(ExceptionHandler handler) : this(handler, new List<ExceptionHandlerTree>()) { }

        public static List<ExceptionHandlerTree> Create(MethodBody body)
        {
            var handlers = body.ExceptionHandlers;
            if (handlers.Count == 0) throw new ArgumentOutOfRangeException();
            var answer = CreateTreeRelationship(body.Instructions, handlers);
            foreach (var tree in answer.Trees)
            {
                tree.Parent = default;
                tree.CleanUpDestinations();
            }
            return answer.Trees;
        }

        private void CleanUpDestinations()
        {
            EndDestinations = new HashSet<Instruction>(EndDestinations).ToArray();
            foreach (var tree in Trees)
            {
                tree.CleanUpDestinations();
            }
        }

        private static ExceptionHandlerTree CreateTreeRelationship(Collection<Instruction> instructions, Collection<ExceptionHandler> handlers)
        {
            var stack = new Stack<ExceptionHandlerTree>();
            var answer = new ExceptionHandlerTree(null);
            stack.Push(answer);
            var tuples = new List<(Instruction, ExceptionHandlerTree parentTree)>(instructions.Count);

            foreach (var instruction in instructions)
            {
                var oldTop = stack.Peek();
                if (!(oldTop.Handler is null) && ReferenceEquals(oldTop.Handler.TryEnd, instruction))
                {
                    stack.Pop();
                }
                var handler = handlers.FirstOrDefault(x => ReferenceEquals(instruction, x.TryStart));
                if (!(handler is null))
                {
                    var exceptionHandlerTree = new ExceptionHandlerTree(handler);
                    exceptionHandlerTree.Parent = oldTop;
                    oldTop.Trees.Add(exceptionHandlerTree);
                    stack.Push(exceptionHandlerTree);
                }
                tuples.Add((instruction, stack.Peek()));
            }

            for (int i = 0; i < tuples.Count; i++)
            {
                var (it, itTree) = tuples[i];
                if (it.OpCode != OpCodes.Leave_S && it.OpCode != OpCodes.Leave) continue;
                var destination = (Instruction)it.Operand;
                var destinationTree = tuples.First(x => ReferenceEquals(x.Item1, destination)).Item2;
                var destinationOwnerTree = GetChildOfAWhichIsAncestorOfB(destinationTree, itTree);
                destinationOwnerTree.AddDestination(destination);
                itTree.LeaveTupleList.Add((it, destination, destinationOwnerTree));
                ForceFromTreeUntilToTreeToBeRelay(itTree, destinationOwnerTree);
            }
            return answer;
        }

        private static void ForceFromTreeUntilToTreeToBeRelay(ExceptionHandlerTree from, ExceptionHandlerTree until)
        {
            while (!ReferenceEquals(from, until))
            {
                from.IsRelay = true;
                from = from.Parent;
            }
        }

        private static ExceptionHandlerTree GetChildOfAWhichIsAncestorOfB(ExceptionHandlerTree a, ExceptionHandlerTree b)
        {
            var parent = b.Parent;
            while (!ReferenceEquals(parent, a))
            {
                b = parent;
                parent = b.Parent;
            }
            return b;
        }
    }
}
