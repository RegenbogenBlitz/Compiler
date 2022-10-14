namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using Lexical;

    internal class SequencePhraseGrabber : IPhraseGrabber
    {
        private readonly IEnumerable<IPhraseGrabber> phraseGrabbers;

        internal SequencePhraseGrabber(
            IEnumerable<IPhraseGrabber> phraseGrabbers)
        {
            this.phraseGrabbers = phraseGrabbers;
        }

        public SyntaxAnalysisResult GrabPhrases(BookmarkedArray<LexicalElement> remainingLexcialElements)
        {
            var localGrabbedPhrases = new List<IPhrase>();

            var detail = new List<XElement>();

            int criterionNumber = 0;
            remainingLexcialElements.AddBookmark();
            foreach (var phraseGrabber in this.phraseGrabbers)
            {
                var result = phraseGrabber.GrabPhrases(remainingLexcialElements);

                detail.Add(new XElement("Criterion_" + criterionNumber,  result.Detail));

                if (!result.Success)
                {
                    remainingLexcialElements.RevertAndRemoveBookmark();
                    return new SyntaxAnalysisResult(new XElement("Sequence", detail));
                }
                
                localGrabbedPhrases.AddRange(result.GrabbedPhrases);

                criterionNumber++;
            }

            return new SyntaxAnalysisResult(
                new XElement("Sequence",  detail),
                localGrabbedPhrases);
        }
    }
}
