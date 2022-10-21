namespace VmHackAsmTranslator.Parsing;

public class FunctionDeclarationCommand : ICommand
{

    public FunctionDeclarationCommand(string functionName, uint numLocals)
    {
        FunctionName = functionName;
        NumLocals = numLocals;
    }
    
    public string FunctionName { get; }
    
    public uint NumLocals { get; }
}