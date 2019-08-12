using System;
using System.Collections.Generic;
using Mono.Cecil;
using UnityEngine;

namespace UniEnumExtension
{
    public sealed class EnumsReplacer : IEnumsReplacer
    {
        private readonly IMethodVisitor[] methodVisitors;

        public EnumsReplacer(IMethodVisitor[] methodVisitors)
        {
            if (methodVisitors.Length == 0)
            {
                this.methodVisitors = Array.Empty<IMethodVisitor>();
            }
            else
            {
                this.methodVisitors = new IMethodVisitor[methodVisitors.Length];
                Array.Copy(methodVisitors, this.methodVisitors, methodVisitors.Length);
                Array.Sort(this.methodVisitors, new VisitorComparer());
            }
        }

        private sealed class VisitorComparer : IComparer<IMethodVisitor>
        {
            public int Compare(IMethodVisitor x, IMethodVisitor y) => x.Order.CompareTo(y.Order);
        }

        public void Replace(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition)
        {
            for (var index = moduleDefinition.Types.Count - 1; index >= 0; index--)
            {
                var type = moduleDefinition.Types[index];
                Replace(moduleDefinition, systemModuleDefinition, type);
            }
        }

        public void Replace(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition, TypeDefinition rootTypeDefinition)
        {
            if (rootTypeDefinition.IsInterface)
            {
                return;
            }
            if (rootTypeDefinition.HasNestedTypes)
            {
                for (var index = rootTypeDefinition.NestedTypes.Count - 1; index >= 0; index--)
                {
                    var type = rootTypeDefinition.NestedTypes[index];
                    Replace(moduleDefinition, systemModuleDefinition, type);
                }
            }
            if (!rootTypeDefinition.HasMethods) return;
            for (var index = rootTypeDefinition.Methods.Count - 1; index >= 0; index--)
            {
                var method = rootTypeDefinition.Methods[index];
                Replace(moduleDefinition, systemModuleDefinition, method);
            }
        }

        public void Replace(ModuleDefinition moduleDefinition, ModuleDefinition systemModuleDefinition, MethodDefinition rootMethodDefinition)
        {
            if (rootMethodDefinition.IsAbstract) return;
            var body = rootMethodDefinition.Body;
            var instructions = body?.Instructions;
            if ((instructions?.Count ?? 0) == 0)
            {
                return;
            }
            var processor = body.GetILProcessor();
            for (var currentInstruction = instructions[0]; !(currentInstruction is null); currentInstruction = currentInstruction.Next)
            {
                foreach (var visitor in methodVisitors)
                {
                    if (currentInstruction.OpCode != visitor.TargetOpCode)
                    {
                        continue;
                    }
                    visitor.Rewrite(moduleDefinition, systemModuleDefinition, processor, ref currentInstruction);
                }
            }
        }
    }
}
