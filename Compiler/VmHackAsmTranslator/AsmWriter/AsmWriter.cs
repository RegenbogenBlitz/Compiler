using FileHandling;
using VmHackAsmTranslator.Parsing;

namespace VmHackAsmTranslator.AsmWriter;

public static class AsmWriter
{
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

    private static IEnumerable<IAsmOutput> WriteHeader()
    {
        return new IAsmOutput[]
        {
            new AsmCodeSection($"Set {StackPointerAddress} to '{BaseStackAddress}'",
                new IAsmOutput[]
                {
                    ValueToD(BaseStackAddress.ToString()),
                    DToMemory(StackPointerAddress)
                }),
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
                        DropStack(),
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
                        DropStack(),
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
                            new AsmCodeLine(string.Empty, "M(LCL)=FRAME"),
                            new AsmCodeLine("A=M-D", "A <= FRAME-5"),
                            new AsmCodeLine(string.Empty, "M(FRAME - 5) = return-address"),
                            new AsmCodeLine("D=M", "D <= return-address"),
                            DToMemory("R13", "return-address"),
                        }),
                        new AsmCodeSection("Reposition the return value of the caller", new[]
                        {
                            PopToIndirectMemory("ARG", 0, "Return Value")
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
                            new AsmCodeLine("M=D", "M(THAT) <= CALLER_THAT")
                        }),
                        new AsmCodeSection("Restore THIS of the caller", new IAsmOutput[]
                        {
                            AInstruction("R14"),
                            new AsmCodeLine("AM=M-1", "M(R14) <= FRAME-2, R14 <= FRAME-2"),
                            new AsmCodeLine("D=M", "D <= CALLER_THIS = M(FRAME-2)"),
                            AInstruction("THIS"),
                            new AsmCodeLine("M=D", "M(THIS) <= CALLER_THIS")
                        }),
                        new AsmCodeSection("Restore ARG of the caller", new IAsmOutput[]
                        {
                            AInstruction("R14"),
                            new AsmCodeLine("AM=M-1", "M(R14) <= FRAME-3, R14 <= FRAME-3"),
                            new AsmCodeLine("D=M", "D <= CALLER_ARG = M(FRAME-3)"),
                            AInstruction("ARG"),
                            new AsmCodeLine("M=D", "M(ARG) <= CALLER_ARG")
                        }),
                        new AsmCodeSection("Restore LCL of the caller", new IAsmOutput[]
                        {
                            AInstruction("R14"),
                            new AsmCodeLine("AM=M-1", "M(R14) <= FRAME-4, R14 <= FRAME-4"),
                            new AsmCodeLine("D=M", "D <= CALLER_LCL = M(FRAME-4)"),
                            AInstruction("LCL"),
                            new AsmCodeLine("M=D", "M(LCL) <= CALLER_LCL")
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
                        PushD_SpPointToTopStackValue(),
                        MemoryToD("ARG", "M(ARG)"),
                        PushD_SpPointToTopStackValue(),
                        MemoryToD("THIS", "M(THIS)"),
                        PushD_SpPointToTopStackValue(),
                        MemoryToD("THAT", "M(THAT)"),
                        PushD_SpPointToTopStackValue(),

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
    }

    private static AsmCodeSection WritePush(string className, SegmentType segment, uint index)
    {
        switch (segment)
        {
            case SegmentType.Argument:
                return new AsmCodeSection($"Push M(M(ARG) + {index})",
                    new []
                    {
                        IndirectMemoryToD("ARG", index),
                        PushD_SpPointsAboveTopStackValue()
                    });
            
            case SegmentType.Local:
                return new AsmCodeSection($"Push M(M(LCL) + {index})",
                    new []
                    {
                        IndirectMemoryToD("LCL", index),
                        PushD_SpPointsAboveTopStackValue()
                    });
            
            case SegmentType.Static:
                return new AsmCodeSection($"Push M(Static {index})",
                    new []
                    {
                        MemoryToD($"{className}.{index}", $"M(M(Static {index}))"),
                        PushD_SpPointsAboveTopStackValue()
                    });
            
            case SegmentType.Constant:
                return index == 0
                    ? new AsmCodeSection("Push Constant '0'",
                        new IAsmOutput[]
                        {
                            LiftStack(),
                            new AsmCodeLine("A=M-1", "A <= M(SP)-1"),
                            new AsmCodeLine("M=0", "M(M(SP)-1) = 0")
                        })
                    : new AsmCodeSection($"Push Constant '{index}'",
                        new[]
                        {
                            ValueToD(index.ToString()),
                            PushD_SpPointsAboveTopStackValue()
                        });
            
            case SegmentType.This:
                return new AsmCodeSection($"Push M(M(THIS) + {index})",
                    new []
                    {
                        IndirectMemoryToD("THIS", index),
                        PushD_SpPointsAboveTopStackValue()
                    });
            
            case SegmentType.That:
                return new AsmCodeSection($"Push M(M(THAT) + {index})",
                    new []
                    {
                        IndirectMemoryToD("THAT", index),
                        PushD_SpPointsAboveTopStackValue()
                    });
            
            case SegmentType.Pointer:
                var pointerAddress = index == 0 ? "THIS" : "THAT";

                return new AsmCodeSection($"Push M({pointerAddress})",
                    new []
                    {
                        MemoryToD(pointerAddress, $"pointer + {index}"),
                        PushD_SpPointsAboveTopStackValue()
                    });
            
            case SegmentType.Temp:
                var tempAddress = "R" + (BaseTempAddress + index);
                
                return new AsmCodeSection($"Push M({tempAddress})",
                    new []
                    {
                        MemoryToD(tempAddress, $"temp + {index}"),
                        PushD_SpPointsAboveTopStackValue()
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
                return new AsmCodeSection($"Pop M(M(ARG) + {index})",
                    new []
                    {
                        OffsetMemoryToMemory("ARG", "Argument", index, "R13"),
                        PopToIndirectMemory("R13", index, $"M(ARG) + {index}")
                    });
            
            case SegmentType.Local:
                return new AsmCodeSection($"Pop M(M(LCL) + {index})",
                    new []
                    {
                        OffsetMemoryToMemory("LCL", "Local", index, "R13"),
                        PopToIndirectMemory("R13", index, $"M(LCL) + {index}")
                    });
            
            case SegmentType.Static:
                return new AsmCodeSection($"Pop M(Static {index})",
                    new IAsmOutput[]
                    {
                        PopToD(),
                        AInstruction($"{className}.{index}"),
                        new AsmCodeLine("M=D", $"M(Static {index}) <= D")
                    });
            
            case SegmentType.This:
                return new AsmCodeSection($"Pop M(M(THIS) + {index})",
                    new[]
                    {
                        PopToIndirectMemory("THIS", index)
                    });
            
            case SegmentType.That:
                return new AsmCodeSection($"Pop M(M(THAT) + {index})",
                    new []
                    {
                        PopToIndirectMemory("THAT", index)
                    });
            case SegmentType.Pointer:
            {
                var pointerAddress = index == 0 ? "THIS" : "THAT";
                
                return new AsmCodeSection($"Pop M({pointerAddress})",
                    new IAsmOutput[]
                    {
                        PopToD(),
                        new AsmCodeLine($"@{pointerAddress}", $"A <= {pointerAddress}"),
                        new AsmCodeLine("M=D", $"M({pointerAddress}) <= D")
                    });
            }

            case SegmentType.Temp:
            {
                var tempAddress = "R" + (BaseTempAddress + index);
                
                return new AsmCodeSection($"Pop M({tempAddress})",
                    new IAsmOutput[]
                    {
                        PopToD(),
                        new AsmCodeLine($"@{tempAddress}", $"A <= {tempAddress}"),
                        new AsmCodeLine("M=D", $"M({tempAddress}) <= D")
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
                DropStack(),
                new AsmCodeLine($"M={operatorSymbol}M", $"M <= {commentOperator}M")
            });

    private static AsmCodeSection WriteBinaryOperator(string operatorSymbol, string operatorName, string commentOperator) =>
        new(operatorName,
            new IAsmOutput[]
            {
                PopToD(),
                new AsmCodeLine("A=A-1", "SP <= SP - 1"),
                operatorSymbol == "-" 
                    ? new AsmCodeLine("M=M-D", "M <= M - D")
                    : new AsmCodeLine($"M=D{operatorSymbol}M", $"M <= M {commentOperator} D")
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
                codeLines.Add(PushD_SpPointsAboveTopStackValue());
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
                new AsmCodeLine("D=A", "D <= Number Of Arguments"),
                DToMemory("R13"),
                AInstruction(escapedFunctionName),
                new AsmCodeLine("D=A", $"D <= {escapedFunctionName}"),
                DToMemory("R14"),
                AInstruction(label),
                new AsmCodeLine("D=A", $"D <= {escapedFunctionName}"),
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
    
    private static AsmCodeSection PushD_SpPointToTopStackValue() =>
        new(new []
        {
            AInstruction("SP"),
            new("AM=M+1", "Lift stack and point to top-stack-value"),
            new("M=D", "TopStack <= D")
        });
    
    private static AsmCodeSection PushD_SpPointsAboveTopStackValue() =>
        new(new[]
        {
            AInstruction("SP"),
            new("AM=M+1", "Lift stack and point to above top-stack-value"),
            new("A=A-1", "A = address of top-stack-value"),
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

    private static AsmCodeSection DropStack() =>
        new(new []
        {
            AInstruction("SP"),
            new AsmCodeLine("A=M-1", "Drop Stack"),
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
            new("D=A", $"D <= {value}")
        });

    private static AsmCodeSection IndirectMemoryToD(string memoryAddress, uint index) 
    {
        if (index == 0)
        {
            return new(new []
            {
                AInstruction(memoryAddress),
                new("A=M", $"A <= M({memoryAddress})"),
                new("D=M", $"D <= M(M({memoryAddress}) + 0)")
            });
        }
        else if (index == 1)
        {
            return new(new IAsmOutput[]
            {
                AInstruction(memoryAddress),
                new AsmCodeLine("A=M+1", $"A <= M({memoryAddress}) + 1"),
                new AsmCodeLine("D=M", $"D <= M(M({memoryAddress}) + 1)")
            });
        }
        else if (index == 2)
        {
            var asmOutput = new List<IAsmOutput>
            {
                AInstruction(memoryAddress),
                new AsmCodeLine("A=M+1", $"A <= M({memoryAddress}) + 1")
            };
            for (var i = 2; i <= index; i++)
            {
                asmOutput.Add(new AsmCodeLine("A=A+1", $"A <= M({memoryAddress}) + {i}"));
            }
            asmOutput.Add(new AsmCodeLine("D=M", $"D <= M(M({memoryAddress}) + {index})"));
            
            return new(asmOutput);
        }
        else
        {
            return new(new []
            {
                AInstruction(memoryAddress),
                new AsmCodeLine("D=M", $"D <= M({memoryAddress})"),
                AInstruction(index.ToString()),
                new AsmCodeLine("A=D+A", $"A <= M({memoryAddress}) + {index}"),
                new AsmCodeLine("D=M", $"D <= M(M({memoryAddress}) + {index})")
            });
        }
    }

    private static AsmCodeSection PopToIndirectMemory(
        string memoryAddress,
        uint index,
        string? valueComment = null)
    {
        if (index == 0)
        {
            return new(new IAsmOutput[]
            {
                PopToD(),
                AInstruction(memoryAddress),
                new AsmCodeLine("A=M", $"A <= M({memoryAddress})"),
                new AsmCodeLine("M=D", $"M(M({memoryAddress})) <= {(string.IsNullOrEmpty(valueComment) ? "D" : valueComment)}")
            });
        }
        else if (index == 1)
        {
            return new(new IAsmOutput[]
            {
                PopToD(),
                AInstruction(memoryAddress),
                new AsmCodeLine("A=M+1", $"A <= M({memoryAddress}) + 1"),
                new AsmCodeLine("M=D", $"M(M({memoryAddress}) + 1) <= {(string.IsNullOrEmpty(valueComment) ? "D" : valueComment)}")
            });
        }
        else if (index < 5)
        {
            var asmOutput = new List<IAsmOutput>
            {
                PopToD(),
                AInstruction(memoryAddress),
                new AsmCodeLine("A=M+1", $"A <= M({memoryAddress}) + 1")
            };
            for (var i = 2; i <= index; i++)
            {
                asmOutput.Add(new AsmCodeLine("A=A+1", $"A <= M({memoryAddress}) + {i}"));
            }
            asmOutput.Add(new AsmCodeLine("M=D", $"M(M({memoryAddress}) + {index}) <= {(string.IsNullOrEmpty(valueComment) ? "D" : valueComment)}"));
            
            return new(asmOutput);
        }
        else
        {
            return new(new IAsmOutput[]
            {
                AInstruction(memoryAddress),
                new AsmCodeLine("D=M", $"D <= M({memoryAddress})"),
                AInstruction(index.ToString()),
                new AsmCodeLine("D=D+A", $"D <= M({memoryAddress})  + {index}"),
                AInstruction("R13"),
                new AsmCodeLine("M=D", $"M(R13) <= M({memoryAddress})  + {index}"),
                DropStackAndPointToTopOfStack(),
                new AsmCodeLine("D=M", $"D <= {(string.IsNullOrEmpty(valueComment) ? "D" : valueComment)}"),
                AInstruction("R13"),
                new AsmCodeLine("A=M", $"A <= M({memoryAddress})  + {index}"),
                new AsmCodeLine("M=D", $"M(M({memoryAddress})  + {index}) <= {(string.IsNullOrEmpty(valueComment) ? "D" : valueComment)}")
            });
        }
    }
        

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
                new AsmCodeLine("D=M", $"D <= M({commentFromMemoryAddress})"),
                DToMemory(toMemoryAddress)
            });
        }
        else
        {
            return new(new IAsmOutput[]
            {
                AInstruction(fromMemoryAddress),
                new AsmCodeLine("D=M", $"D <= M({commentFromMemoryAddress})"),
                AInstruction(index.ToString()),
                new AsmCodeLine("D=D+A", $"D <= M({commentFromMemoryAddress}) + {index}"),
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