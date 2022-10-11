namespace VmHackAsmTranslator;

public static class VmTranslator
{
    private const string StackPointerAddress = "SP";
    
    public static string Translate(string[] lines)
    {
        var trimmedLines = lines.Select(TrimLine).Where(line => !string.IsNullOrWhiteSpace(line));

        var output = string.Empty;
        
        const int baseStackAddress = 256;

        output += SetMemoryToValue(StackPointerAddress, baseStackAddress.ToString());

        foreach (var line in trimmedLines)
        {
            output += line + Environment.NewLine;
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
    
    private static string SetMemoryToValue(string memoryAddress, string value)
    {
        return
            OpenSectionComment($"Set {memoryAddress} to '{value}'")  +
            AInstruction(value) +
            PadLine("D=A") + IndentedComment($"{value} => D") +
            AInstruction(memoryAddress) +
            PadLine("M=D") + IndentedComment($"D => {memoryAddress}") +
            CloseSectionComment();
    }

    private static string AInstruction(string value)
        => PadLine($"@{value}") + Environment.NewLine;
    
    private static string PadLine(string value)
        => value.PadRight(25, ' ');
    
    private static string OpenSectionComment(string comment)
        => PadLine("") + " // [" + comment +  "] {" + Environment.NewLine;
    private static string IndentedComment(string comment)
        => " //    " + comment + Environment.NewLine;
    private static string CloseSectionComment()
        => PadLine("") + " // }" + Environment.NewLine;
    
}