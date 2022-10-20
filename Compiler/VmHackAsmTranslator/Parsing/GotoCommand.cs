namespace VmHackAsmTranslator.Parsing;

public class GotoCommand : ICommand
{
    public GotoCommand(string symbol)
    {
        Symbol = symbol;
    }

    public readonly string Symbol;
}