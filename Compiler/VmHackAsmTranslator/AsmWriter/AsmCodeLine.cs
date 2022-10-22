namespace VmHackAsmTranslator.AsmWriter;

public class AsmCodeLine : AsmCode, IAsmOutput
{
    private readonly string _code;
    private readonly string? _comment;

    public AsmCodeLine(string code, string? comment = null)
    {
        _code = code;
        _comment = comment;
    }

    public IEnumerable<string> WriteWithComments(int indentation)
        => new[]
        {
            PadLine(_code) +
            (string.IsNullOrWhiteSpace(_comment)
                ? string.Empty
                : Comment(_comment, indentation))
        };

    public IEnumerable<string> WriteWithoutComments() => new[] { _code };
}