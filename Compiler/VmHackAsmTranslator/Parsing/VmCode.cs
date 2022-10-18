namespace VmHackAsmTranslator.Parsing;

public class VmCode
{
    public VmCode(string[] fileContent)
    {
        FileContent = fileContent;
    }

    public readonly string[] FileContent;
}