namespace VmHackAsmTranslator.AsmWriter;

public interface IAsmOutput
{
    IEnumerable<string> WriteWithComments(int indentation);
    IEnumerable<string> WriteWithoutComments();
}