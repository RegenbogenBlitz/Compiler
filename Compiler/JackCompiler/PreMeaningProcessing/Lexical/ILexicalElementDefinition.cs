namespace JackCompiler.PreMeaningProcessing.Lexical
{
    internal interface ILexicalElementDefinition
    {
        bool IsOutputted { get; }
        string CategoryName { get; }
        RegexCriterion RegexCriterion { get; }
    }
}