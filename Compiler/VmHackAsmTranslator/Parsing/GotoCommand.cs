namespace VmHackAsmTranslator.Parsing;

public class GotoCommand : ICommand
{
    public GotoCommand(string lineContent)
    {
        LineContent = lineContent;
    }

    public readonly string LineContent;
}