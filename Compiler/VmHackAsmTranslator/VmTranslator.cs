namespace VmHackAsmTranslator;

public static class VmTranslator
{
    private const string StackPointerAddress = "SP";
    
    public static string Translate(string[] lines)
    {
        var output = string.Empty;
        
        const int baseStackAddress = 256;

        output += SetMemoryToValue(StackPointerAddress, baseStackAddress.ToString());

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
                    OpenSectionComment($"Push Constant '{index}'")  +
                    AInstruction(index) +
                    PadLine("D=A") + IndentedComment($"{index} => D") +
                    DToTopStack() +
                    LiftStack() +
                    CloseSectionComment();
            default:
                return TrimLine(line);
        }
    }
    
    private static string SetMemoryToValue(string memoryAddress, string value) =>
        OpenSectionComment($"Set {memoryAddress} to '{value}'")  +
        AInstruction(value) +
        PadLine("D=A") + IndentedComment($"{value} => D") +
        AInstruction(memoryAddress) +
        PadLine("M=D") + IndentedComment($"D => {memoryAddress}") +
        CloseSectionComment();

    private static string DToTopStack() =>
        AInstruction("SP") +
        PadLine("A=M") + Environment.NewLine +
        PadLine("M=D") + IndentedComment("D => TopStack");
    
    private static string LiftStack() =>
        AInstruction("SP") +
        PadLine("M=M+1") + IndentedComment("Lift Stack");
    
    private static string AInstruction(string value)
        => PadLine($"@{value}") + Environment.NewLine;
    
    private static string PadLine(string value)
        => value.PadRight(5, ' ');
    
    private static string OpenSectionComment(string comment)
        => PadLine("") + " // [" + comment +  "] {" + Environment.NewLine;
    private static string IndentedComment(string comment)
        => " //    " + comment + Environment.NewLine;
    private static string CloseSectionComment()
        => PadLine("") + " // }" + Environment.NewLine;
    
}