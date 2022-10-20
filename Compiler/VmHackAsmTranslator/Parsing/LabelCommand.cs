namespace VmHackAsmTranslator.Parsing;

public class LabelCommand : ICommand
{
    public LabelCommand(string symbol)
    {
        Symbol = symbol;
    }

    public readonly string Symbol;
}