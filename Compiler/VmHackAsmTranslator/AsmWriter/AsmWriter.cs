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
    
    private const string ReturnSubLabel = "RETURN_SUB";
    
    // ReSharper disable once RedundantDefaultMemberInitializer
    private static int _returnLabelNum = 0;
        
    public static OutputFileInfo Write(string outputFileName, VmCode vmCode)
    {
        var output = WriteHeader();

        const string functionName = "dummyClass.dummyFunction";
        foreach (var command in vmCode.Commands)
        {
            switch (command)
            {
                case PushCommand pushCommand:
                    output += WritePush(pushCommand.Segment, pushCommand.Index);
                    break;

                case PopCommand popCommand:
                    output += WritePop(popCommand.Segment, popCommand.Index, 0);
                    break;

                case ArithmeticCommand arithmeticCommand:
                {
                    switch (arithmeticCommand.ArithmeticCommandType)
                    {
                        case ArithmeticCommandType.Add:
                            output += WriteBinaryOperator("+", "Add", "+");
                            break;
                        
                        case ArithmeticCommandType.Sub:
                            output += WriteBinaryOperator("-", "Subtract", "-");
                            break;
                        
                        case ArithmeticCommandType.Neg:
                            output += WriteUnaryOperator("-", "Negative", "-");
                            break;
                        
                        case ArithmeticCommandType.And:
                            output += WriteBinaryOperator("&", "And", "and");
                            break;
                        
                        case ArithmeticCommandType.Or:
                            output += WriteBinaryOperator("|", "Or", "or");
                            break;
                        
                        case ArithmeticCommandType.Not:
                            output += WriteUnaryOperator("!", "Not", "not ");
                            break;
                        
                        case ArithmeticCommandType.Eq:
                            output += WriteComparison("Equals", EqualsReturnLabel, EqualsSubLabel);
                            break;
                        
                        case ArithmeticCommandType.Lt:
                            output += WriteComparison("Less Than", LessThanReturnLabel, LessThanSubLabel);
                            break;
                        
                        case ArithmeticCommandType.Gt:
                            output += WriteComparison("Greater Than", GreaterThanReturnLabel, GreaterThanSubLabel);
                            break;
                        
                        default:
                            throw new InvalidOperationException("Should not be reachable");
                    }
                    
                    break;
                }

                case LabelCommand labelCommand:
                    output += WriteFunctionQualifiedLabel(functionName, labelCommand.Symbol);
                    break;

                case IfGotoCommand ifGotoCommand:
                    output += WriteIfGoto(functionName, ifGotoCommand.Symbol);
                    break;
                    
                case GotoCommand gotoCommand:
                    output += WriteGoto(functionName, gotoCommand.Symbol);
                    break;

                case ReturnCommand:
                    output += WriteReturn();
                    break;

                case FunctionDeclarationCommand functionDeclarationCommand:
                    output += WriteFunctionDeclaration(
                        functionDeclarationCommand.FunctionName,
                        functionDeclarationCommand.NumLocals);
                    break;
                    
                case FunctionCallCommand functionCallCommand:
                {
                    var line = functionCallCommand.LineContent;
                    output += line + Environment.NewLine;
                    break;
                }
                
                case NonCommand:
                    // Do Nothing
                    break;
                
                default:
                    throw new InvalidOperationException("Should not be reachable");
            }
        }

        output += WriteFooter();
        
        return new OutputFileInfo(outputFileName, "asm", output);
    }

    private static string WriteHeader() =>
        OpenSectionComment("Reusable Sub Routines", 0) +
        UnconditionalJump(SkipSubsLabel, 1) +

        OpenSectionComment("Equals", 1) +
        WriteLabel(EqualsSubLabel) +
        BinaryOperatorToD("-", "-", 2) +
        PadLine("") + Comment("If D = 0 Then Goto IsTrue Else Goto IsFalse", 2) +
        ConditionalJump("JEQ", IsTrueLabel, 2) +
        UnconditionalJump(IsFalseLabel, 2) +
        CloseSectionComment(1) +

        OpenSectionComment("Is Less Than", 1) +
        WriteLabel(LessThanSubLabel) +
        BinaryOperatorToD("-", "-", 2) +
        PadLine("") + Comment("If D < 0 Then Goto IsTrue Else Goto IsFalse", 2) +
        ConditionalJump("JLT", IsTrueLabel, 2) +
        UnconditionalJump(IsFalseLabel, 2) +
        CloseSectionComment(1) +

        OpenSectionComment("Is Greater Than", 1) +
        WriteLabel(GreaterThanSubLabel) +
        BinaryOperatorToD("-", "-", 2) +
        PadLine("") + Comment("If D > 0 Then Goto IsTrue Else Goto IsFalse", 2) +
        ConditionalJump("JGT", IsTrueLabel, 2) +
        UnconditionalJump(IsFalseLabel, 2) +
        CloseSectionComment(1) +

        OpenSectionComment("ReusableComparison", 1) +

        OpenSectionComment("Is True", 2) +
        WriteLabel(IsTrueLabel) +
        NegativeValueToD("1", 3) +
        DToTopStack(3) +
        LiftStack(3) +
        UnconditionalJumpToAddressInMemory("R14", 3) +
        CloseSectionComment(2) +

        OpenSectionComment("Is False", 2) +
        WriteLabel(IsFalseLabel) +
        ValueToD("0", 3) +
        DToTopStack(3) +
        LiftStack(3) +
        UnconditionalJumpToAddressInMemory("R14", 3) +
        CloseSectionComment(2) +

        CloseSectionComment(1) +

        OpenSectionComment("Return", 1) +
        WriteLabel(ReturnSubLabel) +

        OpenSectionComment("FRAME  = LCL", 2) +
        MemoryToD("LCL", "M[Local]", 3) +
        DToMemory("R14", 3) +
        CloseSectionComment(2) +

        OpenSectionComment("RET = *(FRAME-5)", 2) +
        AInstruction(5.ToString()) +
        PadLine("A=D-A") + Comment("FRAME - 5 => A", 3) +
        PadLine("D=M") + Comment("M[FRAME - 5] => D", 3) +
        DToMemory("R15", 3) +
        CloseSectionComment(2) +

        OpenSectionComment("*ARG = pop()", 2) +
        WritePop(SegmentType.Argument, 0, 3) +
        CloseSectionComment(2) +

        OpenSectionComment("SP = ARG + 1", 2) +
        AInstruction("ARG") +
        PadLine("D=M+1") + Comment("M[Argument] + 1 => D", 3) +
        DToMemory("SP", 3) +
        CloseSectionComment(2) +

        OpenSectionComment("That = *(FRAME-1)", 2) +
        OffsetMemoryToD("R14", "FRAME", -1, 3) +
        DToMemory("THAT", 3) +
        CommentLine("That = M[FRAME-1]", 3) +
        CloseSectionComment(2) +

        OpenSectionComment("This = *(FRAME-2)", 2) +
        OffsetMemoryToD("R14", "FRAME", -2, 3) +
        DToMemory("THIS", 3) +
        CommentLine("This = M[FRAME-2]", 3) +
        CloseSectionComment(2) +

        OpenSectionComment("Argument = *(FRAME-3)", 2) +
        OffsetMemoryToD("R14", "FRAME", -3, 3) +
        DToMemory("ARG", 3) +
        CommentLine("Argument = M[FRAME-3]", 3) +
        CloseSectionComment(2) +

        OpenSectionComment("Local = *(FRAME-4)", 2) +
        OffsetMemoryToD("R14", "FRAME", -4, 3) +
        DToMemory("LCL", 3) +
        CommentLine("Local = M[FRAME-4]", 3) +
        CloseSectionComment(2) +

        OpenSectionComment("goto RET", 2) +
        AInstruction("R15") +
        PadLine("A=M") + Comment("M[RET] => A", 3) +
        PadLine("0;JMP") + Comment("goto RET", 3) +
        CloseSectionComment(2) +

        CloseSectionComment(1) +

        WriteLabel(SkipSubsLabel) +
        CloseSectionComment(0) +

        SetMemoryToValue(StackPointerAddress, BaseStackAddress.ToString(), 0);
    
    private static string WriteFooter() =>
        WriteLabel("END") +
        UnconditionalJump("END", 0);
    
    private static string WritePush(SegmentType segment, uint index)
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
                    MemoryToD($"StaticTest.{index}", $"M[M[Static {index}]]", 1) +
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

    private static string WritePop(SegmentType segment, uint index, int indentation)
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
                    AInstruction($"StaticTest.{index}") +
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
        var label = returnLabel + _returnLabelNum;
        var equalsSection =
            OpenSectionComment(operatorName, 0) +
            OpenSectionComment($"Set R14 to '{label}'", 1) +
            ValueToD(label, 2) +
            DToMemory("R14", 2) +
            CloseSectionComment(1) +
            UnconditionalJump(subLabel, 1) +
            WriteLabel(label) +
            CloseSectionComment(0);

        _returnLabelNum++;
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
        var code = CommentLine($"[Declare Function:{functionName} Locals:{numLocals}] {{", 0);
            code +=WriteLabel("$" + functionName);
        for (var i = 0; i < numLocals; i++)
        {
            code += WritePush(SegmentType.Constant, 0);
        }

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