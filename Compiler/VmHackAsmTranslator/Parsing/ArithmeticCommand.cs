namespace VmHackAsmTranslator.Parsing;

public class ArithmeticCommand : ICommand
{
    public ArithmeticCommand(string lineContent)
    {
        LineContent = lineContent;
    }

    public readonly string LineContent;
}