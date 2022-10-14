namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using Lexical;

    internal interface IPhraseGrabber
    {
        SyntaxAnalysisResult GrabPhrases(BookmarkedArray<LexicalElement> remainingLexcialElements);
    }
}