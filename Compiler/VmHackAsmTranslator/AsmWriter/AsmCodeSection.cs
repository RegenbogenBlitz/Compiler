namespace VmHackAsmTranslator.AsmWriter;

public class AsmCodeSection : AsmCode, IAsmOutput
{
    private readonly string _sectionComment;
    private readonly IEnumerable<IAsmOutput> _asmOutputs;

     public AsmCodeSection(string sectionComment, IEnumerable<IAsmOutput> asmOutputs)
    {
        _sectionComment = sectionComment;
        _asmOutputs = asmOutputs;
    }

    public IEnumerable<string> WriteWithComments(int indentation)
        => string.IsNullOrWhiteSpace(_sectionComment)
            ? _asmOutputs.SelectMany(ao => ao.WriteWithComments(indentation + 1))
            : new[]
                {
                    OpenSectionComment(_sectionComment, indentation)
                }.Concat(_asmOutputs.SelectMany(ao => ao.WriteWithComments(indentation + 1)))
                .Concat(new[] { CloseSectionComment(indentation) });

    public IEnumerable<string> WriteWithoutComments()
        => _asmOutputs.SelectMany(ao => ao.WriteWithoutComments());
    
    private static string OpenSectionComment(string comment, int indentation)
        => CommentLine("[" + comment +  "] {", indentation);

    private static string CloseSectionComment(int indentation)
        => CommentLine("}", indentation);
}