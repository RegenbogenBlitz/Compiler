namespace VmHackAsmTranslator.Parsing;

public class LabelCommand : ICommand
{
    public LabelCommand(string lineContent)
    {
        LineContent = lineContent;
    }

    public readonly string LineContent;
}