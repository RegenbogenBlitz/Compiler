namespace VmHackAsmTranslator.Parsing;

public class FunctionCallCommand : ICommand
{
    public FunctionCallCommand(string lineContent)
    {
        LineContent = lineContent;
    }

    public readonly string LineContent;
}