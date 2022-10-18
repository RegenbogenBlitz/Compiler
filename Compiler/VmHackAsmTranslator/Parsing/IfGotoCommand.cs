namespace VmHackAsmTranslator.Parsing;

public class IfGotoCommand : ICommand
{
    public IfGotoCommand(string lineContent)
    {
        LineContent = lineContent;
    }

    public readonly string LineContent;
}