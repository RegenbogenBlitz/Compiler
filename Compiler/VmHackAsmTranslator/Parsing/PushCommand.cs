namespace VmHackAsmTranslator.Parsing;

public class PushCommand : ICommand
{
    public PushCommand(string lineContent)
    {
        LineContent = lineContent;
    }

    public readonly string LineContent;
}