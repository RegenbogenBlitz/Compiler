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
                    asmOutputs.Add(new AsmCodeLine(WritePush(pushCommand.ClassName, pushCommand.Segment, pushCommand.Index)));
                    break;

                case PopCommand popCommand:
                    asmOutputs.Add(new AsmCodeLine(WritePop(popCommand.ClassName, popCommand.Segment, popCommand.Index, 0)));
                    break;

                case ArithmeticCommand arithmeticCommand:
                {
                    switch (arithmeticCommand.ArithmeticCommandType)
                    {
                        case ArithmeticCommandType.Add:
                            asmOutputs.Add(new AsmCodeLine(WriteBinaryOperator("+", "Add", "+")));
                            break;
                        
                        case ArithmeticCommandType.Sub:
                            asmOutputs.Add(new AsmCodeLine(WriteBinaryOperator("-", "Subtract", "-")));
                            break;
                        
                        case ArithmeticCommandType.Neg:
                            asmOutputs.Add(new AsmCodeLine(WriteUnaryOperator("-", "Negative", "-")));
                            break;
                        
                        case ArithmeticCommandType.And:
                            asmOutputs.Add(new AsmCodeLine(WriteBinaryOperator("&", "And", "and")));
                            break;
                        
                        case ArithmeticCommandType.Or:
                            asmOutputs.Add(new AsmCodeLine(WriteBinaryOperator("|", "Or", "or")));
                            break;
                        
                        case ArithmeticCommandType.Not:
                            asmOutputs.Add(new AsmCodeLine(WriteUnaryOperator("!", "Not", "not ")));
                            break;
                        
                        case ArithmeticCommandType.Eq:
                            asmOutputs.Add(new AsmCodeLine(WriteComparison("Equals", EqualsReturnLabel, EqualsSubLabel)));
                            break;
                        
                        case ArithmeticCommandType.Lt:
                            asmOutputs.Add(new AsmCodeLine(WriteComparison("Less Than", LessThanReturnLabel, LessThanSubLabel)));
                            break;
                        
                        case ArithmeticCommandType.Gt:
                            asmOutputs.Add(new AsmCodeLine(WriteComparison("Greater Than", GreaterThanReturnLabel, GreaterThanSubLabel)));
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
                    asmOutputs.Add(new AsmCodeLine(WriteIfGoto(ifGotoCommand.FunctionName, ifGotoCommand.Symbol)));
                    break;
                    
                case GotoCommand gotoCommand:
                    asmOutputs.Add(new AsmCodeLine(WriteGoto(gotoCommand.FunctionName, gotoCommand.Symbol)));
                    break;

                case ReturnCommand:
                    asmOutputs.Add(new AsmCodeLine(WriteReturn()));
                    break;

                case FunctionDeclarationCommand functionDeclarationCommand:
                    asmOutputs.Add(new AsmCodeLine(WriteFunctionDeclaration(
                        functionDeclarationCommand.FunctionName,
                        functionDeclarationCommand.NumLocals)));
                    break;
                    
                case FunctionCallCommand functionCallCommand:
                    asmOutputs.Add(new AsmCodeLine(WriteFunctionCall(
                        functionCallCommand.FunctionName,
                        functionCallCommand.NumArguments)));
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
                            new AsmCodeLine(WritePop(string.Empty, SegmentType.Argument, 0, 3))
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
            new AsmCodeLine(SetMemoryToValue(StackPointerAddress, BaseStackAddress.ToString(), 0)),
            new AsmCodeLine(WriteFunctionCall("Sys.init", 0)),
            new AsmCodeLine(WriteLabel("END")),
            new AsmCodeLine(UnconditionalJump("END", 0))
        };
    
    private static string WritePush(string className, SegmentType segment, uint index)
    {
        switch (segment)
        {
            case SegmentType.Argument:
                return
                    OpenSectionComment($"Push M[M[Argument] + {index}]", 0) +
                    IndirectMemoryToD("ARG", index, "Argument", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            case SegmentType.Local:
                return
                    OpenSectionComment($"Push M[M[Local] + {index}]", 0) +
                    IndirectMemoryToD("LCL", index, "Local", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            case SegmentType.Static:
                return
                    OpenSectionComment($"Push M[Static {index}]", 0) +
                    MemoryToD($"{className}.{index}", $"M[M[Static {index}]]", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            case SegmentType.Constant:
                return
                    OpenSectionComment($"Push Constant '{index}'", 0) +
                    ValueToD(index.ToString(), 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            case SegmentType.This:
                return
                    OpenSectionComment($"Push M[M[This] + {index}]", 0) +
                    IndirectMemoryToD("THIS", index, "This", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            case SegmentType.That:
                return
                    OpenSectionComment($"Push M[M[That] + {index}]", 0) +
                    IndirectMemoryToD("THAT", index, "That", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            case SegmentType.Pointer:
                var pointerAddress = BasePointerAddress + index;
                
                return
                    OpenSectionComment($"Push M[pointer + {index}]", 0) +
                    MemoryToD(pointerAddress.ToString(), $"pointer + {index}", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            case SegmentType.Temp:
                var tempAddress = BaseTempAddress + index;
                
                return
                    OpenSectionComment($"Push M[temp + {index}]", 0) +
                    MemoryToD(tempAddress.ToString(), $"temp + {index}", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            default:
                throw new InvalidOperationException("Should not be reachable");
        }
    }

    private static string WritePop(string className, SegmentType segment, uint index, int indentation)
    {
        switch (segment)
        {
            case SegmentType.Argument:
                return
                    OpenSectionComment($"Pop M[M[Argument] + {index}]", indentation) +
                    OffsetMemoryToMemory("ARG", "Argument", index, "R13", indentation + 1) +
                    DropStack(indentation + 1) +
                    TopStackToD(indentation + 1) +
                    DToIndirectMemory("R13", $"M[Argument] + {index}", indentation + 1) +
                    CloseSectionComment(indentation);
            
            case SegmentType.Local:
                return
                    OpenSectionComment($"Pop M[M[Local] + {index}]", indentation) +
                    OffsetMemoryToMemory("LCL", "Local", index, "R13", indentation + 1) +
                    DropStack(indentation + 1) +
                    TopStackToD(indentation + 1) +
                    DToIndirectMemory("R13", $"M[Local] + {index}", indentation + 1) +
                    CloseSectionComment(indentation);
            
            case SegmentType.Static:
                return
                    OpenSectionComment($"Pop M[Static {index}]", indentation) +
                    DropStack(indentation + 1) +
                    TopStackToD(indentation + 1) +
                    AInstruction($"{className}.{index}") +
                    PadLine("M=D") + Comment($"D => M[Static {index}]", indentation + 1) +
                    CloseSectionComment(indentation);
            
            case SegmentType.This:
                return
                    OpenSectionComment($"Pop M[M[This] + {index}]", indentation) +
                    OffsetMemoryToMemory("THIS", "This", index, "R13", indentation + 1) +
                    DropStack(indentation + 1) +
                    TopStackToD(indentation + 1) +
                    DToIndirectMemory("R13", $"M[This] + {index}", indentation + 1) +
                    CloseSectionComment(indentation);
            
            case SegmentType.That:
                return
                    OpenSectionComment($"Pop M[M[That] + {index}]", indentation) +
                    OffsetMemoryToMemory("THAT", "That", index, "R13", indentation + 1) +
                    DropStack(indentation + 1) +
                    TopStackToD(indentation + 1) +
                    DToIndirectMemory("R13", $"M[That] + {index}", indentation + 1) +
                    CloseSectionComment(indentation);

            case SegmentType.Pointer:
            {
                var pointerAddress = BasePointerAddress + index;
                var memoryAddressComment = $"pointer + {index}";
                return
                    OpenSectionComment($"Pop M[pointer + {index}]", indentation) +
                    DropStack(indentation + 1) +
                    TopStackToD(indentation + 1) +
                    PadLine($"@{pointerAddress.ToString()}") +
                    Comment($"{memoryAddressComment} => A", indentation + 1) +
                    PadLine("M=D") + Comment($"D => {memoryAddressComment}", indentation + 1) +
                    CloseSectionComment(indentation);
            }

            case SegmentType.Temp:
            {
                var tempAddress = BaseTempAddress + index;
                var memoryAddressComment = $"temp + {index}";
                return
                    OpenSectionComment($"Pop M[temp + {index}]", indentation) +
                    DropStack(indentation + 1) +
                    TopStackToD(indentation + 1) +
                    PadLine($"@{tempAddress.ToString()}") + Comment($"{memoryAddressComment} => A", indentation + 1) +
                    PadLine("M=D") + Comment($"D => {memoryAddressComment}", indentation + 1) +
                    CloseSectionComment(indentation);
            }
            // ReSharper disable once RedundantCaseLabel
            case SegmentType.Constant:
            default:
                throw new InvalidOperationException("Should not be reachable");
        }
    }
    
    private static string WriteUnaryOperator(string operatorSymbol, string operatorName, string commentOperator) =>
        OpenSectionComment(operatorName, 0) +
        DropStack(1) +
        OperatorMemoryToMemory(operatorSymbol, commentOperator, 1) +
        LiftStack(1) +
        CloseSectionComment(0);

    private static string WriteBinaryOperator(string operatorSymbol, string operatorName, string commentOperator) =>
        OpenSectionComment(operatorName, 0) +
        BinaryOperatorToD(operatorSymbol, commentOperator, 1) +
        PushD(1) +
        CloseSectionComment(0);
    
    private static string WriteComparison(string operatorName, string returnLabel, string subLabel)
    {
        var label = returnLabel + _comparisionReturnLabelNum;
        var equalsSection =
            OpenSectionComment(operatorName, 0) +
            OpenSectionComment($"Set R14 to '{label}'", 1) +
            ValueToD(label, 2) +
            DToMemory("R14", 2) +
            CloseSectionComment(1) +
            UnconditionalJump(subLabel, 1) +
            WriteLabel(label) +
            CloseSectionComment(0);

        _comparisionReturnLabelNum++;
        return equalsSection;
    }

    private static string WriteIfGoto(string functionName, string label) =>
        OpenSectionComment($"If-Goto {ToAsmFunctionQualifiedLabel(functionName, label)}", 0) +
        DropStack(1) +
        TopStackToD(1) +
        ConditionalJump("JNE", ToAsmFunctionQualifiedLabel(functionName, label), 1) +
        CloseSectionComment(0);
    
    private static string WriteGoto(string functionName, string label) =>
        OpenSectionComment($"Goto {ToAsmFunctionQualifiedLabel(functionName, label)}", 0) +
        UnconditionalJump(ToAsmFunctionQualifiedLabel(functionName, label), 1) +
        CloseSectionComment(0);
    
    private static string WriteReturn() =>
        OpenSectionComment("Return", 0) +
        UnconditionalJump(ReturnSubLabel, 1) +
        CloseSectionComment(0);
    
    private static string WriteFunctionDeclaration(string functionName, uint numLocals)
    {
        var code =
            OpenSectionComment($"Declare Function:{functionName} Locals:{numLocals}", 0) +
            WriteLabel("$" + functionName);
        
        if (numLocals > 0)
        {
            code += ValueToD(0.ToString(), 1);
            for (var i = 0; i < numLocals; i++)
            {
                code += PushD(1);
            }
        }
        
        code += CloseSectionComment(0);
        return code;
    }

    private static string WriteFunctionCall(string functionName, uint numArguments)
    {
        var label = ReturnAddressLabel + _functionReturnLabelNum;
        string escapedFunctionName = "$" + functionName;
        var code =
            CommentLine($"[Call Function:{functionName} Args:{numArguments}] {{", 0) +
            AInstruction(escapedFunctionName) +
            PadLine("D=A") + Comment($"{escapedFunctionName}=> D", 1) +
            DToMemory("R14", 1) +
            AInstruction(numArguments.ToString()) +
            PadLine("D=A") + Comment("Number Of Arguments => D", 1) +
            DToMemory("R15", 1) +
            AInstruction(label) +
            PadLine("D=A") + Comment($"{escapedFunctionName}=> D", 1) +
            UnconditionalJump(CallSubLabel, 1) +
            WriteLabel(label) +
            CloseSectionComment(0);
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
    
    private static string SetMemoryToValue(string memoryAddress, string value, int indentation) =>
        OpenSectionComment($"Set {memoryAddress} to '{value}'", indentation) +
        AInstruction(value) +
        PadLine("D=A") + Comment($"{value} => D", indentation + 1) +
        DToMemory(memoryAddress, indentation + 1) +
        CloseSectionComment(indentation);

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
    
    private static string OpenSectionComment(string comment, int indentation)
        => CommentLine("[" + comment +  "] {", indentation);

    private static string CloseSectionComment(int indentation)
        => CommentLine("}", indentation);
    
    private static string CommentLine(string comment, int indentation)
        => PadLine("") + " // "+ "".PadRight(indentation * 3, ' ') + comment + Environment.NewLine;
    
    private static string Comment(string comment, int indentation)
        => " // "+ "".PadRight(indentation * 3, ' ') + comment + Environment.NewLine;
    
    private static string PadLine(string value)
        => value.PadRight(25, ' ');
    
}