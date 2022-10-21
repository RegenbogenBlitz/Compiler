namespace VmHackAsmTranslator.Parsing;

public class IfGotoCommand : ICommand
{
    public IfGotoCommand(string functionName, string symbol)
    {
        FunctionName = functionName;
        Symbol = symbol;
    }
    
    public string FunctionName { get; }

    public readonly string Symbol;
}