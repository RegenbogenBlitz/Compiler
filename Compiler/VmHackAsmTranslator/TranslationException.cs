namespace VmHackAsmTranslator;

public class TranslationException : Exception
{
    public TranslationException(int lineNumber, string line, string message) 
        : base($"Line Number {lineNumber}: '{line}' : {message}"){}
}