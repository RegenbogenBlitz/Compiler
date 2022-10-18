namespace VmHackAsmTranslator.Parsing;

public class FunctionDeclarationCommand : ICommand
{
    public FunctionDeclarationCommand(string lineContent)
    {
        LineContent = lineContent;
    }

    public readonly string LineContent;
}