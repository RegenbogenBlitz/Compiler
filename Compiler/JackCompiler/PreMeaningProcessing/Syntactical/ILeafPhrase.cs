namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    internal interface ILeafPhrase : IPhrase
    {
        int LineNumber { get; }
        string Value { get; }
    }
}
