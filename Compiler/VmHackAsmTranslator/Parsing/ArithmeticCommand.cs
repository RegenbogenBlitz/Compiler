namespace VmHackAsmTranslator.Parsing;

public class ArithmeticCommand : ICommand
{
    public ArithmeticCommand(ArithmeticCommandType arithmeticCommandType)
    {
        ArithmeticCommandType = arithmeticCommandType;
    }

    public readonly ArithmeticCommandType ArithmeticCommandType;
}