using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
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
                Debug.Log(exceptionHandlerTree);
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
            if (!(exceptionHandlerTree.Handler is null))
            {
                ProcessSimpleTree(processor, exceptionHandlerTree);
            }
        }

        private static void ProcessSimpleTree(ScopedProcessor processor, ExceptionHandlerTree exceptionHandlerTree)
        {
            var handler = exceptionHandlerTree.Handler;
            var variables = processor.Processor.Body.Variables;
            for (Instruction iterator = handler.TryStart, end = handler.TryEnd; !(iterator is null) && !ReferenceEquals(iterator, end); iterator = iterator.Next)
            {
                if (iterator.OpCode != OpCodes.Leave_S && iterator.OpCode != OpCodes.Leave) continue;
                var destination = (Instruction)iterator.Operand;
                if (ReferenceEquals(destination, iterator.Next))
                {
                    var tmp = iterator.Previous;
                    processor.Remove(iterator);
                    iterator = tmp;
                    continue;
                }
                var (constant, variableIndex) = exceptionHandlerTree.FindIndex(destination);
                var brHandler = Instruction.Create(OpCodes.Br, handler.HandlerStart);
                if (variableIndex == -1)
                {
                    processor.Replace(iterator, brHandler);
                    iterator = brHandler;
                    continue;
                }
                var ldConstant = InstructionUtility.LoadConstant(constant);
                var stTarget = variables.StoreLocal(variableIndex);
                processor
                    .Replace(iterator, ldConstant)
                    .InsertAfter(ldConstant, stTarget);
                iterator = stTarget;
                if (variableIndex != exceptionHandlerTree.VariableIndex)
                {
                    var minus = InstructionUtility.LoadConstant(-1);
                    var stCurrent = variables.StoreLocal(exceptionHandlerTree.VariableIndex);
                    processor
                        .InsertAfter(stTarget, minus)
                        .InsertAfter(minus, stCurrent);
                    iterator = stCurrent;
                }
                processor.InsertAfter(iterator, brHandler);
                iterator = brHandler;
            }

            for (Instruction iterator = handler.HandlerStart, end = handler.HandlerEnd; !(iterator is null) && !ReferenceEquals(iterator, end); iterator = iterator.Next)
            {
                if (iterator.OpCode != OpCodes.Endfinally) continue;
                if (exceptionHandlerTree.VariableIndex == -1)
                {
                    if (exceptionHandlerTree.EndDestinations.Count == 0 || ReferenceEquals(exceptionHandlerTree.EndDestinations[0], iterator.Next))
                    {
                        processor.Remove(iterator);
                        break;
                    }
                    var br = Instruction.Create(OpCodes.Br, exceptionHandlerTree.EndDestinations[0]);
                    processor.Replace(iterator, br);
                    break;
                }
                var ld = variables.LoadLocal(exceptionHandlerTree.VariableIndex);
                var @switch = Instruction.Create(OpCodes.Switch, exceptionHandlerTree.ToArray());

                processor
                    .InsertBefore(iterator, ld)
                    .Replace(iterator, @switch);
                if (!(exceptionHandlerTree.Parent is null))
                {
                    var brParent = Instruction.Create(OpCodes.Br, exceptionHandlerTree.Parent.Handler.HandlerStart);
                    processor.InsertAfter(@switch, brParent);
                }
                break;
            }
        }

        private static void ProcessSeparatedHandler(ScopedProcessor processor, ExceptionHandlerTree exceptionHandlerTree)
        {
            ProcessEachHandlerDestinationMoreThanOne(processor, exceptionHandlerTree);
        }


        private static void ProcessEachHandlerDestinationMoreThanOne(ScopedProcessor processor, ExceptionHandlerTree exceptionHandlerTree)
        {
            var variables = processor.Processor.Body.Variables;
            var variableIndex = variables.Count;
            variables.Add(new VariableDefinition(processor.Processor.Body.Method.Module.TypeSystem.Int32));
            var leaveInstructions = new List<Instruction>();
            for (Instruction iterator = exceptionHandlerTree.Handler.TryStart, end = exceptionHandlerTree.Handler.TryEnd; !(iterator is null) && !ReferenceEquals(iterator, end); iterator = iterator.Next)
            {
                switch (iterator.OpCode.Code)
                {
                    case Code.Leave:
                    case Code.Leave_S:
                        var destinationInstruction = (Instruction)iterator.Operand;
                        var parent = exceptionHandlerTree.Parent;

                        var ld = InstructionUtility.LoadConstant(leaveInstructions.Count);
                        var st = variables.StoreLocal(variableIndex);
                        var br = Instruction.Create(OpCodes.Br, exceptionHandlerTree.Handler.HandlerStart);
                        leaveInstructions.Add(destinationInstruction);
                        processor
                            .Replace(iterator, ld)
                            .InsertAfter(ld, st)
                            .InsertAfter(st, br);
                        iterator = br;
                        continue;
                }
            }
            for (Instruction iterator = exceptionHandlerTree.Handler.HandlerStart, end = exceptionHandlerTree.Handler.HandlerEnd; !(iterator is null) && !ReferenceEquals(iterator, end); iterator = iterator.Next)
            {
                if (iterator.OpCode.Code != Code.Endfinally) continue;
                var ld = variables.LoadLocal(variableIndex);
                var @switch = Instruction.Create(OpCodes.Switch, leaveInstructions.ToArray());
                processor
                    .Replace(iterator, ld)
                    .InsertAfter(ld, @switch);
                iterator = @switch;
            }
        }
    }
}
