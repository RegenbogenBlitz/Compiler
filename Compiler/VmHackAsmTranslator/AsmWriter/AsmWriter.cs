using FileHandling;
using VmHackAsmTranslator.Parsing;

namespace VmHackAsmTranslator.AsmWriter;

public static class AsmWriter
{
    private const int BasePointerAddress = 3;
    private const int BaseTempAddress = 5;
    private const string StackPointerAddress = "SP";
    private const int BaseStackAddress = 256;
    
    private const string SkipSubsLabel = "SKIP_SUBS";
    private const string IsTrueLabel = "IS_TRUE";
    private const string IsFalseLabel = "IS_FALSE";

    private const string EqualsSubLabel = "EQUALS_SUB";
    private const string LessThanSubLabel = "LESSTHAN_SUB";
    private const string GreaterThanSubLabel = "GREATERTHAN_SUB";
    
    private const string EqualsReturnLabel = "EQUALS_RETURN_";
    private const string LessThanReturnLabel = "LESSTHAN_RETURN_";
    private const string GreaterThanReturnLabel = "GREATERTHAN_RETURN_";
    // ReSharper disable once RedundantDefaultMemberInitializer
    private static int _comparisionReturnLabelNum = 0;
    
    private const string CallSubLabel = "CALL_SUB";
    private const string ReturnSubLabel = "RETURN_SUB";
    
    private const string ReturnAddressLabel = "RETURN_ADDRESS_";
    // ReSharper disable once RedundantDefaultMemberInitializer
    private static int _functionReturnLabelNum = 0;
        
    public static OutputFileInfo Write(string outputFileName, VmCode vmCode, bool writeWithComments)
    {
        var asmOutputs = new List<IAsmOutput>(WriteHeader());

        foreach (var command in vmCode.Commands)
        {
            switch (command)
            {
                case PushCommand pushCommand:
                    asmOutputs.Add(WritePush(pushCommand.ClassName, pushCommand.Segment, pushCommand.Index));
                    break;

                case PopCommand popCommand:
                    asmOutputs.Add(WritePop(popCommand.ClassName, popCommand.Segment, popCommand.Index, 0));
                    break;

                case ArithmeticCommand arithmeticCommand:
                {
                    switch (arithmeticCommand.ArithmeticCommandType)
                    {
                        case ArithmeticCommandType.Add:
                            asmOutputs.Add(WriteBinaryOperator("+", "Add", "+"));
                            break;
                        
                        case ArithmeticCommandType.Sub:
                            asmOutputs.Add(WriteBinaryOperator("-", "Subtract", "-"));
                            break;
                        
                        case ArithmeticCommandType.Neg:
                            asmOutputs.Add(WriteUnaryOperator("-", "Negative", "-"));
                            break;
                        
                        case ArithmeticCommandType.And:
                            asmOutputs.Add(WriteBinaryOperator("&", "And", "and"));
                            break;
                        
                        case ArithmeticCommandType.Or:
                            asmOutputs.Add(WriteBinaryOperator("|", "Or", "or"));
                            break;
                        
                        case ArithmeticCommandType.Not:
                            asmOutputs.Add(WriteUnaryOperator("!", "Not", "not "));
                            break;
                        
                        case ArithmeticCommandType.Eq:
                            asmOutputs.Add(WriteComparison("Equals", EqualsReturnLabel, EqualsSubLabel));
                            break;
                        
                        case ArithmeticCommandType.Lt:
                            asmOutputs.Add(WriteComparison("Less Than", LessThanReturnLabel, LessThanSubLabel));
                            break;
                        
                        case ArithmeticCommandType.Gt:
                            asmOutputs.Add(WriteComparison("Greater Than", GreaterThanReturnLabel, GreaterThanSubLabel));
                            break;
                        
                        default:
                            throw new InvalidOperationException("Should not be reachable");
                    }
                    
                    break;
                }

                case LabelCommand labelCommand:
                    asmOutputs.Add(new AsmCodeLine(WriteFunctionQualifiedLabel(labelCommand.FunctionName, labelCommand.Symbol)));
                    break;

                case IfGotoCommand ifGotoCommand:
                    asmOutputs.Add(WriteIfGoto(ifGotoCommand.FunctionName, ifGotoCommand.Symbol));
                    break;
                    
                case GotoCommand gotoCommand:
                    asmOutputs.Add(WriteGoto(gotoCommand.FunctionName, gotoCommand.Symbol));
                    break;

                case ReturnCommand:
                    asmOutputs.Add(WriteReturn());
                    break;

                case FunctionDeclarationCommand functionDeclarationCommand:
                    asmOutputs.Add(WriteFunctionDeclaration(
                        functionDeclarationCommand.FunctionName,
                        functionDeclarationCommand.NumLocals));
                    break;
                    
                case FunctionCallCommand functionCallCommand:
                    asmOutputs.Add(WriteFunctionCall(
                        functionCallCommand.FunctionName,
                        functionCallCommand.NumArguments));
                    break;
                
                case NonCommand:
                    // Do Nothing
                    break;
                
                default:
                    throw new InvalidOperationException("Should not be reachable");
            }
        }

        var outputLines =
            asmOutputs.SelectMany(ao =>
                writeWithComments
                    ? ao.WriteWithComments(0)
                    : ao.WriteWithoutComments());
        var output = string.Join(Environment.NewLine, outputLines);
        
        return new OutputFileInfo(outputFileName, "asm", output);
    }

    private static IEnumerable<IAsmOutput> WriteHeader() =>
        new IAsmOutput[]
        {
            new AsmCodeSection("Reusable Sub Routines", new IAsmOutput[]
            {
                new AsmCodeLine(UnconditionalJump(SkipSubsLabel, 1)),
                new AsmCodeSection("Equals",
                    new[]
                    {
                        new AsmCodeLine(WriteLabel(EqualsSubLabel)),
                        new AsmCodeLine(BinaryOperatorToD("-", "-", 2)),
                        new AsmCodeLine(string.Empty, "If D = 0 Then Goto IsTrue Else Goto IsFalse"),
                        new AsmCodeLine(ConditionalJump("JEQ", IsTrueLabel, 2)),
                        new AsmCodeLine(UnconditionalJump(IsFalseLabel, 2))
                    }),
                new AsmCodeSection("Is Less Than",
                    new[]
                    {
                        new AsmCodeLine(WriteLabel(LessThanSubLabel)),
                        new AsmCodeLine(BinaryOperatorToD("-", "-", 2)),
                        new AsmCodeLine(string.Empty, "If D < 0 Then Goto IsTrue Else Goto IsFalse"),
                        new AsmCodeLine(ConditionalJump("JLT", IsTrueLabel, 2)),
                        new AsmCodeLine(UnconditionalJump(IsFalseLabel, 2))
                    }),
                new AsmCodeSection("Is Greater Than",
                    new[]
                    {
                        new AsmCodeLine(WriteLabel(GreaterThanSubLabel)),
                        new AsmCodeLine(BinaryOperatorToD("-", "-", 2)),
                        new AsmCodeLine(string.Empty, "If D > 0 Then Goto IsTrue Else Goto IsFalse"),
                        new AsmCodeLine(ConditionalJump("JGT", IsTrueLabel, 2)),
                        new AsmCodeLine(UnconditionalJump(IsFalseLabel, 2))
                    }),
                new AsmCodeSection("ReusableComparison",
                    new[]
                    {
                        new AsmCodeSection("Is True", new[]
                        {
                            new AsmCodeLine(WriteLabel(IsTrueLabel)),
                            new AsmCodeLine(NegativeValueToD("1", 3)),
                            new AsmCodeLine(DToTopStack(3)),
                            new AsmCodeLine(LiftStack(3)),
                            new AsmCodeLine(UnconditionalJumpToAddressInMemory("R14", 3))
                        }),
                        new AsmCodeSection("Is False", new[]
                        {
                            new AsmCodeLine(WriteLabel(IsFalseLabel)),
                            new AsmCodeLine(ValueToD("0", 3)),
                            new AsmCodeLine(DToTopStack(3)),
                            new AsmCodeLine(LiftStack(3)),
                            new AsmCodeLine(UnconditionalJumpToAddressInMemory("R14", 3))
                        })
                    }),
                new AsmCodeSection("Return",
                    new IAsmOutput[]
                    {
                        new AsmCodeLine(WriteLabel(ReturnSubLabel)),
                        new AsmCodeSection("FRAME  = LCL", new[]
                        {
                            new AsmCodeLine(MemoryToD("LCL", "M[Local]", 3)),
                            new AsmCodeLine(DToMemory("R14", 3))
                        }),
                        new AsmCodeSection("RET = *(FRAME-5)", new[]
                        {
                            new AsmCodeLine(AInstruction(5.ToString())),
                            new AsmCodeLine("A=D-A", "FRAME - 5 => A"),
                            new AsmCodeLine("D=M", "M[FRAME - 5] => D"),
                            new AsmCodeLine(DToMemory("R15", 3))
                        }),
                        new AsmCodeSection("*ARG = pop()", new[]
                        {
                            WritePop(string.Empty, SegmentType.Argument, 0, 3)
                        }),
                        new AsmCodeSection("SP = ARG + 1", new[]
                        {
                            new AsmCodeLine(AInstruction("ARG")),
                            new AsmCodeLine("D=M+1", "M[Argument] + 1 => D"),
                            new AsmCodeLine(DToMemory("SP", 3))
                        }),
                        new AsmCodeSection("That = *(FRAME-1)", new[]
                        {
                            new AsmCodeLine(OffsetMemoryToD("R14", "FRAME", -1, 3)),
                            new AsmCodeLine(DToMemory("THAT", 3)),
                            new AsmCodeLine(string.Empty, "That = M[FRAME-1]")
                        }),
                        new AsmCodeSection("This = *(FRAME-2)", new[]
                        {
                            new AsmCodeLine(OffsetMemoryToD("R14", "FRAME", -2, 3)),
                            new AsmCodeLine(DToMemory("THIS", 3)),
                            new AsmCodeLine(string.Empty, "This = M[FRAME-2]")
                        }),
                        new AsmCodeSection("Argument = *(FRAME-3)", new[]
                        {
                            new AsmCodeLine(OffsetMemoryToD("R14", "FRAME", -3, 3)),
                            new AsmCodeLine(DToMemory("ARG", 3)),
                            new AsmCodeLine(string.Empty, "Argument = M[FRAME-3]")
                        }),
                        new AsmCodeSection("Local = *(FRAME-4)", new[]
                        {
                            new AsmCodeLine(OffsetMemoryToD("R14", "FRAME", -4, 3)),
                            new AsmCodeLine(DToMemory("LCL", 3)),
                            new AsmCodeLine(string.Empty, "Local = M[FRAME-4]")
                        }),
                        new AsmCodeSection("goto RET", new[]
                        {
                            new AsmCodeLine(AInstruction("R15")),
                            new AsmCodeLine("A=M", "M[RET] => A"),
                            new AsmCodeLine("0;JMP", "goto RET")
                        })
                    }),
                new AsmCodeSection("Call Function",
                    new IAsmOutput[]
                    {
                        new AsmCodeLine(WriteLabel(CallSubLabel)),
                        new AsmCodeLine(PushD(2)),
                        new AsmCodeLine(AInstruction("LCL")),
                        new AsmCodeLine("D=M", "M[LCL] => D "),
                        new AsmCodeLine(PushD(2)),
                        new AsmCodeLine(AInstruction("ARG")),
                        new AsmCodeLine("D=M", "M[ARG] => D "),
                        new AsmCodeLine(PushD(2)),
                        new AsmCodeLine(AInstruction("THIS")),
                        new AsmCodeLine("D=M", "M[THIS] => D "),
                        new AsmCodeLine(PushD(2)),
                        new AsmCodeLine(AInstruction("THAT")),
                        new AsmCodeLine("D=M", "M[THAT] => D "),
                        new AsmCodeLine(PushD(2)),
                        new AsmCodeLine(AInstruction("R15")),
                        new AsmCodeLine("D=M", "M[R15] => D "),
                        new AsmCodeLine(AInstruction("5")),
                        new AsmCodeLine("D=D+A", "D = #arguments + 5"),
                        new AsmCodeLine(AInstruction("SP")),
                        new AsmCodeLine("D=M-D", "D = SP - #arguments - 5"),
                        new AsmCodeLine(DToMemory("ARG", 2)),
                        new AsmCodeLine(AInstruction("SP")),
                        new AsmCodeLine("D=M", "M[SP] => D "),
                        new AsmCodeLine(DToMemory("LCL", 2)),
                        new AsmCodeSection("Goto function address",
                            new[]
                            {
                                new AsmCodeLine(UnconditionalJumpToAddressInMemory("R14", 3))
                            })
                    }),
                new AsmCodeLine(WriteLabel(SkipSubsLabel))
            }),
            SetMemoryToValue(StackPointerAddress, BaseStackAddress.ToString(), 0),
            WriteFunctionCall("Sys.init", 0),
            new AsmCodeLine(WriteLabel("END")),
            new AsmCodeLine(UnconditionalJump("END", 0))
        };
    
    private static AsmCodeSection WritePush(string className, SegmentType segment, uint index)
    {
        switch (segment)
        {
            case SegmentType.Argument:
                return new AsmCodeSection($"Push M[M[Argument] + {index}]",
                    new[]
                    {
                        new AsmCodeLine(IndirectMemoryToD("ARG", index, "Argument", 1)),
                        new AsmCodeLine(PushD(1))
                    });
            
            case SegmentType.Local:
                return new AsmCodeSection($"Push M[M[Local] + {index}]",
                    new[]
                    {
                        new AsmCodeLine(IndirectMemoryToD("LCL", index, "Local", 1)),
                        new AsmCodeLine(PushD(1))
                    });
            
            case SegmentType.Static:
                return new AsmCodeSection($"Push M[Static {index}]",
                    new[]
                    {
                        new AsmCodeLine(MemoryToD($"{className}.{index}", $"M[M[Static {index}]]", 1)),
                        new AsmCodeLine(PushD(1))
                    });
            
            case SegmentType.Constant:
                return new AsmCodeSection($"Push Constant '{index}'",
                    new[]
                    {
                        new AsmCodeLine(ValueToD(index.ToString(), 1)),
                        new AsmCodeLine(PushD(1))
                    });
            
            case SegmentType.This:
                return new AsmCodeSection($"Push M[M[This] + {index}]",
                    new[]
                    {
                        new AsmCodeLine(IndirectMemoryToD("THIS", index, "This", 1)),
                        new AsmCodeLine(PushD(1))
                    });
            
            case SegmentType.That:
                return new AsmCodeSection($"Push M[M[That] + {index}]",
                    new[]
                    {
                        new AsmCodeLine(IndirectMemoryToD("THAT", index, "That", 1)),
                        new AsmCodeLine(PushD(1))
                    });
            
            case SegmentType.Pointer:
                var pointerAddress = BasePointerAddress + index;
                
                return new AsmCodeSection($"Push M[pointer + {index}]",
                    new[]
                    {
                        new AsmCodeLine(MemoryToD(pointerAddress.ToString(), $"pointer + {index}", 1)),
                        new AsmCodeLine(PushD(1))
                    });
            
            case SegmentType.Temp:
                var tempAddress = BaseTempAddress + index;
                
                return new AsmCodeSection($"Push M[temp + {index}]",
                    new[]
                    {
                        new AsmCodeLine(MemoryToD(tempAddress.ToString(), $"temp + {index}", 1)),
                        new AsmCodeLine(PushD(1))
                    });
            
            default:
                throw new InvalidOperationException("Should not be reachable");
        }
    }

    private static AsmCodeSection WritePop(string className, SegmentType segment, uint index, int indentation)
    {
        
        
        switch (segment)
        {
            case SegmentType.Argument:
                return new AsmCodeSection($"Pop M[M[Argument] + {index}]",
                    new[]
                    {
                        new AsmCodeLine(OffsetMemoryToMemory("ARG", "Argument", index, "R13", indentation + 1)),
                        new AsmCodeLine(DropStack(indentation + 1)),
                        new AsmCodeLine(TopStackToD(indentation + 1)),
                        new AsmCodeLine(DToIndirectMemory("R13", $"M[Argument] + {index}", indentation + 1))
                    });
            
            case SegmentType.Local:
                return new AsmCodeSection($"Pop M[M[Local] + {index}]",
                    new[]
                    {
                        new AsmCodeLine(OffsetMemoryToMemory("LCL", "Local", index, "R13", indentation + 1)),
                        new AsmCodeLine(DropStack(indentation + 1)),
                        new AsmCodeLine(TopStackToD(indentation + 1)),
                        new AsmCodeLine(DToIndirectMemory("R13", $"M[Local] + {index}", indentation + 1))
                    });
            
            case SegmentType.Static:
                return new AsmCodeSection($"Pop M[Static {index}]",
                    new[]
                    {
                        new AsmCodeLine(DropStack(indentation + 1)),
                        new AsmCodeLine(TopStackToD(indentation + 1)),
                        new AsmCodeLine(AInstruction($"{className}.{index}")),
                        new AsmCodeLine("M=D", $"D => M[Static {index}]")
                    });
            
            case SegmentType.This:
                return new AsmCodeSection($"Pop M[M[This] + {index}]",
                    new[]
                    {
                        new AsmCodeLine(OffsetMemoryToMemory("THIS", "This", index, "R13", indentation + 1)),
                        new AsmCodeLine(DropStack(indentation + 1)),
                        new AsmCodeLine(TopStackToD(indentation + 1)),
                        new AsmCodeLine(DToIndirectMemory("R13", $"M[This] + {index}", indentation + 1) )
                    });
            
            case SegmentType.That:
                return new AsmCodeSection($"Pop M[M[That] + {index}]",
                    new[]
                    {
                        new AsmCodeLine(OffsetMemoryToMemory("THAT", "That", index, "R13", indentation + 1)),
                        new AsmCodeLine(DropStack(indentation + 1)),
                        new AsmCodeLine(TopStackToD(indentation + 1)),
                        new AsmCodeLine(DToIndirectMemory("R13", $"M[That] + {index}", indentation + 1))
                    });
            case SegmentType.Pointer:
            {
                var pointerAddress = BasePointerAddress + index;
                var memoryAddressComment = $"pointer + {index}";
                
                return new AsmCodeSection($"Pop M[pointer + {index}]",
                    new[]
                    {
                        new AsmCodeLine(DropStack(indentation + 1)),
                        new AsmCodeLine(TopStackToD(indentation + 1)),
                        new AsmCodeLine($"@{pointerAddress.ToString()}", $"{memoryAddressComment} => A"),
                        new AsmCodeLine("M=D", $"D => {memoryAddressComment}")
                    });
            }

            case SegmentType.Temp:
            {
                var tempAddress = BaseTempAddress + index;
                var memoryAddressComment = $"temp + {index}";
                
                return new AsmCodeSection($"Pop M[temp + {index}]",
                    new[]
                    {
                        new AsmCodeLine(DropStack(indentation + 1)),
                        new AsmCodeLine(TopStackToD(indentation + 1)),
                        new AsmCodeLine($"@{tempAddress.ToString()}", $"{memoryAddressComment} => A"),
                        new AsmCodeLine("M=D", $"D => {memoryAddressComment}")
                    });
            }
            // ReSharper disable once RedundantCaseLabel
            case SegmentType.Constant:
            default:
                throw new InvalidOperationException("Should not be reachable");
        }
    }

    private static AsmCodeSection WriteUnaryOperator(string operatorSymbol, string operatorName, string commentOperator) =>
        new (operatorName,
            new[]
            {
                new AsmCodeLine(DropStack(1)),
                new AsmCodeLine(OperatorMemoryToMemory(operatorSymbol, commentOperator, 1)),
                new AsmCodeLine(LiftStack(1)),
            });

    private static AsmCodeSection WriteBinaryOperator(string operatorSymbol, string operatorName, string commentOperator) =>
        new(operatorName,
            new[]
            {
                new AsmCodeLine(BinaryOperatorToD(operatorSymbol, commentOperator, 1)),
                new AsmCodeLine(PushD(1))
            });
    
    private static AsmCodeSection WriteComparison(string operatorName, string returnLabel, string subLabel)
    {
        var label = returnLabel + _comparisionReturnLabelNum;
        var equalsSection = new AsmCodeSection(operatorName,
            new IAsmOutput[]
            {
                new AsmCodeSection($"Set R14 to '{label}'",
                    new []
                    {
                        new AsmCodeLine(ValueToD(label, 2)),
                        new AsmCodeLine(DToMemory("R14", 2))
                    }),
                new AsmCodeLine(UnconditionalJump(subLabel, 1)),
                new AsmCodeLine(WriteLabel(label))
            });

        _comparisionReturnLabelNum++;
        return equalsSection;
    }

    private static AsmCodeSection WriteIfGoto(string functionName, string label) =>
        new($"If-Goto {ToAsmFunctionQualifiedLabel(functionName, label)}",
            new[]
            {
                new AsmCodeLine(DropStack(1)),
                new AsmCodeLine(TopStackToD(1)),
                new AsmCodeLine(ConditionalJump("JNE", ToAsmFunctionQualifiedLabel(functionName, label), 1))
            });
    
    private static AsmCodeSection WriteGoto(string functionName, string label) =>
        new($"Goto {ToAsmFunctionQualifiedLabel(functionName, label)}",
            new[]
            {
                new AsmCodeLine(UnconditionalJump(ToAsmFunctionQualifiedLabel(functionName, label), 1))
            });
    
    private static AsmCodeSection WriteReturn() =>
        new("Return",
            new[]
            {
                new AsmCodeLine(UnconditionalJump(ReturnSubLabel, 1))
            });
    
    private static AsmCodeSection WriteFunctionDeclaration(string functionName, uint numLocals)
    {
        var codeLines = new List<AsmCodeLine>();
        var codeSection = new AsmCodeSection(
            $"Declare Function:{functionName} Locals:{numLocals}",
            codeLines);
            
        codeLines.Add(new AsmCodeLine(WriteLabel("$" + functionName)));

        if (numLocals > 0)
        {
            codeLines.Add(new AsmCodeLine(ValueToD(0.ToString(), 1)));
            for (var i = 0; i < numLocals; i++)
            {
                codeLines.Add(new AsmCodeLine(PushD(1)));
            }
        }

        return codeSection;
    }

    private static AsmCodeSection WriteFunctionCall(string functionName, uint numArguments)
    {
        var label = ReturnAddressLabel + _functionReturnLabelNum;
        string escapedFunctionName = "$" + functionName;
        var code = new AsmCodeSection(
            $"Call Function:{functionName} Args:{numArguments}",
            new[]
            {
                new AsmCodeLine(AInstruction(escapedFunctionName)),
                new AsmCodeLine("D=A", $"{escapedFunctionName}=> D"),
                new AsmCodeLine(DToMemory("R14", 1)),
                new AsmCodeLine(AInstruction(numArguments.ToString())),
                new AsmCodeLine("D=A", "Number Of Arguments => D"),
                new AsmCodeLine(DToMemory("R15", 1)),
                new AsmCodeLine(AInstruction(label)),
                new AsmCodeLine("D=A", $"{escapedFunctionName}=> D"),
                new AsmCodeLine(UnconditionalJump(CallSubLabel, 1)),
                new AsmCodeLine(WriteLabel(label))
            });
        _functionReturnLabelNum++;
        return code;
    }

    private static string WriteFunctionQualifiedLabel(string functionName, string label) =>
        WriteLabel(ToAsmFunctionQualifiedLabel(functionName, label));
    
    private static string ToAsmFunctionQualifiedLabel(string functionName, string label) =>
        $"{functionName}${label}";
    
    private static string BinaryOperatorToD(string operatorSymbol, string commentOperator, int indentation) =>
        PopToD(indentation) +
        DToMemory("R13", indentation) +
        PopToD(indentation) +
        DOperatorMemoryToD("R13", operatorSymbol, commentOperator, indentation);
    
    private static AsmCodeSection SetMemoryToValue(string memoryAddress, string value, int indentation) =>
        new ($"Set {memoryAddress} to '{value}'",
            new[]
            {
                new AsmCodeLine(AInstruction(value)),
                new AsmCodeLine("D=A", $"{value} => D"),
                new AsmCodeLine(DToMemory(memoryAddress, indentation + 1))
            });

    private static string PushD(int indentation) =>
        DToTopStack(indentation) +
        LiftStack(indentation);

    private static string PopToD(int indentation) =>
        DropStack(indentation) +
        TopStackToD(indentation);

    private static string DToMemory(string memoryAddress, int indentation) =>
        AInstruction(memoryAddress) +
        PadLine("M=D") + Comment($"D => {memoryAddress}", indentation);

    private static string MemoryToD(string memoryAddress, string memoryAddressComment, int indentation) =>
        AInstruction(memoryAddress) +
        PadLine("D=M") + Comment($"{memoryAddressComment} => D", indentation);
    
    private static string ValueToD(string value, int indentation) =>
        AInstruction(value) +
        PadLine("D=A") + Comment($"{value} => D", indentation);
    
    private static string NegativeValueToD(string value, int indentation) =>
        AInstruction(value) +
        PadLine("D=-A") + Comment($"-{value} => D", indentation);
    
    private static string DOperatorMemoryToD(string memoryAddress, string operatorSymbol, string commentOperator, int indentation) =>
        AInstruction(memoryAddress) +
        PadLine($"D=D{operatorSymbol}M") + Comment($"D {commentOperator} M[{memoryAddress}] => D", indentation);

    private static string IndirectMemoryToD(string memoryAddress, uint index, string commentMemoryAddress, int indentation) 
    {
        if (index == 0)
        {
            return 
                AInstruction(memoryAddress) +
                PadLine("A=M") + Comment($"M[{commentMemoryAddress}] => A", indentation) +
                PadLine("D=M") + Comment($"M[M[{commentMemoryAddress}] + 0] => D", indentation);
        }
        else
        {
            return 
                ValueToD(index.ToString(), indentation) +
                AInstruction(memoryAddress) +
                PadLine("A=M") + Comment($"M[{commentMemoryAddress}] => A", indentation) +
                PadLine("A=D+A") + Comment($"M[{commentMemoryAddress}] + {index} => A", indentation) +
                PadLine("D=M") + Comment($"M[M[{commentMemoryAddress}] + {index}] => D", indentation);
        }
    }
        
    private static string DToIndirectMemory(string memoryAddress, string commentMemoryAddress, int indentation) =>  
        AInstruction(memoryAddress) +
        PadLine("A=M") + Comment($"{commentMemoryAddress} => A", indentation) +
        PadLine("M=D") + Comment($"D => {commentMemoryAddress}", indentation);

    private static string OffsetMemoryToMemory(
        string fromMemoryAddress,
        string commentFromMemoryAddress,
        uint index,
        string toMemoryAddress,
        int indentation)
    {
        if (index == 0)
        {
            return
                AInstruction(fromMemoryAddress) +
                PadLine("D=M") + Comment($"M[{commentFromMemoryAddress}] => D", indentation) +
                DToMemory(toMemoryAddress, indentation);
        }
        else
        {
            return
                AInstruction(fromMemoryAddress) +
                PadLine("D=M") + Comment($"M[{commentFromMemoryAddress}] => D", indentation) +
                AInstruction(index.ToString()) +
                PadLine("D=D+A") + Comment($"M[{commentFromMemoryAddress}] + {index} => D", indentation) +
                DToMemory(toMemoryAddress, indentation);
        }
    }

    private static string OffsetMemoryToD(string toMemoryAddress, string commentToMemoryAddress, int index, int indentation) 
    {
        if (index == 0)
        {
            return MemoryToD(toMemoryAddress, commentToMemoryAddress, indentation);
        }

        if (index == -1)
        {
            return
                AInstruction(toMemoryAddress) +
                PadLine("A=M-1") + Comment($"{commentToMemoryAddress} - {-index} => A", indentation) +
                PadLine("D=M") + Comment($"M[{commentToMemoryAddress}-{-index}] => D", indentation);
        }
        else if (index == 1)
        {
            return
                AInstruction(toMemoryAddress) +
                PadLine("A=M+1") + Comment($"{commentToMemoryAddress} + {index} => A", indentation) +
                PadLine("D=M") + Comment($"M[{commentToMemoryAddress}+{index}] => D", indentation);
        }
        else if (index < 0)
        {
            return
                AInstruction(toMemoryAddress) +
                PadLine("D=M") + Comment($"{commentToMemoryAddress} => D", indentation) +
                AInstruction((-index).ToString()) +
                PadLine("A=D-A") + Comment($"{commentToMemoryAddress}-{-index} => A", indentation) +
                PadLine("D=M") + Comment($"M[{commentToMemoryAddress}-{-index}] => D", indentation);
        }
        else
        {
            return
                AInstruction(toMemoryAddress) +
                PadLine("D=M") + Comment($"{commentToMemoryAddress} => D", indentation) +
                AInstruction(index.ToString()) +
                PadLine("A=D+A") + Comment($"{commentToMemoryAddress}+{index} => A", indentation) +
                PadLine("D=M") + Comment($"M[{commentToMemoryAddress}+{index}] => D", indentation);
        }
    }
    
    private static string OperatorMemoryToMemory(string operatorSymbol, string commentOperator, int indentation) =>
        PadLine("A=M") + Environment.NewLine +
        PadLine($"M={operatorSymbol}M") + Comment($"{commentOperator}M => M", indentation);

    private static string DToTopStack(int indentation) =>
        AInstruction("SP") +
        PadLine("A=M") + Environment.NewLine +
        PadLine("M=D") + Comment("D => TopStack", indentation);

    private static string TopStackToD(int indentation) =>
        PadLine("A=M") + Environment.NewLine +
        PadLine("D=M") + Comment("TopStack => D", indentation);
    
    private static string LiftStack(int indentation) =>
        AInstruction("SP") +
        PadLine("M=M+1") + Comment("Lift Stack", indentation);
    
    private static string DropStack(int indentation) =>
        AInstruction("SP") +
        PadLine("M=M-1") + Comment("Drop Stack", indentation);

    private static string AInstruction(string value)
        => PadLine($"@{value}") + Environment.NewLine;
    
    private static string WriteLabel(string label)
        => PadLine($"({label})") + Environment.NewLine;

    private static string UnconditionalJump(string address, int indentation) =>
        AInstruction(address) +
        PadLine("0;JMP")  + Comment($"goto {address}", indentation);

    private static string ConditionalJump(string jumpType, string address, int indentation)
    {
        if (jumpType == "JNE")
        {
            return
                AInstruction(address) +
                PadLine($"D;{jumpType}") + Comment($"if D!= 0 then goto {address}", indentation);
        }
        else
        {
            return
                AInstruction(address) +
                PadLine($"D;{jumpType}") + Comment($"goto {address}", indentation);
        }
    }

    private static string UnconditionalJumpToAddressInMemory(string memoryAddress, int indentation) =>
        AInstruction(memoryAddress) +
        PadLine("A=M") + Environment.NewLine +
        PadLine("0;JMP")  + Comment($"goto {memoryAddress}", indentation);
    
    private static string Comment(string comment, int indentation)
        => " // "+ "".PadRight(indentation * 3, ' ') + comment + Environment.NewLine;
    
    private static string PadLine(string value)
        => value.PadRight(25, ' ');
    
}