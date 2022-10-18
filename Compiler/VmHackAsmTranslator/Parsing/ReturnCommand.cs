namespace VmHackAsmTranslator.Parsing;

public class ReturnCommand : ICommand
{
    public ReturnCommand(string lineContent)
    {
        LineContent = lineContent;
    }

    public readonly string LineContent;
}