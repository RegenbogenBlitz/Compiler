namespace VmHackAsmTranslator.Parsing;

public class PushCommand : ICommand
{
    public PushCommand(string segment, uint index, string lineContent)
    {
        Segment = segment;
        Index = index;
        LineContent = lineContent;
    }

    public readonly string Segment;
    public readonly uint Index;
    public readonly string LineContent;
}