namespace VmHackAsmTranslator.Parsing;

public class VmCode
{
    public VmCode(IEnumerable<ICommand> commands)
    {
        Commands = commands;
    }

    public readonly IEnumerable<ICommand> Commands;
}