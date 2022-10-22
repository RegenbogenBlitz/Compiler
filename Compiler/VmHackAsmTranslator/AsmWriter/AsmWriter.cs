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

    private const string StartEqualsSubLabel = "START_EQ";
    private const string EndEqualsSubLabel = "END_EQ";
    private const string StartLessThanSubLabel = "START_LT";
    private const string EndLessThanSubLabel = "END_LT";
    private const string StartGreaterThanSubLabel = "START_GT";
    private const string EndGreaterThanSubLabel = "END_GT";
    
    private const string EqualsReturnLabel = "EQUALS_RETURN_";
    private const string LessThanReturnLabel = "LESSTHAN_RETURN_";
    private const string GreaterThanReturnLabel = "GREATERTHAN_RETURN_";
    // ReSharper disable once RedundantDefaultMemberInitializer
    private static int _comparisionReturnLabelNum = 0;
    
    private const string CallSubLabel = "CALL_SUB";
    private const string ReturnSubLabel = "RETURN_SUB";
    
    private const string ReturnAddressLabel = "RET_ADDRESS_CALL";
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
                            asmOutputs.Add(WriteComparison("Equals", EqualsReturnLabel, StartEqualsSubLabel));
                            break;
                        
                        case ArithmeticCommandType.Lt:
                            asmOutputs.Add(WriteComparison("Less Than", LessThanReturnLabel, StartLessThanSubLabel));
                            break;
                        
                        case ArithmeticCommandType.Gt:
                            asmOutputs.Add(WriteComparison("Greater Than", GreaterThanReturnLabel, StartGreaterThanSubLabel));
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
            SetMemoryToValue(StackPointerAddress, BaseStackAddress.ToString()),
            new AsmCodeSection("Reusable Sub Routines", new IAsmOutput[]
            {
                UnconditionalJump(SkipSubsLabel),
                new AsmCodeSection("Equals",
                    new IAsmOutput[]
                    {
                        WriteLabel(StartEqualsSubLabel),
                        DToMemory("R15"),
                        PopToD(),
                        new AsmCodeLine("A=A-1"),
                        new AsmCodeLine("D=M-D"),
                        new AsmCodeLine("M=0", "Leave 'false' on stack"),
                        new AsmCodeLine(string.Empty, "If D = 0 Then continue Else goto EndEqualsSubLabel"),
                        ConditionalJump("JNE", EndEqualsSubLabel),
                        AInstruction("SP"),
                        new AsmCodeLine("A=M-1"),
                        new AsmCodeLine("M=-1", "Leave 'true' on stack"),
                        WriteLabel(EndEqualsSubLabel),
                        UnconditionalJumpToAddressInMemory("R15")
                    }),
                new AsmCodeSection("Is Greater Than",
                    new IAsmOutput[]
                    {
                        WriteLabel(StartGreaterThanSubLabel),
                        DToMemory("R15"),
                        PopToD(),
                        new AsmCodeLine("A=A-1"),
                        new AsmCodeLine("D=M-D"),
                        new AsmCodeLine("M=0", "Leave 'false' on stack"),
                        new AsmCodeLine(string.Empty, "If D > 0 Then continue Else goto EndGreaterThanSubLabel"),
                        ConditionalJump("JLE", EndGreaterThanSubLabel),
                        AInstruction("SP"),
                        new AsmCodeLine("A=M-1"),
                        new AsmCodeLine("M=-1", "Leave 'true' on stack"),
                        WriteLabel(EndGreaterThanSubLabel),
                        UnconditionalJumpToAddressInMemory("R15")
                    }),
                new AsmCodeSection("Is Less Than",
                    new IAsmOutput[]
                    {
                        WriteLabel(StartLessThanSubLabel),
                        DToMemory("R15"),
                        PopToD(),
                        new AsmCodeLine("A=A-1"),
                        new AsmCodeLine("D=M-D"),
                        new AsmCodeLine("M=0", "Leave 'false' on stack"),
                        new AsmCodeLine(string.Empty, "If D < 0 Then continue Else goto EndLessThanSubLabel"),
                        ConditionalJump("JGE", EndLessThanSubLabel),
                        AInstruction("SP"),
                        new AsmCodeLine("A=M-1"),
                        new AsmCodeLine("M=-1", "Leave 'true' on stack"),
                        WriteLabel(EndLessThanSubLabel),
                        UnconditionalJumpToAddressInMemory("R15")
                    }),
                new AsmCodeSection("Return",
                    new IAsmOutput[]
                    {
                        WriteLabel(ReturnSubLabel),
                        new AsmCodeSection("Put the return-address in a temp. var.", new IAsmOutput[]
                        {
                            AInstruction(5.ToString()),
                            new AsmCodeLine("D=A"),
                            AInstruction("LCL"),
                            new AsmCodeLine(string.Empty, "M[LCL]=FRAME"),
                            new AsmCodeLine("A=M-D", "FRAME-5 => A"),
                            new AsmCodeLine(string.Empty, "M[FRAME - 5] = return-address"),
                            new AsmCodeLine("D=M", "return-address => D"),
                            DToMemory("R13", "return-address"),
                        }),
                        new AsmCodeSection("Reposition the return value of the caller", new[]
                        {
                            PopToD(),
                            DToIndirectMemory("ARG", "M(ARG)", "Return Value")
                        }),
                        new AsmCodeSection("Restore SP of the caller", new IAsmOutput[]
                        {
                            new AsmCodeLine("D=A", "D = M(ARG)"),
                            AInstruction("SP"),
                            new AsmCodeLine("M=D+1", "M(SP) = M(ARG) + 1")
                        }),
                        MemoryToD("LCL", "FRAME"),
                        new AsmCodeSection("Restore THAT of the Caller", new IAsmOutput[]
                        {
                            AInstruction("R14"),
                            new AsmCodeLine("AM=D-1", "M(R14) <= FRAME-1, R14 <= FRAME-1"),
                            new AsmCodeLine("D=M", "D <= CALLER_THAT = M(FRAME-1)"),
                            AInstruction("THAT"),
                            new AsmCodeLine("M=D", "M[THAT] <= CALLER_THAT")
                        }),
                        new AsmCodeSection("Restore THIS of the caller", new IAsmOutput[]
                        {
                            AInstruction("R14"),
                            new AsmCodeLine("AM=M-1", "M(R14) <= FRAME-2, R14 <= FRAME-2"),
                            new AsmCodeLine("D=M", "D <= CALLER_THIS = M(FRAME-2)"),
                            AInstruction("THIS"),
                            new AsmCodeLine("M=D", "M[THIS] <= CALLER_THIS")
                        }),
                        new AsmCodeSection("Restore ARG of the caller", new IAsmOutput[]
                        {
                            AInstruction("R14"),
                            new AsmCodeLine("AM=M-1", "M(R14) <= FRAME-3, R14 <= FRAME-3"),
                            new AsmCodeLine("D=M", "D <= CALLER_ARG = M(FRAME-3)"),
                            AInstruction("ARG"),
                            new AsmCodeLine("M=D", "M[ARG] <= CALLER_ARG")
                        }),
                        new AsmCodeSection("Restore LCL of the caller", new IAsmOutput[]
                        {
                            AInstruction("R14"),
                            new AsmCodeLine("AM=M-1", "M(R14) <= FRAME-4, R14 <= FRAME-4"),
                            new AsmCodeLine("D=M", "D <= CALLER_LCL = M(FRAME-4)"),
                            AInstruction("LCL"),
                            new AsmCodeLine("M=D", "M[LCL] <= CALLER_LCL")
                        }),
                        new AsmCodeSection("goto return-address", new[]
                        {
                            AInstruction("R13"),
                            new AsmCodeLine("A=M", "A <= return-address"),
                            new AsmCodeLine("0;JMP", "goto return-address")
                        })
                    }),
                new AsmCodeSection("Call Function",
                    new IAsmOutput[]
                    {
                        WriteLabel(CallSubLabel),
                        AInstruction("SP"),
                        new AsmCodeLine("A=M"),
                        new AsmCodeLine("M=D", "TopStack <= return-address"),
                        MemoryToD("LCL", "M(LCL)"),
                        PushD(),
                        MemoryToD("ARG", "M(ARG)"),
                        PushD(),
                        MemoryToD("THIS", "M(THIS)"),
                        PushD(),
                        MemoryToD("THAT", "M(THAT)"),
                        PushD(),
                        
                        AInstruction("4"),
                        new AsmCodeLine("D=A", "D <= 4"),
                        AInstruction("R13"),
                        new AsmCodeLine("D=D+M", "D <= #arguments + 4"),
                        AInstruction("SP"),
                        new AsmCodeLine("D=M-D", "D <= M(SP) - #arguments - 4"),
                        DToMemory("ARG", "M(SP) - #arguments - 4"),
                        
                        AInstruction("SP"),
                        new AsmCodeLine("MD=M+1", "D, M(SP) <= M(SP) + 1"),
                        DToMemory("LCL", "M(SP) + 1"),
                        
                        new AsmCodeSection("Goto function address", new[]
                        {
                            UnconditionalJumpToAddressInMemory("R14")
                        })
                    }),
                WriteLabel(SkipSubsLabel),
            }),
            WriteFunctionCall("Sys.init", 0)
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
                        PopToD(),
                        DToIndirectMemory("R13", $"M[Argument] + {index}")
                    });
            
            case SegmentType.Local:
                return new AsmCodeSection($"Pop M[M[Local] + {index}]",
                    new []
                    {
                        OffsetMemoryToMemory("LCL", "Local", index, "R13"),
                        PopToD(),
                        DToIndirectMemory("R13", $"M[Local] + {index}")
                    });
            
            case SegmentType.Static:
                return new AsmCodeSection($"Pop M[Static {index}]",
                    new IAsmOutput[]
                    {
                        PopToD(),
                        AInstruction($"{className}.{index}"),
                        new AsmCodeLine("M=D", $"D => M[Static {index}]")
                    });
            
            case SegmentType.This:
                return new AsmCodeSection($"Pop M[M[This] + {index}]",
                    new[]
                    {
                        OffsetMemoryToMemory("THIS", "This", index, "R13"),
                        PopToD(),
                        DToIndirectMemory("R13", $"M[This] + {index}")
                    });
            
            case SegmentType.That:
                return new AsmCodeSection($"Pop M[M[That] + {index}]",
                    new []
                    {
                        OffsetMemoryToMemory("THAT", "That", index, "R13"),
                        PopToD(),
                        DToIndirectMemory("R13", $"M[That] + {index}")
                    });
            case SegmentType.Pointer:
            {
                var pointerAddress = BasePointerAddress + index;
                var memoryAddressComment = $"pointer + {index}";
                
                return new AsmCodeSection($"Pop M[pointer + {index}]",
                    new IAsmOutput[]
                    {
                        PopToD(),
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
                        PopToD(),
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
            new IAsmOutput[]
            {
                DropStackAndPointToTopOfStack(),
                new AsmCodeLine($"M={operatorSymbol}M", $"{commentOperator}M => M"),
                LiftStack(),
            });

    private static AsmCodeSection WriteBinaryOperator(string operatorSymbol, string operatorName, string commentOperator) =>
        new(operatorName,
            new []
            {
                new(new []
                {
                    PopToD(),
                    DToMemory("R13"),
                    PopToD(),
                    DOperatorMemoryToD("R13", operatorSymbol, commentOperator)
                }),
                PushD()
            });
    
    private static AsmCodeSection WriteComparison(string operatorName, string returnLabel, string subLabel)
    {
        var label = returnLabel + _comparisionReturnLabelNum;
        var equalsSection = new AsmCodeSection(operatorName,
            new IAsmOutput[]
            {
                ValueToD(label),
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
                PopToD(),
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
                AInstruction(numArguments.ToString()),
                new AsmCodeLine("D=A", "Number Of Arguments => D"),
                DToMemory("R13"),
                AInstruction(escapedFunctionName),
                new AsmCodeLine("D=A", $"{escapedFunctionName}=> D"),
                DToMemory("R14"),
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
            AInstruction("SP"),
            new("AM=M+1", "Lift stack and point to top of stack"),
            new("M=D", "TopStack <= D")
        });
    
    private static AsmCodeSection LiftStack() =>
        new(new []
        {
            AInstruction("SP"),
            new("M=M+1", "Lift Stack")
        });
    
    private static AsmCodeSection PopToD() =>
        new(new IAsmOutput[]
        {
            DropStackAndPointToTopOfStack(),
            new AsmCodeLine("D=M", "TopStack => D")
        });
    
    private static AsmCodeSection DropStackAndPointToTopOfStack() =>
        new(new []
        {
            AInstruction("SP"),
            new AsmCodeLine("AM=M-1", "Drop Stack, Point to TopStack"),
        });

    private static AsmCodeSection DToMemory(string memoryAddress, string? valueComment = null) =>
        new(new []
        {
            AInstruction(memoryAddress),
            new("M=D", $"M({memoryAddress}) <= {(string.IsNullOrEmpty(valueComment) ? "D" : valueComment)}")
        });

    private static AsmCodeSection MemoryToD(string memoryAddress, string memoryAddressComment) =>
        new(new []
        {
            AInstruction(memoryAddress),
            new("D=M", $"D <= {memoryAddressComment}")
        });
    
    private static AsmCodeSection ValueToD(string value) =>
        new(new []
        {
            AInstruction(value),
            new("D=A", $"{value} => D")
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

    private static AsmCodeSection DToIndirectMemory(string memoryAddress, string commentMemoryAddress, string? valueComment = null) =>
        new(new[]
        {
            AInstruction(memoryAddress),
            new("A=M", $"A <= {commentMemoryAddress}"),
            new("M=D", $"M({memoryAddress}) <= {(string.IsNullOrEmpty(valueComment) ? "D" : valueComment)}")
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