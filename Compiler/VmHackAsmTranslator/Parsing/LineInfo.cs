namespace VmHackAsmTranslator.Parsing;

public class LineInfo
{
    public LineInfo(string fileName, uint lineNumber, string originalLine)
    {
        FileName = fileName;
        LineNumber = lineNumber;
        OriginalLine = originalLine;
    }

    public string FileName { get; }
    public uint LineNumber { get; }
    public string OriginalLine { get; }
}