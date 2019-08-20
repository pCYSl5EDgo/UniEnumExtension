using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using UnityEngine;

namespace UniEnumExtension
{
    public class BurstForeachTypeProcessor : ITypeProcessor
    {
        public byte Stage => 64;
        public void Process(ModuleDefinition systemModuleDefinition, TypeDefinition typeDefinition)
        {
            if (typeDefinition.Name != "Job") return;
            if (!typeDefinition.HasCustomAttributes || typeDefinition.HasGenericParameters || typeDefinition.IsGenericInstance || !typeDefinition.IsValueType || typeDefinition.IsEnum || typeDefinition.CustomAttributes.All(x => x.AttributeType.FullName != "Unity.Burst.BurstCompileAttribute"))
            {
                return;
            }
            foreach (var methodDefinition in typeDefinition.Methods.Where(x => x.HasBody && x.Body.HasExceptionHandlers && (!x.HasCustomAttributes || x.CustomAttributes.All(y => y.AttributeType.FullName != "Unity.Burst.BurstDiscardAttribute"))))
            {
                ProcessEachMethod(methodDefinition);
            }
        }

        private static void ProcessEachMethod(MethodDefinition methodDefinition)
        {
            var body = methodDefinition.Body;
            var handlers = body.ExceptionHandlers;
            if (handlers.Any(x => x.HandlerType != ExceptionHandlerType.Finally))
            {
                Debug.LogWarning(methodDefinition.FullName + " contains not finally clause!");
                return;
            }
            using (ScopedProcessor processor = body.GetILProcessor())
            {
                processor.Simplify();
                var exceptionHandlerTrees = ExceptionHandlerTree.Create(body);
                var totalDepth = exceptionHandlerTrees.Max(x => x.TotalDepth);
                for (var i = 0; i < totalDepth; i++)
                {
                    body.Variables.Add(new VariableDefinition(methodDefinition.Module.TypeSystem.Int32));
                }
                foreach (var exceptionHandlerTree in exceptionHandlerTrees)
                {
                    ProcessComplexTree(processor, exceptionHandlerTree);
                }
            }
            handlers.Clear();
        }

        private static void ProcessComplexTree(ScopedProcessor processor, ExceptionHandlerTree exceptionHandlerTree)
        {
            foreach (var tree in exceptionHandlerTree.Trees)
            {
                ProcessComplexTree(processor, tree);
            }
            if (exceptionHandlerTree.Handler is null)
            {
                return;
            }
            ProcessSimpleTree(processor, exceptionHandlerTree);
        }

        private static void ProcessSimpleTree(ScopedProcessor processor, ExceptionHandlerTree exceptionHandlerTree)
        {
            ProcessHandler(processor, exceptionHandlerTree);
            ProcessTry(processor, exceptionHandlerTree);
        }

        private static void ProcessHandler(ScopedProcessor processor, ExceptionHandlerTree exceptionHandlerTree)
        {
            var iterator = exceptionHandlerTree.Handler.HandlerStart;
            for (var end = exceptionHandlerTree.Handler.HandlerEnd; iterator != end; iterator = iterator.Next)
            {
                if (iterator.OpCode.Code == Code.Endfinally)
                    break;
            }

            if (iterator is null)
            {
                return;
            }

            var methodBody = processor.Processor.Body;
            var variables = methodBody.Variables;

            var currentDepth = exceptionHandlerTree.CurrentDepth;
            var ld = variables.LoadLocal(variables.Count - 1 - currentDepth);
            Instruction @switch;
            if (exceptionHandlerTree.Parent?.Handler is null)
            {
                @switch = Instruction.Create(OpCodes.Switch, exceptionHandlerTree.EndDestinations);
            }
            else
            {
                @switch = Instruction.Create(OpCodes.Switch, exceptionHandlerTree.EndDestinations.Append(exceptionHandlerTree.Parent.Handler.HandlerStart).ToArray());
            }
            processor.Replace(iterator, ld).InsertAfter(ld, @switch);
        }

        private static void ProcessTry(ScopedProcessor processor, ExceptionHandlerTree exceptionHandlerTree)
        {
            var handler = exceptionHandlerTree.Handler;
            var variables = processor.Processor.Body.Variables;
            for (Instruction iterator = handler.TryStart, end = handler.TryEnd; !(iterator is null) && !ReferenceEquals(iterator, end);)
            {
                if (iterator.OpCode != OpCodes.Leave_S && iterator.OpCode != OpCodes.Leave)
                {
                    iterator = iterator.Next;
                    continue;
                }
                iterator = ProcessLeave(processor, exceptionHandlerTree, iterator, variables, handler);
            }
        }

        private static Instruction ProcessLeave(ScopedProcessor processor, ExceptionHandlerTree exceptionHandlerTree, Instruction iterator, Collection<VariableDefinition> variables, ExceptionHandler handler)
        {
            var (_, toInstruction, tree) = exceptionHandlerTree.LeaveTupleList.First(x => ReferenceEquals(x.fromInstruction, iterator));

            var loadConstantInstruction = InstructionUtility.LoadConstant(Array.IndexOf(tree.EndDestinations, toInstruction));
            processor.Replace(iterator, loadConstantInstruction);
            var storeLocalInstruction = variables.StoreLocal(variables.Count - 1 - tree.CurrentDepth);
            processor.InsertAfter(loadConstantInstruction, storeLocalInstruction);

            var br = Instruction.Create(OpCodes.Br, exceptionHandlerTree.Handler.HandlerStart);
            processor.InsertAfter(storeLocalInstruction, br);
            var itr = exceptionHandlerTree;
            for (var currentIndex = variables.Count - 1 - itr.CurrentDepth; !ReferenceEquals(itr, tree); itr = itr.Parent, ++currentIndex)
            {
                var ldc = InstructionUtility.LoadConstant(itr.EndDestinations.Length);
                var stLoc = variables.StoreLocal(currentIndex);
                processor.InsertBefore(br, ldc).InsertBefore(br, stLoc);
            }

            return br;
        }
    }
}
