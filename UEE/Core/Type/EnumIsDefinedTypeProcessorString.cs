using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UniEnumExtension
{
    public sealed class EnumIsDefinedTypeProcessorString
        : ITypeProcessor
    {
        private readonly bool isLittleEndian;
        public byte Stage => 0;

        public EnumIsDefinedTypeProcessorString(bool isLittleEndian)
        {
            this.isLittleEndian = isLittleEndian;
        }

        public void Process(ModuleDefinition systemModuleDefinition, TypeDefinition typeDefinition)
        {
            const string name = "IsDefinedString";
            if (!typeDefinition.IsEnum || typeDefinition.Methods.Any(x => x.Name == name))
            {
                return;
            }
            var module = typeDefinition.Module;
            var parameterDefinition = new ParameterDefinition("name", ParameterAttributes.None, module.TypeSystem.String);
            var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, module.TypeSystem.Boolean)
            {
                AggressiveInlining = true,
                Parameters = { parameterDefinition }
            };
            typeDefinition.Methods.Add(method);

            var stringTypeDefinition = systemModuleDefinition.GetType("System", "String");

            var count = typeDefinition.Fields.Count - 1;
            var processor = method.Body.GetILProcessor();
            var systemStringEqualsMethodReference = module.ImportReference(stringTypeDefinition.Methods.First(x => x.FullName == "System.Boolean System.String::Equals(System.String)"));
            switch (count)
            {
                case 0:
                    processor.LdC(false).Ret();
                    return;
                case 1:
                    processor
                        .LdStr(typeDefinition.Fields[1].Name)
                        .LdArg(0)
                        .Call(systemStringEqualsMethodReference)
                        .Ret();
                    return;
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                    var instruction = Instruction.Create(OpCodes.Ldc_I4_1);
                    for (var i = 1; i <= count; i++)
                    {
                        processor
                            .LdStr(typeDefinition.Fields[i].Name)
                            .LdArg(0)
                            .Call(systemStringEqualsMethodReference)
                            .BrTrueS(instruction);
                    }
                    processor.LdC(false)
                        .Ret()
                        .Add(instruction)
                        .Ret();
                    return;
                default:
                    MoreThan6(systemModuleDefinition, typeDefinition, module, stringTypeDefinition, processor, systemStringEqualsMethodReference, method);
                    return;
            }
        }

        private void MoreThan6(ModuleDefinition systemModuleDefinition, TypeDefinition typeDefinition, ModuleDefinition module, TypeDefinition stringTypeDefinition, ILProcessor processor, MethodReference systemStringEqualsMethodReference, MethodDefinition method)
        {
            (int Length, string[] names)[] tuples = typeDefinition.Fields.Skip(1).GroupBy(x => x.Name.Length, x => x.Name).OrderBy(x => x.Key).Select(x => (x.Key, x.ToArray())).ToArray();

            {
                var systemStringGetLengthMethodReference = module.ImportReference(stringTypeDefinition.Methods.Single(x => x.FullName == "System.Int32 System.String::get_Length()"));
                var ifNotNullArgument = Instruction.Create(OpCodes.Callvirt, systemStringGetLengthMethodReference);

                processor
                    .LdArg(0)
                    .Dup()
                    .BrTrueS(ifNotNullArgument)
                    .Pop()
                    .LdC(false)
                    .Ret()
                    .Add(ifNotNullArgument);
            }

            var systemRuntimeHelpersGetOffsetMethodReference = module.ImportReference(systemModuleDefinition.GetType("System.Runtime.CompilerServices", "RuntimeHelpers").Methods.First(x => x.Name == "get_OffsetToStringData"));
            if (tuples.Length == 1)
            {
                method.Body.Variables.Add(new VariableDefinition(new PointerType(module.TypeSystem.Char)));
                var enumerable = ProcessTrees(NameTree.Create(tuples[0].names, 0, tuples[0].Length, isLittleEndian), out _);
                var ifSameLength = Instruction.Create(OpCodes.Ldarg_0);
                processor
                    .LdC(tuples[0].Length)
                    .BeqS(ifSameLength)
                    .LdC(false)
                    .Ret()
                    .Add(ifSameLength)
                    .ConvIntPtr()
                    .Call(systemRuntimeHelpersGetOffsetMethodReference)
                    .Add()
                    .StLoc(0)
                    .AddRange(enumerable);
                return;
            }
            var length = tuples[tuples.Length - 1].Length - tuples[0].Length + 1;
            if (length == tuples.Length)
            {
                Continuous(module, processor, systemStringEqualsMethodReference, method, tuples, systemRuntimeHelpersGetOffsetMethodReference);
            }
            Discontinuous(module, processor, systemStringEqualsMethodReference, method, tuples, systemRuntimeHelpersGetOffsetMethodReference, length);
        }

        private void Discontinuous(ModuleDefinition module, ILProcessor processor, MethodReference systemStringEqualsMethodReference, MethodDefinition method, (int Length, string[] names)[] tuples, MethodReference systemRuntimeHelpersGetOffsetMethodReference, int length)
        {
            var switchInstructions = new Instruction[length];
            var fail = Instruction.Create(OpCodes.Ldc_I4_0);
            for (var i = 0; i < switchInstructions.Length; i++)
            {
                switchInstructions[i] = fail;
            }
            processor
                .Sub(tuples[0].Length)
                .Switch<int>(switchInstructions)
                .Add(fail)
                .Ret();
            var shouldBePinned = false;
            for (var i = 0; i < tuples.Length; i++)
            {
                processor.AddRange(MakeEachLengthInstructions(tuples[i].names, systemStringEqualsMethodReference, systemRuntimeHelpersGetOffsetMethodReference, out switchInstructions[tuples[i].Length - tuples[0].Length], out var pinned));
                shouldBePinned |= pinned;
            }
            if (!shouldBePinned) return;
            var variableDefinitions = method.Body.Variables;
            variableDefinitions.Add(new VariableDefinition(new PointerType(module.TypeSystem.Char)));
        }

        private void Continuous(ModuleDefinition module, ILProcessor processor, MethodReference systemStringEqualsMethodReference, MethodDefinition method, (int Length, string[] names)[] tuples, MethodReference systemRuntimeHelpersGetOffsetMethodReference)
        {
            var switchInstructions = new Instruction[tuples.Length];
            processor
                .Sub(tuples[0].Length)
                .Switch<int>(switchInstructions)
                .LdC(false)
                .Ret();
            var shouldBePinned = false;
            for (var i = 0; i < tuples.Length; i++)
            {
                processor.AddRange(MakeEachLengthInstructions(tuples[i].names, systemStringEqualsMethodReference, systemRuntimeHelpersGetOffsetMethodReference, out switchInstructions[i], out var pinned));
                shouldBePinned |= pinned;
            }
            if (!shouldBePinned) return;
            var variableDefinitions = method.Body.Variables;
            variableDefinitions.Add(new VariableDefinition(new PointerType(module.TypeSystem.Char)));
        }

        private IEnumerable<Instruction> MakeEachLengthInstructions(string[] names, MethodReference systemStringEqualsMethodReference, MethodReference systemRuntimeHelpersGetOffsetMethodReference, out Instruction firstInstruction, out bool needPin)
        {
            switch (names.Length)
            {
                case 0: throw new ArgumentOutOfRangeException();
                case 1:
                    firstInstruction = Instruction.Create(OpCodes.Ldstr, names[0]);
                    needPin = false;
                    return new[]
                    {
                        firstInstruction,
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Call, systemStringEqualsMethodReference),
                        Instruction.Create(OpCodes.Ret),
                    };
                case 2:
                case 3:
                    {
                        var instruction = Instruction.Create(OpCodes.Ldc_I4_1);
                        IEnumerable<Instruction> enumerable = new[]
                        {
                            Instruction.Create(OpCodes.Ldc_I4_0),
                            Instruction.Create(OpCodes.Ret),
                            instruction,
                            Instruction.Create(OpCodes.Ret),
                        };
                        firstInstruction = null;
                        foreach (var name in names)
                        {
                            firstInstruction = Instruction.Create(OpCodes.Ldstr, name);
                            enumerable = new[]
                            {
                                firstInstruction,
                                Instruction.Create(OpCodes.Ldarg_0),
                                Instruction.Create(OpCodes.Call, systemStringEqualsMethodReference),
                                Instruction.Create(OpCodes.Brtrue_S, instruction),
                            }.Concat(enumerable);
                        }
                        needPin = false;
                        return enumerable;
                    }
                default:
                    needPin = true;
                    firstInstruction = Instruction.Create(OpCodes.Ldarg_0);
                    IEnumerable<Instruction> instructions = new[]
                    {
                        firstInstruction,
                        Instruction.Create(OpCodes.Conv_I),
                        Instruction.Create(OpCodes.Call, systemRuntimeHelpersGetOffsetMethodReference),
                        Instruction.Create(OpCodes.Add),
                        Instruction.Create(OpCodes.Stloc_0),
                    };
                    return instructions.Concat(ProcessTrees(NameTree.Create(names, 0, names[0].Length, isLittleEndian), out _));
            }
        }

        private IEnumerable<Instruction> ProcessTrees(NameTree[] nameTrees, out Instruction first)
        {
            switch (nameTrees.Length)
            {
                case 0:
                    first = null;
                    return Array.Empty<Instruction>();
                case 1:
                    return ProcessOneTree(nameTrees[0], out first);
                case 2:
                    return nameTrees[0].HasChild ? ProcessTwoTree(nameTrees[0], nameTrees[1], out first) : ProcessTwoTree(nameTrees[1], nameTrees[0], out first);
                default:
                    return ProcessBinary(nameTrees, minIncludeIndex: 0, maxIncludeIndex: nameTrees.Length - 1, out first);
            }
        }

        private IEnumerable<Instruction> ProcessBinary(NameTree[] nameTrees, int minIncludeIndex, int maxIncludeIndex, out Instruction first)
        {
            switch (maxIncludeIndex - minIncludeIndex)
            {
                case 0:
                    return ProcessOneTree(nameTrees[maxIncludeIndex], out first);
                case 1:
                    return ProcessTwoTree(nameTrees[minIncludeIndex], nameTrees[maxIncludeIndex], out first);
                default:
                    var index = (maxIncludeIndex + minIncludeIndex) >> 1;
                    var tree = nameTrees[index];
                    first = Instruction.Create(OpCodes.Ldloc_0);
                    switch (tree.Value.Length)
                    {
                        default: throw new ArgumentOutOfRangeException();
                        case 1:
                            {
                                var lessEnumerable = ProcessBinary(nameTrees, minIncludeIndex, index - 1, out var lessFirst);
                                var greaterEnumerable = ProcessBinary(nameTrees, index + 1, maxIncludeIndex, out _);
                                var fail = InstructionUtility.LoadConstant(tree.Value.CVal0);
                                return new[]
                                {
                                    first,
                                    Instruction.Create(OpCodes.Ldind_I2),
                                    Instruction.Create(OpCodes.Dup),
                                    InstructionUtility.LoadConstant(tree.Value.CVal0),
                                    Instruction.Create(OpCodes.Bne_Un_S, fail),
                                    Instruction.Create(OpCodes.Pop),
                                    Instruction.Create(OpCodes.Ldc_I4_1),
                                    Instruction.Create(OpCodes.Ret),
                                    fail,
                                    Instruction.Create(OpCodes.Blt, lessFirst),
                                }.Concat(greaterEnumerable).Concat(lessEnumerable);
                            }
                        case 2:
                            {
                                var lessEnumerable = ProcessBinary(nameTrees, minIncludeIndex, index - 1, out var lessFirst);
                                var greaterEnumerable = ProcessBinary(nameTrees, index + 1, maxIncludeIndex, out _);
                                var fail = InstructionUtility.LoadConstant(tree.Value.IVal0);
                                return new[]
                                {
                                    first,
                                    Instruction.Create(OpCodes.Ldind_I4),
                                    Instruction.Create(OpCodes.Dup),
                                    InstructionUtility.LoadConstant(tree.Value.IVal0),
                                    Instruction.Create(OpCodes.Bne_Un_S, fail),
                                    Instruction.Create(OpCodes.Pop),
                                    Instruction.Create(OpCodes.Ldc_I4_1),
                                    Instruction.Create(OpCodes.Ret),
                                    fail,
                                    Instruction.Create(OpCodes.Blt, lessFirst),
                                }.Concat(greaterEnumerable).Concat(lessEnumerable);
                            }
                        case 3:
                            {
                                var same = Instruction.Create(OpCodes.Ldc_I4_4);
                                var notSame = InstructionUtility.LoadConstant(tree.Value.CVal2);
                                var lessEnumerable = ProcessBinary(nameTrees, minIncludeIndex, index - 1, out var less);
                                var greaterEnumerable = ProcessBinary(nameTrees, index + 1, maxIncludeIndex, out var greater);
                                return new[]
                                {
                                    first,
                                    Instruction.Create(OpCodes.Dup),
                                    Instruction.Create(OpCodes.Ldind_I4),
                                    InstructionUtility.LoadConstant(tree.Value.IVal0),
                                    Instruction.Create(OpCodes.Beq_S, same),
                                    Instruction.Create(OpCodes.Ldind_I4),
                                    InstructionUtility.LoadConstant(tree.Value.IVal0),
                                    Instruction.Create(OpCodes.Ble_S, less),
                                    Instruction.Create(OpCodes.Br, greater),
                                    same,
                                    Instruction.Create(OpCodes.Add),
                                    Instruction.Create(OpCodes.Ldind_I2),
                                    Instruction.Create(OpCodes.Dup),
                                    InstructionUtility.LoadConstant(tree.Value.CVal2),
                                    Instruction.Create(OpCodes.Bne_Un_S, notSame),
                                    Instruction.Create(OpCodes.Pop),
                                    Instruction.Create(OpCodes.Ldc_I4_1),
                                    Instruction.Create(OpCodes.Ret),
                                    notSame,
                                    Instruction.Create(OpCodes.Ble_S, less),
                                    Instruction.Create(OpCodes.Br, greater),
                                }.Concat(lessEnumerable).Concat(greaterEnumerable);
                            }
                        case 4:
                            if (tree.HasChild)
                            {
                                var lessEnumerable = ProcessBinary(nameTrees, minIncludeIndex, index - 1, out var lessFirst);
                                var greaterEnumerable = ProcessBinary(nameTrees, index + 1, maxIncludeIndex, out var greatFirst);
                                var childEnumerable = ProcessTrees(tree.Trees, out _);
                                var same = Instruction.Create(OpCodes.Ldc_I4_8);
                                return new[]
                                {
                                    first,
                                    Instruction.Create(OpCodes.Dup),
                                    Instruction.Create(OpCodes.Ldind_I8),
                                    Instruction.Create(OpCodes.Ldc_I8, tree.Value.LVal0),
                                    Instruction.Create(OpCodes.Beq_S, same),
                                    Instruction.Create(OpCodes.Ldind_I8),
                                    Instruction.Create(OpCodes.Ldc_I8, tree.Value.LVal0),
                                    Instruction.Create(OpCodes.Blt, lessFirst),
                                    Instruction.Create(OpCodes.Br, greatFirst),
                                    same,
                                    Instruction.Create(OpCodes.Add),
                                    Instruction.Create(OpCodes.Stloc_0),
                                }.Concat(childEnumerable).Concat(greaterEnumerable).Concat(lessEnumerable);
                            }
                            else
                            {
                                var lessEnumerable = ProcessBinary(nameTrees, minIncludeIndex, index - 1, out var lessFirst);
                                var greaterEnumerable = ProcessBinary(nameTrees, index + 1, maxIncludeIndex, out _);
                                var fail = Instruction.Create(OpCodes.Ldc_I8, tree.Value.LVal0);
                                return new[]
                                {
                                    first,
                                    Instruction.Create(OpCodes.Ldind_I8),
                                    Instruction.Create(OpCodes.Dup),
                                    Instruction.Create(OpCodes.Ldc_I8, tree.Value.LVal0),
                                    Instruction.Create(OpCodes.Bne_Un_S, fail),
                                    Instruction.Create(OpCodes.Pop),
                                    Instruction.Create(OpCodes.Ldc_I4_1),
                                    Instruction.Create(OpCodes.Ret),
                                    fail,
                                    Instruction.Create(OpCodes.Blt, lessFirst),
                                }.Concat(greaterEnumerable).Concat(lessEnumerable);
                            }
                    }
            }
        }

        private IEnumerable<Instruction> ProcessTwoTree(NameTree nameTree0, NameTree nameTree1, out Instruction first)
        {
            var nameLeaf0 = nameTree0.Value;
            var nameLeaf1 = nameTree1.Value;
            first = Instruction.Create(OpCodes.Ldloc_0);
            switch (nameLeaf0.Length)
            {
                case 1:
                    {
                        var fail = InstructionUtility.LoadConstant(nameLeaf1.CVal0);
                        return new[]
                        {
                            first,
                            Instruction.Create(OpCodes.Ldind_I2),
                            Instruction.Create(OpCodes.Dup),
                            InstructionUtility.LoadConstant(nameLeaf0.CVal0),
                            Instruction.Create(OpCodes.Bne_Un_S, fail),
                            Instruction.Create(OpCodes.Pop),
                            Instruction.Create(OpCodes.Ldc_I4_0),
                            Instruction.Create(OpCodes.Ret),
                            fail,
                            Instruction.Create(OpCodes.Ceq),
                            Instruction.Create(OpCodes.Ret),
                        };
                    }
                case 2:
                    {
                        var fail = InstructionUtility.LoadConstant(nameLeaf1.IVal0);
                        return new[]
                        {
                            first,
                            Instruction.Create(OpCodes.Ldind_I4),
                            Instruction.Create(OpCodes.Dup),
                            InstructionUtility.LoadConstant(nameLeaf0.IVal0),
                            Instruction.Create(OpCodes.Bne_Un_S, fail),
                            Instruction.Create(OpCodes.Pop),
                            Instruction.Create(OpCodes.Ldc_I4_0),
                            Instruction.Create(OpCodes.Ret),
                            fail,
                            Instruction.Create(OpCodes.Ceq),
                            Instruction.Create(OpCodes.Ret),
                        };
                    }
                case 3:
                    {
                        var success = Instruction.Create(OpCodes.Ldloc_0);
                        var fail = InstructionUtility.LoadConstant(nameLeaf1.IVal0);
                        return new[]
                        {
                            first,
                            Instruction.Create(OpCodes.Ldind_I4),
                            InstructionUtility.LoadConstant(nameLeaf0.IVal0),
                            Instruction.Create(OpCodes.Dup),
                            Instruction.Create(OpCodes.Bne_Un_S, fail),
                                Instruction.Create(OpCodes.Pop),
                                Instruction.Create(OpCodes.Ldloc_0),
                                Instruction.Create(OpCodes.Ldc_I4_4),
                                Instruction.Create(OpCodes.Add),
                                Instruction.Create(OpCodes.Ldind_I2),
                                InstructionUtility.LoadConstant(nameLeaf0.CVal2),
                                Instruction.Create(OpCodes.Ceq),
                                Instruction.Create(OpCodes.Ret),
                            fail,
                            Instruction.Create(OpCodes.Beq_S, success),
                                Instruction.Create(OpCodes.Ldc_I4_0),
                                Instruction.Create(OpCodes.Ret),
                            success,
                            Instruction.Create(OpCodes.Ldc_I4_4),
                            Instruction.Create(OpCodes.Ldind_I2),
                            InstructionUtility.LoadConstant(nameLeaf1.CVal2),
                            Instruction.Create(OpCodes.Ceq),
                            Instruction.Create(OpCodes.Ret),
                        };
                    }
                case 4:
                    return nameTree0.HasChild
                        ? nameTree1.HasChild
                            ? ProcessTwoTreeBothHaveChild(nameTree0, nameTree1, first)
                            : ProcessTwoTreeHasChild(nameTree0, first, noChildLeaf: nameLeaf1)
                        : nameTree1.HasChild
                            ? ProcessTwoTreeHasChild(nameTree1, first, noChildLeaf: nameLeaf0)
                            : ProcessTwoTreeNoChild(first, nameLeaf1, nameLeaf0);
                default: throw new ArgumentOutOfRangeException(nameLeaf0.Length.ToString());
            }
        }

        private static IEnumerable<Instruction> ProcessTwoTreeNoChild(Instruction first, NameLeaf nameLeaf1, NameLeaf nameLeaf0)
        {
            var fail = Instruction.Create(OpCodes.Ldc_I8, nameLeaf1.LVal0);
            return new[]
            {
                first,
                Instruction.Create(OpCodes.Ldind_I8),
                Instruction.Create(OpCodes.Dup),
                Instruction.Create(OpCodes.Ldc_I8, nameLeaf0.LVal0),
                Instruction.Create(OpCodes.Bne_Un_S, fail),
                Instruction.Create(OpCodes.Pop),
                Instruction.Create(OpCodes.Ldc_I4_0),
                Instruction.Create(OpCodes.Ret),
                fail,
                Instruction.Create(OpCodes.Ceq),
                Instruction.Create(OpCodes.Ret),
            };
        }

        private IEnumerable<Instruction> ProcessTwoTreeHasChild(NameTree childHolderTree, Instruction first, NameLeaf noChildLeaf)
        {
            var fail = Instruction.Create(OpCodes.Ldc_I8, childHolderTree.Value.LVal0);
            var success = Instruction.Create(OpCodes.Ldloc_0);
            return new[]
            {
                first,
                Instruction.Create(OpCodes.Ldind_I8),
                Instruction.Create(OpCodes.Dup),
                Instruction.Create(OpCodes.Ldc_I8, noChildLeaf.LVal0),
                Instruction.Create(OpCodes.Bne_Un_S, fail),
                Instruction.Create(OpCodes.Pop),
                Instruction.Create(OpCodes.Ldc_I4_1),
                Instruction.Create(OpCodes.Ret),
                fail,
                Instruction.Create(OpCodes.Beq_S, success),
                Instruction.Create(OpCodes.Ldc_I4_0),
                Instruction.Create(OpCodes.Ret),
                success,
                Instruction.Create(OpCodes.Ldc_I4_8),
                Instruction.Create(OpCodes.Add),
                Instruction.Create(OpCodes.Stloc_0),
            }.Concat(ProcessTrees(childHolderTree.Trees, out _));
        }

        private IEnumerable<Instruction> ProcessTwoTreeBothHaveChild(NameTree nameTree0, NameTree nameTree1, Instruction first)
        {
            var success0 = Instruction.Create(OpCodes.Pop);
            var success1 = Instruction.Create(OpCodes.Ldloc_0);
            var enumerable0 = ProcessTrees(nameTree0.Trees, out _);
            var enumerable1 = ProcessTrees(nameTree1.Trees, out _);
            return new[]
            {
                first,
                Instruction.Create(OpCodes.Ldind_I8),
                Instruction.Create(OpCodes.Dup),
                Instruction.Create(OpCodes.Ldc_I8, nameTree0.Value.LVal0),
                Instruction.Create(OpCodes.Beq_S, success0),
                Instruction.Create(OpCodes.Ldc_I8, nameTree1.Value.LVal0),
                Instruction.Create(OpCodes.Beq, success1),
                Instruction.Create(OpCodes.Ldc_I4_0),
                Instruction.Create(OpCodes.Ret),
                success0,
                Instruction.Create(OpCodes.Ldloc_0),
                Instruction.Create(OpCodes.Ldc_I4_8),
                Instruction.Create(OpCodes.Add),
                Instruction.Create(OpCodes.Stloc_0),
            }
            .Concat(enumerable0)
            .Concat(new[]
            {
                success1,
                Instruction.Create(OpCodes.Ldc_I4_8),
                Instruction.Create(OpCodes.Add),
                Instruction.Create(OpCodes.Stloc_0),
            })
            .Concat(enumerable1);
        }

        private IEnumerable<Instruction> ProcessOneTree(NameTree nameTree, out Instruction first)
        {
            var nameLeaf = nameTree.Value;
            first = Instruction.Create(OpCodes.Ldloc_0);
            switch (nameLeaf.Length)
            {
                case 1:
                    return new[]
                    {
                        first,
                        Instruction.Create(OpCodes.Ldind_U2),
                        InstructionUtility.LoadConstant(nameLeaf.CVal0),
                        Instruction.Create(OpCodes.Ceq),
                        Instruction.Create(OpCodes.Ret),
                    };
                case 2:
                    return new[]
                    {
                        first,
                        Instruction.Create(OpCodes.Ldind_U4),
                        InstructionUtility.LoadConstant(nameLeaf.IVal0),
                        Instruction.Create(OpCodes.Ceq),
                        Instruction.Create(OpCodes.Ret),
                    };
                case 3:
                    var success = Instruction.Create(OpCodes.Ldloc_0);
                    return new[]
                    {
                        first,
                        Instruction.Create(OpCodes.Ldind_U4),
                        InstructionUtility.LoadConstant(nameLeaf.IVal0),
                        Instruction.Create(OpCodes.Beq_S, success),
                        Instruction.Create(OpCodes.Ldc_I4_0),
                        Instruction.Create(OpCodes.Ret),
                        success,
                        Instruction.Create(OpCodes.Ldc_I4_4),
                        Instruction.Create(OpCodes.Add),
                        Instruction.Create(OpCodes.Ldind_U2),
                        InstructionUtility.LoadConstant(nameLeaf.CVal2),
                        Instruction.Create(OpCodes.Ceq),
                        Instruction.Create(OpCodes.Ret),
                    };
                case 4:
                    if (!nameTree.HasChild)
                    {
                        return new[]
                        {
                            first,
                            Instruction.Create(OpCodes.Ldind_I8),
                            Instruction.Create(OpCodes.Ldc_I8, nameLeaf.LVal0),
                            Instruction.Create(OpCodes.Ceq),
                            Instruction.Create(OpCodes.Ret),
                        };
                    }
                    var enumerable = ProcessTrees(nameTree.Trees, out var successFirst);
                    return new[]
                    {
                        first,
                        Instruction.Create(OpCodes.Dup),
                        Instruction.Create(OpCodes.Ldc_I4_8),
                        Instruction.Create(OpCodes.Add),
                        Instruction.Create(OpCodes.Stloc_0),
                        Instruction.Create(OpCodes.Ldind_I8),
                        Instruction.Create(OpCodes.Ldc_I8, nameLeaf.LVal0),
                        Instruction.Create(OpCodes.Beq_S, successFirst),
                        Instruction.Create(OpCodes.Ldc_I4_0),
                        Instruction.Create(OpCodes.Ret),
                    }.Concat(enumerable);
                default: throw new ArgumentOutOfRangeException(nameLeaf.Length.ToString());
            }
        }
    }
}
