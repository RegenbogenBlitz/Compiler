namespace VmHackAsmTranslator.Parsing;

public class ParserException : Exception
{
    public ParserException(int lineNumber, string line, string message) 
        : base($"Line Number {lineNumber}: '{line}' : {message}"){}
}