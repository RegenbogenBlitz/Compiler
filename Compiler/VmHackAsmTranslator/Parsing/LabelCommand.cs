namespace VmHackAsmTranslator.Parsing;

public class LabelCommand : ICommand
{

    public LabelCommand(string functionName, string symbol)
    {
        FunctionName = functionName;
        Symbol = symbol;
    }
    
    public string FunctionName { get; }

    public readonly string Symbol;
}