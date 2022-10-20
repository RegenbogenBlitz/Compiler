namespace VmHackAsmTranslator.Parsing;

public class IfGotoCommand : ICommand
{
    public IfGotoCommand(string symbol)
    {
        Symbol = symbol;
    }

    public readonly string Symbol;
}