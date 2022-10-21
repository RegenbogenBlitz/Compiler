namespace VmHackAsmTranslator.Parsing;

public class PushCommand : ICommand
{
    public PushCommand(SegmentType segment, uint index)
    {
        Segment = segment;
        Index = index;
    }

    public readonly SegmentType Segment;
    public readonly uint Index;
}