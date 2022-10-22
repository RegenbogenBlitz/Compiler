namespace VmHackAsmTranslator.AsmWriter;

public abstract class AsmCode
{
    protected static string CommentLine(string comment, int indentation)
        => PadLine("") + " // "+ "".PadRight(indentation * 3, ' ') + comment;
    
    protected static string Comment(string comment, int indentation)
        => " // "+ "".PadRight(indentation * 3, ' ') + comment;
    
    protected static string PadLine(string value)
        => value.PadRight(25, ' ');
}