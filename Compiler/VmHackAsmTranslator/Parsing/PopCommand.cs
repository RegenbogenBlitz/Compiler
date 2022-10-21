namespace VmHackAsmTranslator.Parsing;

public class PopCommand : ICommand
{
    public PopCommand(string className, SegmentType segment, uint index)
    {
        ClassName = className;
        Segment = segment;
        Index = index;
    }

    public readonly string ClassName;
    public readonly SegmentType Segment;
    public readonly uint Index;
}