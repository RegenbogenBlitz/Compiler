namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using Lexical;

    internal class SyntaxElementDefinition : IPhraseGrabber
    {
        private readonly string categoryName;
        private readonly IPhraseGrabber phraseGrabber;

        internal SyntaxElementDefinition(
            string categoryName,
            AnyPhraseGrabber phraseGrabber)
        {
            this.categoryName = categoryName;
            this.phraseGrabber = phraseGrabber;
        }

        internal SyntaxElementDefinition(
            string categoryName,
            NoughtOnePhraseGrabber phraseGrabber)
        {
            this.categoryName = categoryName;
            this.phraseGrabber = phraseGrabber;
        }

        internal SyntaxElementDefinition(
            string categoryName,
            NoughtManyPhraseGrabber phraseGrabber)
        {
            this.categoryName = categoryName;
            this.phraseGrabber = phraseGrabber;
        }

        internal SyntaxElementDefinition(
            string categoryName,
            IEnumerable<IPhraseGrabber> syntacticalElementDefinitions)
        {
            this.categoryName = categoryName;
            this.phraseGrabber = new SequencePhraseGrabber(syntacticalElementDefinitions);
        }

        public SyntaxAnalysisResult GrabPhrases(BookmarkedArray<LexicalElement> remainingLexcialElements)
        {
            var result = this.phraseGrabber.GrabPhrases(remainingLexcialElements);

            if (result.Success)
            {
                var grabbedPhrases = new IPhrase[]
                {
                    new SyntaxElement(this.categoryName, result.GrabbedPhrases)
                };

                return new SyntaxAnalysisResult(
                    new XElement(this.categoryName, result.Detail),
                    grabbedPhrases);
            }
            else
            {
                return new SyntaxAnalysisResult(new XElement(this.categoryName, result.Detail));
            }
        }
    }
}
