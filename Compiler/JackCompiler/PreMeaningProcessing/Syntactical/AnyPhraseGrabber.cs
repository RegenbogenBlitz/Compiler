namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using Lexical;

    internal class AnyPhraseGrabber : IPhraseGrabber
    {
        private readonly IEnumerable<IPhraseGrabber> phraseGrabbers;

        internal AnyPhraseGrabber(IEnumerable<IPhraseGrabber> phraseGrabbers)
        {
            this.phraseGrabbers = phraseGrabbers;
        }

        public SyntaxAnalysisResult GrabPhrases(BookmarkedArray<LexicalElement> remainingLexcialElements)
        {
            var failedReasons = new List<XElement>();

            foreach (var phraseGrabber in this.phraseGrabbers)
            {
                remainingLexcialElements.AddBookmark();
                var result = phraseGrabber.GrabPhrases(remainingLexcialElements);

                if (result.Success)
                {
                    return result;
                }
                else
                {
                    remainingLexcialElements.RevertAndRemoveBookmark();
                    failedReasons.Add(result.Detail);
                }
            }
            
            return new SyntaxAnalysisResult(
                new XElement(
                    "AllCriteriaFailed",
                    failedReasons));
        }
    }
}
