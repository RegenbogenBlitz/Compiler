namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using Lexical;

    internal class NoughtOnePhraseGrabber : IPhraseGrabber
    {
        private readonly IPhraseGrabber phraseGrabber;

        internal NoughtOnePhraseGrabber(IPhraseGrabber phraseGrabber)
        {
            this.phraseGrabber = phraseGrabber;
        }

        internal NoughtOnePhraseGrabber(IEnumerable<IPhraseGrabber> phraseGrabbers)
        {
            this.phraseGrabber = new SequencePhraseGrabber(phraseGrabbers);
        }

        public SyntaxAnalysisResult GrabPhrases(BookmarkedArray<LexicalElement> remainingLexcialElements)
        {
            var result = this.phraseGrabber.GrabPhrases(remainingLexcialElements);

            if (result.Success)
            {
                return new SyntaxAnalysisResult(
                    new XElement("NoughtOne", result.Detail),
                    result.GrabbedPhrases);
            }
            else
            {
                return new SyntaxAnalysisResult(
                    new XElement("NoughtOneNotFound", result.Detail),
                    new IPhrase[] { });
            }
        }
    }
}
