namespace VmHackAsmTranslator.Parsing;

public class PushCommand : ICommand
{
    public PushCommand(SegmentType segment, uint index, string lineContent)
    {
        Segment = segment;
        Index = index;
        LineContent = lineContent;
    }

    public readonly SegmentType Segment;
    public readonly uint Index;
    public readonly string LineContent;
}