namespace VmHackAsmTranslator.Parsing;

public class GotoCommand : ICommand
{
    public GotoCommand(string functionName, string symbol)
    {
        FunctionName = functionName;
        Symbol = symbol;
    }
    
    public string FunctionName { get; }

    public readonly string Symbol;
}