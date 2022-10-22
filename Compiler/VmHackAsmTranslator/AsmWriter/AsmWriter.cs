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
                    asmOutputs.Add(WritePop(popCommand.ClassName, popCommand.Segment, popCommand.Index));
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
                        BinaryOperatorToD("-", "-"),
                        new AsmCodeLine(string.Empty, "If D = 0 Then Goto IsTrue Else Goto IsFalse"),
                        ConditionalJump("JEQ", IsTrueLabel),
                        UnconditionalJump(IsFalseLabel)
                    }),
                new AsmCodeSection("Is Less Than",
                    new IAsmOutput[]
                    {
                        WriteLabel(LessThanSubLabel),
                        BinaryOperatorToD("-", "-"),
                        new AsmCodeLine(string.Empty, "If D < 0 Then Goto IsTrue Else Goto IsFalse"),
                        ConditionalJump("JLT", IsTrueLabel),
                        UnconditionalJump(IsFalseLabel)
                    }),
                new AsmCodeSection("Is Greater Than",
                    new IAsmOutput[]
                    {
                        WriteLabel(GreaterThanSubLabel),
                        BinaryOperatorToD("-", "-"),
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
                            DToTopStack(),
                            LiftStack(),
                            UnconditionalJumpToAddressInMemory("R14")
                        }),
                        new AsmCodeSection("Is False", new IAsmOutput[]
                        {
                            WriteLabel(IsFalseLabel),
                            ValueToD("0"),
                            DToTopStack(),
                            LiftStack(),
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
                            AInstruction(5.ToString()),
                            new AsmCodeLine("A=D-A", "FRAME - 5 => A"),
                            new AsmCodeLine("D=M", "M[FRAME - 5] => D"),
                            DToMemory("R15")
                        }),
                        new AsmCodeSection("*ARG = pop()", new[]
                        {
                            WritePop(string.Empty, SegmentType.Argument, 0)
                        }),
                        new AsmCodeSection("SP = ARG + 1", new IAsmOutput[]
                        {
                            AInstruction("ARG"),
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
                            AInstruction("R15"),
                            new AsmCodeLine("A=M", "M[RET] => A"),
                            new AsmCodeLine("0;JMP", "goto RET")
                        })
                    }),
                new AsmCodeSection("Call Function",
                    new IAsmOutput[]
                    {
                        WriteLabel(CallSubLabel),
                        PushD(),
                        AInstruction("LCL"),
                        new AsmCodeLine("D=M", "M[LCL] => D "),
                        PushD(),
                        AInstruction("ARG"),
                        new AsmCodeLine("D=M", "M[ARG] => D "),
                        PushD(),
                        AInstruction("THIS"),
                        new AsmCodeLine("D=M", "M[THIS] => D "),
                        PushD(),
                        AInstruction("THAT"),
                        new AsmCodeLine("D=M", "M[THAT] => D "),
                        PushD(),
                        AInstruction("R15"),
                        new AsmCodeLine("D=M", "M[R15] => D "),
                        AInstruction("5"),
                        new AsmCodeLine("D=D+A", "D = #arguments + 5"),
                        AInstruction("SP"),
                        new AsmCodeLine("D=M-D", "D = SP - #arguments - 5"),
                        DToMemory("ARG"),
                        AInstruction("SP"),
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
                    new []
                    {
                        IndirectMemoryToD("ARG", index, "Argument"),
                        PushD()
                    });
            
            case SegmentType.Local:
                return new AsmCodeSection($"Push M[M[Local] + {index}]",
                    new []
                    {
                        IndirectMemoryToD("LCL", index, "Local"),
                        PushD()
                    });
            
            case SegmentType.Static:
                return new AsmCodeSection($"Push M[Static {index}]",
                    new []
                    {
                        MemoryToD($"{className}.{index}", $"M[M[Static {index}]]"),
                        PushD()
                    });
            
            case SegmentType.Constant:
                return new AsmCodeSection($"Push Constant '{index}'",
                    new []
                    {
                        ValueToD(index.ToString()),
                        PushD()
                    });
            
            case SegmentType.This:
                return new AsmCodeSection($"Push M[M[This] + {index}]",
                    new []
                    {
                        IndirectMemoryToD("THIS", index, "This"),
                        PushD()
                    });
            
            case SegmentType.That:
                return new AsmCodeSection($"Push M[M[That] + {index}]",
                    new []
                    {
                        IndirectMemoryToD("THAT", index, "That"),
                        PushD()
                    });
            
            case SegmentType.Pointer:
                var pointerAddress = BasePointerAddress + index;
                
                return new AsmCodeSection($"Push M[pointer + {index}]",
                    new []
                    {
                        MemoryToD(pointerAddress.ToString(), $"pointer + {index}"),
                        PushD()
                    });
            
            case SegmentType.Temp:
                var tempAddress = BaseTempAddress + index;
                
                return new AsmCodeSection($"Push M[temp + {index}]",
                    new []
                    {
                        MemoryToD(tempAddress.ToString(), $"temp + {index}"),
                        PushD()
                    });
            
            default:
                throw new InvalidOperationException("Should not be reachable");
        }
    }

    private static AsmCodeSection WritePop(string className, SegmentType segment, uint index)
    {
        switch (segment)
        {
            case SegmentType.Argument:
                return new AsmCodeSection($"Pop M[M[Argument] + {index}]",
                    new []
                    {
                        OffsetMemoryToMemory("ARG", "Argument", index, "R13"),
                        DropStack(),
                        TopStackToD(),
                        DToIndirectMemory("R13", $"M[Argument] + {index}")
                    });
            
            case SegmentType.Local:
                return new AsmCodeSection($"Pop M[M[Local] + {index}]",
                    new []
                    {
                        OffsetMemoryToMemory("LCL", "Local", index, "R13"),
                        DropStack(),
                        TopStackToD(),
                        DToIndirectMemory("R13", $"M[Local] + {index}")
                    });
            
            case SegmentType.Static:
                return new AsmCodeSection($"Pop M[Static {index}]",
                    new IAsmOutput[]
                    {
                        DropStack(),
                        TopStackToD(),
                        AInstruction($"{className}.{index}"),
                        new AsmCodeLine("M=D", $"D => M[Static {index}]")
                    });
            
            case SegmentType.This:
                return new AsmCodeSection($"Pop M[M[This] + {index}]",
                    new[]
                    {
                        OffsetMemoryToMemory("THIS", "This", index, "R13"),
                        DropStack(),
                        TopStackToD(),
                        DToIndirectMemory("R13", $"M[This] + {index}")
                    });
            
            case SegmentType.That:
                return new AsmCodeSection($"Pop M[M[That] + {index}]",
                    new []
                    {
                        OffsetMemoryToMemory("THAT", "That", index, "R13"),
                        DropStack(),
                        TopStackToD(),
                        DToIndirectMemory("R13", $"M[That] + {index}")
                    });
            case SegmentType.Pointer:
            {
                var pointerAddress = BasePointerAddress + index;
                var memoryAddressComment = $"pointer + {index}";
                
                return new AsmCodeSection($"Pop M[pointer + {index}]",
                    new IAsmOutput[]
                    {
                        DropStack(),
                        TopStackToD(),
                        new AsmCodeLine($"@{pointerAddress.ToString()}", $"{memoryAddressComment} => A"),
                        new AsmCodeLine("M=D", $"D => {memoryAddressComment}")
                    });
            }

            case SegmentType.Temp:
            {
                var tempAddress = BaseTempAddress + index;
                var memoryAddressComment = $"temp + {index}";
                
                return new AsmCodeSection($"Pop M[temp + {index}]",
                    new IAsmOutput[]
                    {
                        DropStack(),
                        TopStackToD(),
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
        new(operatorName,
            new[]
            {
                DropStack(),
                OperatorMemoryToMemory(operatorSymbol, commentOperator),
                LiftStack(),
            });

    private static AsmCodeSection WriteBinaryOperator(string operatorSymbol, string operatorName, string commentOperator) =>
        new(operatorName,
            new []
            {
                BinaryOperatorToD(operatorSymbol, commentOperator),
                PushD()
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
                        ValueToD(label),
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
                DropStack(),
                TopStackToD(),
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
            codeLines.Add(ValueToD("0"));
            for (var i = 0; i < numLocals; i++)
            {
                codeLines.Add(PushD());
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
                AInstruction(escapedFunctionName),
                new AsmCodeLine("D=A", $"{escapedFunctionName}=> D"),
                DToMemory("R14"),
                AInstruction(numArguments.ToString()),
                new AsmCodeLine("D=A", "Number Of Arguments => D"),
                DToMemory("R15"),
                AInstruction(label),
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

    private static AsmCodeSection BinaryOperatorToD(string operatorSymbol, string commentOperator) =>
        new(new []
        {
            PopToD(),
            DToMemory("R13"),
            PopToD(),
            DOperatorMemoryToD("R13", operatorSymbol, commentOperator)
        });

    private static AsmCodeSection SetMemoryToValue(string memoryAddress, string value) =>
        new($"Set {memoryAddress} to '{value}'",
            new IAsmOutput[]
            {
                AInstruction(value),
                new AsmCodeLine("D=A", $"{value} => D"),
                DToMemory(memoryAddress)
            });

    private static AsmCodeSection PushD() =>
        new(new []
        {
            DToTopStack(),
            LiftStack()
        });

    private static AsmCodeSection PopToD() =>
        new(new[]
        {
            DropStack(),
            TopStackToD()
        });

    private static AsmCodeSection DToMemory(string memoryAddress) =>
        new(new []
        {
            AInstruction(memoryAddress),
            new("M=D", $"D => {memoryAddress}")
        });

    private static AsmCodeSection MemoryToD(string memoryAddress, string memoryAddressComment) =>
        new(new []
        {
            AInstruction(memoryAddress),
            new("D=M", $"{memoryAddressComment} => D")
        });
    
    private static AsmCodeSection ValueToD(string value) =>
        new(new []
        {
            AInstruction(value),
            new("D=A", $"{value} => D")
        });

    private static AsmCodeSection NegativeValueToD(string value) =>
        new(new []
        {
            AInstruction(value),
            new("D=-A", $"-{value} => D")
        });

    private static AsmCodeSection DOperatorMemoryToD(string memoryAddress, string operatorSymbol, string commentOperator) =>
        new(new []
        {
            AInstruction(memoryAddress),
            new($"D=D{operatorSymbol}M", $"D {commentOperator} M[{memoryAddress}] => D")
        });

    private static AsmCodeSection IndirectMemoryToD(string memoryAddress, uint index, string commentMemoryAddress) 
    {
        if (index == 0)
        {
            return new(new []
            {
                AInstruction(memoryAddress),
                new("A=M", $"M[{commentMemoryAddress}] => A"),
                new("D=M", $"M[M[{commentMemoryAddress}] + 0] => D")
            });
        }
        else
        {
            return new(new IAsmOutput[]
            {
                ValueToD(index.ToString()),
                AInstruction(memoryAddress),
                new AsmCodeLine("A=M", $"M[{commentMemoryAddress}] => A"),
                new AsmCodeLine("A=D+A", $"M[{commentMemoryAddress}] + {index} => A"),
                new AsmCodeLine("D=M", $"M[M[{commentMemoryAddress}] + {index}] => D")
            });
        }
    }

    private static AsmCodeSection DToIndirectMemory(string memoryAddress, string commentMemoryAddress) =>
        new(new[]
        {
            AInstruction(memoryAddress),
            new("A=M", $"{commentMemoryAddress} => A"),
            new("M=D", $"D => {commentMemoryAddress}")
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
                AInstruction(fromMemoryAddress),
                new AsmCodeLine("D=M", $"M[{commentFromMemoryAddress}] => D"),
                DToMemory(toMemoryAddress)
            });
        }
        else
        {
            return new(new IAsmOutput[]
            {
                AInstruction(fromMemoryAddress),
                new AsmCodeLine("D=M", $"M[{commentFromMemoryAddress}] => D"),
                AInstruction(index.ToString()),
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
                AInstruction(toMemoryAddress),
                new("A=M-1", $"{commentToMemoryAddress} - {-index} => A"),
                new("D=M", $"M[{commentToMemoryAddress}-{-index}] => D")
            });
        }
        else if (index == 1)
        {
            return new(new[]
            {
                AInstruction(toMemoryAddress),
                new("A=M+1", $"{commentToMemoryAddress} + {index} => A"),
                new("D=M", $"M[{commentToMemoryAddress}+{index}] => D")
            });
        }
        else if (index < 0)
        {
            return new( new[]
            {
                AInstruction(toMemoryAddress),
                new("D=M", $"{commentToMemoryAddress} => D"),
                AInstruction((-index).ToString()),
                new("A=D-A", $"{commentToMemoryAddress}-{-index} => A"),
                new("D=M", $"M[{commentToMemoryAddress}-{-index}] => D")
            });
        }
        else
        {
            return new( new[]
            {
                AInstruction(toMemoryAddress),
                new("D=M", $"{commentToMemoryAddress} => D"),
                AInstruction(index.ToString()),
                new("A=D+A", $"{commentToMemoryAddress}+{index} => A"),
                new("D=M", $"M[{commentToMemoryAddress}+{index}] => D")
            });
        }
    }
    
    private static AsmCodeSection OperatorMemoryToMemory(string operatorSymbol, string commentOperator) =>
        new(new AsmCodeLine[]
        {
            new("A=M"),
            new($"M={operatorSymbol}M", $"{commentOperator}M => M")
        });

    private static AsmCodeSection DToTopStack() =>
        new(new []
        {
            AInstruction("SP"),
            new("A=M"),
            new("M=D", "D => TopStack")
        });

    private static AsmCodeSection TopStackToD() =>
        new(new AsmCodeLine[]
        {
            new("A=M"),
            new("D=M", "TopStack => D")
        });
    
    private static AsmCodeSection LiftStack() =>
        new(new []
        {
            AInstruction("SP"),
            new("M=M+1", "Lift Stack")
        });
    
    private static AsmCodeSection DropStack() =>
        new(new []
        {
            AInstruction("SP"),
            new("M=M-1", "Drop Stack")
        });

    private static AsmCodeLine WriteLabel(string label)
        => new($"({label})");

    private static AsmCodeSection UnconditionalJump(string address) =>
        new(new []
        {
            AInstruction(address),
            new("0;JMP", $"goto {address}")
        });

    private static AsmCodeSection ConditionalJump(string jumpType, string address)
    {
        if (jumpType == "JNE")
        {
            return new(new []
            {
                AInstruction(address),
                new($"D;{jumpType}", $"if D!= 0 then goto {address}")
            });
        }
        else
        {
            return new(new []
            {
                AInstruction(address),
                new($"D;{jumpType}", $"goto {address}")
            });
        }
    }

    private static AsmCodeSection UnconditionalJumpToAddressInMemory(string memoryAddress) =>
        new(new []
        {
            AInstruction(memoryAddress),
            new("A=M"),
            new("0;JMP", $"goto {memoryAddress}")
        });

    private static AsmCodeLine AInstruction(string value)
        => new($"@{value}");
    
}