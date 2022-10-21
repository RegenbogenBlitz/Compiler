namespace VmHackAsmTranslator.Parsing;

public class PushCommand : ICommand
{
    public PushCommand(string className, SegmentType segment, uint index)
    {
        ClassName = className;
        Segment = segment;
        Index = index;
    }

    public readonly string ClassName;
    public readonly SegmentType Segment;
    public readonly uint Index;
}