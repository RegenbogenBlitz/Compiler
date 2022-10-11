namespace VmHackAsmTranslator;

public static class VmTranslator
{
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
    private static int _returnLabelNum = 0;
        
    public static string Translate(string[] lines)
    {
        var output = WriteHeader();

        var lineNumber = 0;
        foreach (var line in lines)
        {
            lineNumber++;
            var trimmedLine = TrimLine(line);
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }
            
            var lineComponents = trimmedLine.Split(' ');

            switch (lineComponents[0])
            {
                case "push":
                    if (lineComponents.Length != 3)
                    {
                        throw new TranslationException(lineNumber, line, "expected Push SEGMENT INDEX");
                    }

                    output += WritePush(lineComponents[1], lineComponents[2], line);
                    break;

                case "add":
                    output += WriteBinaryOperator("+", "Add", "+");
                    break;
                
                case "sub":
                    output += WriteBinaryOperator("-", "Subtract", "-");
                    break;
                
                case "neg":
                    output += WriteUnaryOperator("-", "Negative", "-");
                    break;
                
                case "and":
                    output += WriteBinaryOperator("&", "And", "and");
                    break;
                
                case "or":
                    output += WriteBinaryOperator("|", "Or", "or");
                    break;
                
                case "not":
                    output += WriteUnaryOperator("!", "Not", "not ");
                    break;
                
                case "eq":
                    output += WriteComparison("Equals", EqualsReturnLabel, EqualsSubLabel);
                    break;
                
                case "lt":
                    output += WriteComparison("Less Than", LessThanReturnLabel, LessThanSubLabel);
                    break;
                
                case "gt":
                    output += WriteComparison("Greater Than", GreaterThanReturnLabel, GreaterThanSubLabel);
                    break;
                
                default:
                    output += trimmedLine + Environment.NewLine;
                    break;
            }
        }

        output += WriteFooter();
        
        return output;
    }
    
    private static string TrimLine(string line)
    {
        if (line.Contains("//"))
        {
            var commentStart = line.IndexOf("//", StringComparison.OrdinalIgnoreCase);
            var trimmedLine = commentStart == 0
                ? ""
                : line.Substring(0, commentStart).Trim();
            return trimmedLine;
        }
        else
        {
            return line.Trim();
        }
    }

    private static string WriteHeader() =>
        OpenSectionComment("Reusable Sub Routines", 0) +
        UnconditionalJump(SkipSubsLabel, 1) +
        
        OpenSectionComment("Equals", 1) +
        WriteLabel(EqualsSubLabel) +
        BinaryOperatorToD("-", "-", 2) +
        PadLine("")  + Comment("If D = 0 Then Goto IsTrue Else Goto IsFalse", 2) +
        ConditionalJump("JEQ", IsTrueLabel, 2) +
        UnconditionalJump(IsFalseLabel, 2) +
        CloseSectionComment(1) +
        
        OpenSectionComment("Is Less Than", 1) +
        WriteLabel(LessThanSubLabel) +
        BinaryOperatorToD("-", "-", 2) +
        PadLine("")  + Comment("If D < 0 Then Goto IsTrue Else Goto IsFalse", 2) +
        ConditionalJump("JLT", IsTrueLabel, 2) +
        UnconditionalJump(IsFalseLabel, 2) +
        CloseSectionComment(1) +
        
        OpenSectionComment("Is Greater Than", 1) +
        WriteLabel(GreaterThanSubLabel) +
        BinaryOperatorToD("-", "-", 2) +
        PadLine("")  + Comment("If D > 0 Then Goto IsTrue Else Goto IsFalse", 2) +
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
        
        WriteLabel(SkipSubsLabel) +
        CloseSectionComment(0) +
        
        SetMemoryToValue(StackPointerAddress, BaseStackAddress.ToString(), 0);
    
    private static string WriteFooter() =>
        WriteLabel("END") +
        UnconditionalJump("END", 0);
    
    private static string WritePush(string segment, string index, string line)
    {
        switch (segment)
        {
            case "constant":
                return
                    OpenSectionComment($"Push Constant '{index}'", 0) +
                    ValueToD(index, 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            case "argument":
                return
                    OpenSectionComment($"Push M[M[Argument] + {index}]", 0) +
                    IndirectMemoryToD("ARG", index, "Argument", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            case "this":
                return
                    OpenSectionComment($"Push M[M[This] + {index}]", 0) +
                    IndirectMemoryToD("THIS", index, "This", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            case "that":
                return
                    OpenSectionComment($"Push M[M[That] + {index}]", 0) +
                    IndirectMemoryToD("THAT", index, "That", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            default:
                return TrimLine(line) + Environment.NewLine;
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

    private static string ValueToD(string value, int indentation) =>
        AInstruction(value) +
        PadLine("D=A") + Comment($"{value} => D", indentation);
    
    private static string NegativeValueToD(string value, int indentation) =>
        AInstruction(value) +
        PadLine("D=-A") + Comment($"-{value} => D", indentation);
    
    private static string DOperatorMemoryToD(string memoryAddress, string operatorSymbol, string commentOperator, int indentation) =>
        AInstruction(memoryAddress) +
        PadLine($"D=D{operatorSymbol}M") + Comment($"D {commentOperator} M[{memoryAddress}] => D", indentation);

    private static string IndirectMemoryToD(string memoryAddress, string index, string commentMemoryAddress, int indentation) =>
        ValueToD(index, indentation) +
        AInstruction(memoryAddress) +
        PadLine("A=M") + Comment($"M[{commentMemoryAddress}] => A", indentation) +
        PadLine("A=D+A") + Comment($"M[{commentMemoryAddress}] + {index} => A", indentation) +
        PadLine("D=M") + Comment($"M[M[{commentMemoryAddress}] + {index}] => D", indentation);
    
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
    
    private static string ConditionalJump(string jumpType, string address, int indentation) =>
        AInstruction(address) +
        PadLine($"D;{jumpType}")  + Comment($"goto {address}", indentation);
    
    private static string UnconditionalJumpToAddressInMemory(string memoryAddress, int indentation) =>
        AInstruction(memoryAddress) +
        PadLine("A=M") + Environment.NewLine +
        PadLine("0;JMP")  + Comment($"goto {memoryAddress}", indentation);
    
    private static string PadLine(string value)
        => value.PadRight(25, ' ');
    
    private static string OpenSectionComment(string comment, int indentation)
        => PadLine("") + " // " + "".PadRight(indentation * 3, ' ') + "[" + comment +  "] {" + Environment.NewLine;

    private static string Comment(string comment, int indentation)
        => " // "+ "".PadRight(indentation * 3, ' ') + comment + Environment.NewLine;
    
    private static string CloseSectionComment(int indentation)
        => PadLine("") + " // "+ "".PadRight(indentation * 3, ' ') + "}" + Environment.NewLine;
    
}