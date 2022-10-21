namespace VmHackAsmTranslator.Parsing;

public class ParserException : Exception
{
    public ParserException(LineInfo lineInfo, string message) 
        : base($"File {lineInfo.FileName} Line Number {lineInfo.LineNumber}: '{lineInfo.OriginalLine}' : {message}"){}
}