namespace VmHackAsmTranslator.Parsing;

public class PopCommand : ICommand
{
    public PopCommand(SegmentType segment, uint index, string lineContent)
    {
        Segment = segment;
        Index = index;
        LineContent = lineContent;
    }

    public readonly SegmentType Segment;
    public readonly uint Index;
    public readonly string LineContent;
}