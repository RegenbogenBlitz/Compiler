namespace VmHackAsmTranslator.Parsing;

public class PopCommand : ICommand
{
    public PopCommand(string lineContent)
    {
        LineContent = lineContent;
    }

    public readonly string LineContent;
}