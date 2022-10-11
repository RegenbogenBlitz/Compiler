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
                    output += WriteAdd();
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
                    AInstruction(index, 1) +
                    PadLine("D=A") + Comment($"{index} => D", 1) +
                    PushD(1) +
                    CloseSectionComment(0);
            
            default:
                return TrimLine(line);
        }
    }
    
    private static string WriteAdd() =>
        OpenSectionComment("Add", 0)  +
        PopToD(1) +
        DToMemory("R13", 1) +
        PopToD(1) +
        DPlusMemoryToD("R13", 1) +
        PushD(1) +
        CloseSectionComment(0);

    private static string SetMemoryToValue(string memoryAddress, string value, int indentation) =>
        OpenSectionComment($"Set {memoryAddress} to '{value}'", indentation) +
        AInstruction(value, indentation + 1) +
        PadLine("D=A") + Comment($"{value} => D", indentation + 1) +
        DToMemory(memoryAddress, indentation + 1) +
        CloseSectionComment(indentation);

    private static string PushD(int indentation) =>
        OpenSectionComment("Push D", indentation) +
        DToTopStack(indentation + 1) +
        LiftStack(indentation + 1) +
        CloseSectionComment(indentation);
    
    private static string PopToD(int indentation) =>
        OpenSectionComment("Pop to D", indentation) +
        DropStack(indentation +1 ) +
        TopStackToD(indentation +1 ) +
        CloseSectionComment(indentation);
    
    private static string DToMemory(string memoryAddress, int indentation) =>
        AInstruction(memoryAddress, indentation) +
        PadLine("M=D") + Comment($"D => {memoryAddress}", indentation);
    
    private static string DPlusMemoryToD(string memoryAddress, int indentation) =>
        AInstruction(memoryAddress, indentation) +
        PadLine("D=D+M") + Comment($"D + {memoryAddress} => D", indentation);
    
    private static string DToTopStack(int indentation) =>
        AInstruction("SP", indentation) +
        PadLine("A=M") + Comment("", indentation) +
        PadLine("M=D") + Comment("D => TopStack", indentation);
    
    private static string TopStackToD(int indentation) =>
        PadLine("A=M") + Comment("", indentation) +
        PadLine("D=M") + Comment("TopStack => D", indentation);
    
    private static string LiftStack(int indentation) =>
        AInstruction("SP", indentation) +
        PadLine("M=M+1") + Comment("Lift Stack", indentation);
    
    private static string DropStack(int indentation) =>
        AInstruction("SP", indentation) +
        PadLine("M=M-1") + Comment("Drop Stack", indentation);
    
    private static string AInstruction(string value, int indentation)
        => PadLine($"@{value}") + Comment("", indentation);
    
    private static string PadLine(string value)
        => value.PadRight(6, ' ');
    
    private static string OpenSectionComment(string comment, int indentation)
        => PadLine("") + "// " + "".PadRight(indentation * 3, ' ') + "[" + comment +  "] {" + Environment.NewLine;

    private static string Comment(string comment, int indentation)
        => "// "+ "".PadRight(indentation * 3, ' ') + comment + Environment.NewLine;
    
    private static string CloseSectionComment(int indentation)
        => PadLine("") + "// "+ "".PadRight(indentation * 3, ' ') + "}" + Environment.NewLine;
    
}