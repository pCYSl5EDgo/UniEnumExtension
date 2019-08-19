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
                var exceptionHandlerTree = ExceptionHandlerTree.Create(body);
                Debug.Log("count : " + body.ExceptionHandlers.Count + exceptionHandlerTree);
                ProcessComplexTree(processor, exceptionHandlerTree);
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

            var @switch = Instruction.Create(OpCodes.Switch, exceptionHandlerTree.EndDestinations);
            processor.Replace(iterator, @switch);
            if (exceptionHandlerTree.Parent is null) return;
            var br = Instruction.Create(OpCodes.Br, exceptionHandlerTree.Parent.Handler.HandlerStart);
            processor.InsertAfter(@switch, br);
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
            var destination = (Instruction)iterator.Operand;

            var (switchIndex, tree) = exceptionHandlerTree.FindIndex(destination);

            var br = Instruction.Create(OpCodes.Br, exceptionHandlerTree.Handler.HandlerStart);
            processor.Replace(iterator, br);
            for (var itr = exceptionHandlerTree; !ReferenceEquals(itr, tree); itr = itr.Parent)
            {
                processor
                    .InsertBefore(br, InstructionUtility.LoadConstant(itr.EndDestinations.Length));
            }
            processor
                .InsertBefore(br, InstructionUtility.LoadConstant(switchIndex));
            return br;
        }
    }
}
