namespace VmHackAsmTranslator;

public static class VmTranslator
{
    private const string StackPointerAddress = "SP";
    
    public static string Translate(string[] lines)
    {
        var output = string.Empty;
        
        const int baseStackAddress = 256;

        output += SetMemoryToValue(StackPointerAddress, baseStackAddress.ToString(), 0);

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
                
                default:
                    output += trimmedLine + Environment.NewLine;
                    break;
            }
        }
        
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
    
    private static string WritePush(string segment, string index, string line)
    {
        switch (segment)
        {
            case "constant":
                return
                    OpenSectionComment($"Push Constant '{index}'", 0) +
                    AInstruction(index) +
                    PadLine("D=A") + Comment($"{index} => D", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            default:
                return TrimLine(line);
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
        PopToD(1) +
        DToMemory("R13", 1) +
        PopToD(1) +
        DOperatorMemoryToD("R13", operatorSymbol, commentOperator, 1) +
        PushD(1) +
        CloseSectionComment(0);

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

    private static string DOperatorMemoryToD(string memoryAddress, string operatorSymbol, string commentOperator, int indentation) =>
        AInstruction(memoryAddress) +
        PadLine($"D=D{operatorSymbol}M") + Comment($"D {commentOperator} {memoryAddress} => D", indentation);

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
    
    private static string PadLine(string value)
        => value.PadRight(25, ' ');
    
    private static string OpenSectionComment(string comment, int indentation)
        => PadLine("") + " // " + "".PadRight(indentation * 3, ' ') + "[" + comment +  "] {" + Environment.NewLine;

    private static string Comment(string comment, int indentation)
        => " // "+ "".PadRight(indentation * 3, ' ') + comment + Environment.NewLine;
    
    private static string CloseSectionComment(int indentation)
        => PadLine("") + " // "+ "".PadRight(indentation * 3, ' ') + "}" + Environment.NewLine;
    
}