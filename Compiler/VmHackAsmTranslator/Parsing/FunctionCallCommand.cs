namespace VmHackAsmTranslator.Parsing;

public class FunctionCallCommand : ICommand
{
    public FunctionCallCommand(string functionName, uint numArguments)
    {
        FunctionName = functionName;
        NumArguments = numArguments;
    }
    
    public string FunctionName { get; }
    
    public uint NumArguments { get; }
}