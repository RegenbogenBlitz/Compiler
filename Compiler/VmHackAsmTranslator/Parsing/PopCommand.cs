namespace VmHackAsmTranslator.Parsing;

public class PopCommand : ICommand
{
    public PopCommand(SegmentType segment, uint index)
    {
        Segment = segment;
        Index = index;
    }

    public readonly SegmentType Segment;
    public readonly uint Index;
}