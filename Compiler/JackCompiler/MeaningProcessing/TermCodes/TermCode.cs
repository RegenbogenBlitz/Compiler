namespace JackCompiler.MeaningProcessing.TermCodes
{
    internal abstract class TermCode
    {
        internal abstract string GetDescription();

        internal abstract string PushOntoStackCompile();
    }
}
