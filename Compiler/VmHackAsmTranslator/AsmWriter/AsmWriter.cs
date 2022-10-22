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
                    asmOutputs.Add(WriteFunctionQualifiedLabel(labelCommand.FunctionName, labelCommand.Symbol));
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
                UnconditionalJump(SkipSubsLabel),
                new AsmCodeSection("Equals",
                    new IAsmOutput[]
                    {
                        WriteLabel(EqualsSubLabel),
                        BinaryOperatorToD("-", "-", 2),
                        new AsmCodeLine(string.Empty, "If D = 0 Then Goto IsTrue Else Goto IsFalse"),
                        ConditionalJump("JEQ", IsTrueLabel),
                        UnconditionalJump(IsFalseLabel)
                    }),
                new AsmCodeSection("Is Less Than",
                    new IAsmOutput[]
                    {
                        WriteLabel(LessThanSubLabel),
                        BinaryOperatorToD("-", "-", 2),
                        new AsmCodeLine(string.Empty, "If D < 0 Then Goto IsTrue Else Goto IsFalse"),
                        ConditionalJump("JLT", IsTrueLabel),
                        UnconditionalJump(IsFalseLabel)
                    }),
                new AsmCodeSection("Is Greater Than",
                    new IAsmOutput[]
                    {
                        WriteLabel(GreaterThanSubLabel),
                        BinaryOperatorToD("-", "-", 2),
                        new AsmCodeLine(string.Empty, "If D > 0 Then Goto IsTrue Else Goto IsFalse"),
                        ConditionalJump("JGT", IsTrueLabel),
                        UnconditionalJump(IsFalseLabel)
                    }),
                new AsmCodeSection("ReusableComparison",
                    new[]
                    {
                        new AsmCodeSection("Is True", new IAsmOutput[]
                        {
                            WriteLabel(IsTrueLabel),
                            NegativeValueToD("1"),
                            new AsmCodeLine(DToTopStack(3)),
                            new AsmCodeLine(LiftStack(3)),
                            UnconditionalJumpToAddressInMemory("R14")
                        }),
                        new AsmCodeSection("Is False", new IAsmOutput[]
                        {
                            WriteLabel(IsFalseLabel),
                            new AsmCodeLine(ValueToD("0", 3)),
                            new AsmCodeLine(DToTopStack(3)),
                            new AsmCodeLine(LiftStack(3)),
                            UnconditionalJumpToAddressInMemory("R14")
                        })
                    }),
                new AsmCodeSection("Return",
                    new IAsmOutput[]
                    {
                        WriteLabel(ReturnSubLabel),
                        new AsmCodeSection("FRAME  = LCL", new []
                        {
                            MemoryToD("LCL", "M[Local]"),
                            DToMemory("R14")
                        }),
                        new AsmCodeSection("RET = *(FRAME-5)", new IAsmOutput[]
                        {
                            new AsmCodeLine(AInstruction(5.ToString())),
                            new AsmCodeLine("A=D-A", "FRAME - 5 => A"),
                            new AsmCodeLine("D=M", "M[FRAME - 5] => D"),
                            DToMemory("R15")
                        }),
                        new AsmCodeSection("*ARG = pop()", new[]
                        {
                            WritePop(string.Empty, SegmentType.Argument, 0, 3)
                        }),
                        new AsmCodeSection("SP = ARG + 1", new IAsmOutput[]
                        {
                            new AsmCodeLine(AInstruction("ARG")),
                            new AsmCodeLine("D=M+1", "M[Argument] + 1 => D"),
                            DToMemory("SP")
                        }),
                        new AsmCodeSection("That = *(FRAME-1)", new IAsmOutput[]
                        {
                            OffsetMemoryToD("R14", "FRAME", -1),
                            DToMemory("THAT"),
                            new AsmCodeLine(string.Empty, "That = M[FRAME-1]")
                        }),
                        new AsmCodeSection("This = *(FRAME-2)", new IAsmOutput[]
                        {
                            OffsetMemoryToD("R14", "FRAME", -2),
                            DToMemory("THIS"),
                            new AsmCodeLine(string.Empty, "This = M[FRAME-2]")
                        }),
                        new AsmCodeSection("Argument = *(FRAME-3)", new IAsmOutput[]
                        {
                            OffsetMemoryToD("R14", "FRAME", -3),
                            DToMemory("ARG"),
                            new AsmCodeLine(string.Empty, "Argument = M[FRAME-3]")
                        }),
                        new AsmCodeSection("Local = *(FRAME-4)", new IAsmOutput[]
                        {
                            OffsetMemoryToD("R14", "FRAME", -4),
                            DToMemory("LCL"),
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
                        WriteLabel(CallSubLabel),
                        PushD(2),
                        new AsmCodeLine(AInstruction("LCL")),
                        new AsmCodeLine("D=M", "M[LCL] => D "),
                        PushD(2),
                        new AsmCodeLine(AInstruction("ARG")),
                        new AsmCodeLine("D=M", "M[ARG] => D "),
                        PushD(2),
                        new AsmCodeLine(AInstruction("THIS")),
                        new AsmCodeLine("D=M", "M[THIS] => D "),
                        PushD(2),
                        new AsmCodeLine(AInstruction("THAT")),
                        new AsmCodeLine("D=M", "M[THAT] => D "),
                        PushD(2),
                        new AsmCodeLine(AInstruction("R15")),
                        new AsmCodeLine("D=M", "M[R15] => D "),
                        new AsmCodeLine(AInstruction("5")),
                        new AsmCodeLine("D=D+A", "D = #arguments + 5"),
                        new AsmCodeLine(AInstruction("SP")),
                        new AsmCodeLine("D=M-D", "D = SP - #arguments - 5"),
                        DToMemory("ARG"),
                        new AsmCodeLine(AInstruction("SP")),
                        new AsmCodeLine("D=M", "M[SP] => D "),
                        DToMemory("LCL"),
                        new AsmCodeSection("Goto function address", new[]
                        {
                            UnconditionalJumpToAddressInMemory("R14")
                        })
                    }),
                WriteLabel(SkipSubsLabel)
            }),
            SetMemoryToValue(StackPointerAddress, BaseStackAddress.ToString()),
            WriteFunctionCall("Sys.init", 0),
            WriteLabel("END"),
            UnconditionalJump("END")
        };
    
    private static AsmCodeSection WritePush(string className, SegmentType segment, uint index)
    {
        switch (segment)
        {
            case SegmentType.Argument:
                return new AsmCodeSection($"Push M[M[Argument] + {index}]",
                    new IAsmOutput[]
                    {
                        new AsmCodeLine(IndirectMemoryToD("ARG", index, "Argument", 1)),
                        PushD(1)
                    });
            
            case SegmentType.Local:
                return new AsmCodeSection($"Push M[M[Local] + {index}]",
                    new IAsmOutput[]
                    {
                        new AsmCodeLine(IndirectMemoryToD("LCL", index, "Local", 1)),
                        PushD(1)
                    });
            
            case SegmentType.Static:
                return new AsmCodeSection($"Push M[Static {index}]",
                    new []
                    {
                        MemoryToD($"{className}.{index}", $"M[M[Static {index}]]"),
                        PushD(1)
                    });
            
            case SegmentType.Constant:
                return new AsmCodeSection($"Push Constant '{index}'",
                    new IAsmOutput[]
                    {
                        new AsmCodeLine(ValueToD(index.ToString(), 1)),
                        PushD(1)
                    });
            
            case SegmentType.This:
                return new AsmCodeSection($"Push M[M[This] + {index}]",
                    new IAsmOutput[]
                    {
                        new AsmCodeLine(IndirectMemoryToD("THIS", index, "This", 1)),
                        PushD(1)
                    });
            
            case SegmentType.That:
                return new AsmCodeSection($"Push M[M[That] + {index}]",
                    new IAsmOutput[]
                    {
                        new AsmCodeLine(IndirectMemoryToD("THAT", index, "That", 1)),
                        PushD(1)
                    });
            
            case SegmentType.Pointer:
                var pointerAddress = BasePointerAddress + index;
                
                return new AsmCodeSection($"Push M[pointer + {index}]",
                    new []
                    {
                        MemoryToD(pointerAddress.ToString(), $"pointer + {index}"),
                        PushD(1)
                    });
            
            case SegmentType.Temp:
                var tempAddress = BaseTempAddress + index;
                
                return new AsmCodeSection($"Push M[temp + {index}]",
                    new []
                    {
                        MemoryToD(tempAddress.ToString(), $"temp + {index}"),
                        PushD(1)
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
                    new IAsmOutput[]
                    {
                        OffsetMemoryToMemory("ARG", "Argument", index, "R13"),
                        new AsmCodeLine(DropStack(indentation + 1)),
                        new AsmCodeLine(TopStackToD(indentation + 1)),
                        DToIndirectMemory("R13", $"M[Argument] + {index}")
                    });
            
            case SegmentType.Local:
                return new AsmCodeSection($"Pop M[M[Local] + {index}]",
                    new IAsmOutput[]
                    {
                        OffsetMemoryToMemory("LCL", "Local", index, "R13"),
                        new AsmCodeLine(DropStack(indentation + 1)),
                        new AsmCodeLine(TopStackToD(indentation + 1)),
                        DToIndirectMemory("R13", $"M[Local] + {index}")
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
                    new IAsmOutput[]
                    {
                        OffsetMemoryToMemory("THIS", "This", index, "R13"),
                        new AsmCodeLine(DropStack(indentation + 1)),
                        new AsmCodeLine(TopStackToD(indentation + 1)),
                       DToIndirectMemory("R13", $"M[This] + {index}")
                    });
            
            case SegmentType.That:
                return new AsmCodeSection($"Pop M[M[That] + {index}]",
                    new IAsmOutput[]
                    {
                        OffsetMemoryToMemory("THAT", "That", index, "R13"),
                        new AsmCodeLine(DropStack(indentation + 1)),
                        new AsmCodeLine(TopStackToD(indentation + 1)),
                        DToIndirectMemory("R13", $"M[That] + {index}")
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
            new []
            {
                BinaryOperatorToD(operatorSymbol, commentOperator, 1),
                PushD(1)
            });
    
    private static AsmCodeSection WriteComparison(string operatorName, string returnLabel, string subLabel)
    {
        var label = returnLabel + _comparisionReturnLabelNum;
        var equalsSection = new AsmCodeSection(operatorName,
            new IAsmOutput[]
            {
                new AsmCodeSection($"Set R14 to '{label}'",
                    new IAsmOutput[]
                    {
                        new AsmCodeLine(ValueToD(label, 2)),
                        DToMemory("R14")
                    }),
                UnconditionalJump(subLabel),
                WriteLabel(label)
            });

        _comparisionReturnLabelNum++;
        return equalsSection;
    }

    private static AsmCodeSection WriteIfGoto(string functionName, string label) =>
        new($"If-Goto {ToAsmFunctionQualifiedLabel(functionName, label)}",
            new IAsmOutput[]
            {
                new AsmCodeLine(DropStack(1)),
                new AsmCodeLine(TopStackToD(1)),
                ConditionalJump("JNE", ToAsmFunctionQualifiedLabel(functionName, label))
            });
    
    private static AsmCodeSection WriteGoto(string functionName, string label) =>
        new($"Goto {ToAsmFunctionQualifiedLabel(functionName, label)}",
            new[]
            {
                UnconditionalJump(ToAsmFunctionQualifiedLabel(functionName, label))
            });
    
    private static AsmCodeSection WriteReturn() =>
        new("Return",
            new[]
            {
                UnconditionalJump(ReturnSubLabel)
            });
    
    private static AsmCodeSection WriteFunctionDeclaration(string functionName, uint numLocals)
    {
        var codeLines = new List<IAsmOutput>();
        var codeSection = new AsmCodeSection(
            $"Declare Function:{functionName} Locals:{numLocals}",
            codeLines);
            
        codeLines.Add(WriteLabel("$" + functionName));

        if (numLocals > 0)
        {
            codeLines.Add(new AsmCodeLine(ValueToD(0.ToString(), 1)));
            for (var i = 0; i < numLocals; i++)
            {
                codeLines.Add(PushD(1));
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
            new IAsmOutput[]
            {
                new AsmCodeLine(AInstruction(escapedFunctionName)),
                new AsmCodeLine("D=A", $"{escapedFunctionName}=> D"),
                DToMemory("R14"),
                new AsmCodeLine(AInstruction(numArguments.ToString())),
                new AsmCodeLine("D=A", "Number Of Arguments => D"),
                DToMemory("R15"),
                new AsmCodeLine(AInstruction(label)),
                new AsmCodeLine("D=A", $"{escapedFunctionName}=> D"),
                UnconditionalJump(CallSubLabel),
                WriteLabel(label)
            });
        _functionReturnLabelNum++;
        return code;
    }

    private static AsmCodeLine WriteFunctionQualifiedLabel(string functionName, string label) =>
        WriteLabel(ToAsmFunctionQualifiedLabel(functionName, label));
    
    private static string ToAsmFunctionQualifiedLabel(string functionName, string label) =>
        $"{functionName}${label}";

    private static AsmCodeSection BinaryOperatorToD(string operatorSymbol, string commentOperator, int indentation) =>
        new(new IAsmOutput[]
        {
            PopToD(indentation),
            DToMemory("R13"),
            PopToD(indentation),
            new AsmCodeLine(DOperatorMemoryToD("R13", operatorSymbol, commentOperator, indentation))
        });
    
    private static AsmCodeSection SetMemoryToValue(string memoryAddress, string value) =>
        new ($"Set {memoryAddress} to '{value}'",
            new IAsmOutput[]
            {
                new AsmCodeLine(AInstruction(value)),
                new AsmCodeLine("D=A", $"{value} => D"),
                DToMemory(memoryAddress)
            });

    private static AsmCodeSection PushD(int indentation) =>
        new(new AsmCodeLine[]
        {
            new(DToTopStack(indentation)),
            new(LiftStack(indentation))
        });

    private static AsmCodeSection PopToD(int indentation) =>
        new(new AsmCodeLine[]
        {
            new(DropStack(indentation)),
            new(TopStackToD(indentation))
        });

    private static AsmCodeSection DToMemory(string memoryAddress) =>
        new(new AsmCodeLine[]
        {
            new(AInstruction(memoryAddress)),
            new("M=D", $"D => {memoryAddress}")
        });

    private static AsmCodeSection MemoryToD(string memoryAddress, string memoryAddressComment) =>
        new(new AsmCodeLine[]
        {
            new(AInstruction(memoryAddress)),
            new("D=M", $"{memoryAddressComment} => D")
        });
    
    private static string ValueToD(string value, int indentation) =>
        AInstruction(value) +
        PadLine("D=A") + Comment($"{value} => D", indentation);

    private static AsmCodeSection NegativeValueToD(string value) =>
        new(new AsmCodeLine[]
        {
            new(AInstruction(value)),
            new("D=-A", $"-{value} => D")
        });
    
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

    private static AsmCodeSection DToIndirectMemory(string memoryAddress, string commentMemoryAddress) =>
        new(new[]
        {
            new AsmCodeLine(AInstruction(memoryAddress)),
            new AsmCodeLine("A=M", $"{commentMemoryAddress} => A"),
            new AsmCodeLine("M=D", $"D => {commentMemoryAddress}")
        });

    private static AsmCodeSection OffsetMemoryToMemory(
        string fromMemoryAddress,
        string commentFromMemoryAddress,
        uint index,
        string toMemoryAddress)
    {
        if (index == 0)
        {
            return new(new IAsmOutput[]
            {
                new AsmCodeLine(AInstruction(fromMemoryAddress)),
                new AsmCodeLine("D=M", $"M[{commentFromMemoryAddress}] => D"),
                DToMemory(toMemoryAddress)
            });
        }
        else
        {
            return new(new IAsmOutput[]
            {
                new AsmCodeLine(AInstruction(fromMemoryAddress)),
                new AsmCodeLine("D=M", $"M[{commentFromMemoryAddress}] => D"),
                new AsmCodeLine(AInstruction(index.ToString())),
                new AsmCodeLine("D=D+A", $"M[{commentFromMemoryAddress}] + {index} => D"),
                DToMemory(toMemoryAddress),
            });
        }
    }

    private static AsmCodeSection OffsetMemoryToD(string toMemoryAddress, string commentToMemoryAddress, int index) 
    {
        if (index == 0)
        {
            return MemoryToD(toMemoryAddress, commentToMemoryAddress);
        }

        if (index == -1)
        {
            return new(new[]
            {
                new AsmCodeLine(AInstruction(toMemoryAddress)),
                new AsmCodeLine("A=M-1", $"{commentToMemoryAddress} - {-index} => A"),
                new AsmCodeLine("D=M", $"M[{commentToMemoryAddress}-{-index}] => D")
            });
        }
        else if (index == 1)
        {
            return new(new[]
            {
                new AsmCodeLine(AInstruction(toMemoryAddress)),
                new AsmCodeLine("A=M+1", $"{commentToMemoryAddress} + {index} => A"),
                new AsmCodeLine("D=M", $"M[{commentToMemoryAddress}+{index}] => D")
            });
        }
        else if (index < 0)
        {
            return new( new[]
            {
                new AsmCodeLine(AInstruction(toMemoryAddress)),
                new AsmCodeLine("D=M", $"{commentToMemoryAddress} => D"),
                new AsmCodeLine(AInstruction((-index).ToString())),
                new AsmCodeLine("A=D-A", $"{commentToMemoryAddress}-{-index} => A"),
                new AsmCodeLine("D=M", $"M[{commentToMemoryAddress}-{-index}] => D")
            });
        }
        else
        {
            return new( new[]
            {
                new AsmCodeLine(AInstruction(toMemoryAddress)),
                new AsmCodeLine("D=M", $"{commentToMemoryAddress} => D"),
                new AsmCodeLine(AInstruction(index.ToString())),
                new AsmCodeLine("A=D+A", $"{commentToMemoryAddress}+{index} => A"),
                new AsmCodeLine("D=M", $"M[{commentToMemoryAddress}+{index}] => D")
            });
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

    private static AsmCodeLine WriteLabel(string label)
        => new($"({label})");

    private static AsmCodeSection UnconditionalJump(string address) =>
        new(new AsmCodeLine[]
        {
            new(AInstruction(address)),
            new("0;JMP", $"goto {address}")
        });

    private static AsmCodeSection ConditionalJump(string jumpType, string address)
    {
        if (jumpType == "JNE")
        {
            return new(new AsmCodeLine[]
            {
                new(AInstruction(address)),
                new($"D;{jumpType}", $"if D!= 0 then goto {address}")
            });
        }
        else
        {
            return new(new AsmCodeLine[]
            {
                new(AInstruction(address)),
                new($"D;{jumpType}", $"goto {address}")
            });
        }
    }

    private static AsmCodeSection UnconditionalJumpToAddressInMemory(string memoryAddress) =>
        new(new AsmCodeLine[]
        {
            new(AInstruction(memoryAddress)),
            new("A=M"),
            new("0;JMP", $"goto {memoryAddress}")
        });
    
    private static string Comment(string comment, int indentation)
        => " // "+ "".PadRight(indentation * 3, ' ') + comment + Environment.NewLine;
    
    private static string PadLine(string value)
        => value.PadRight(25, ' ');
    
}