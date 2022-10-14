namespace JackCompiler.MeaningProcessing.StatementCodes
{
    internal abstract class StatementCode
    {
        internal abstract string GetDescription();

        internal abstract string Compile();
    }
}
