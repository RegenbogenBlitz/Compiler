namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using System.Collections.Generic;
    using System.Xml.Linq;
    using Lexical;

    internal class NoughtManyPhraseGrabber : IPhraseGrabber
    {
        private readonly IPhraseGrabber phraseGrabber;

        internal NoughtManyPhraseGrabber(IPhraseGrabber phraseGrabber)
        {
            this.phraseGrabber = phraseGrabber;
        }

        internal NoughtManyPhraseGrabber(IEnumerable<IPhraseGrabber> phraseGrabbers)
        {
            this.phraseGrabber = new SequencePhraseGrabber(phraseGrabbers);
        }

        public SyntaxAnalysisResult GrabPhrases(BookmarkedArray<LexicalElement> remainingLexcialElements)
        {
            var endResult = new XElement("NoughtMany");

            var results = new List<SyntaxAnalysisResult>();
            var grabbedPhrases = new List<IPhrase>();

            SyntaxAnalysisResult lastResult;
            bool success;
            do
            {
                lastResult = this.phraseGrabber.GrabPhrases(remainingLexcialElements);
                success = lastResult.Success;
                if (success)
                {
                    results.Add(lastResult);
                    grabbedPhrases.AddRange(lastResult.GrabbedPhrases);
                }
            } 
            while (success);

            foreach (var result in results)
            {
                endResult.Add(result.Detail);
            }

            endResult.Add(new XElement("NoughtManyNotFound", lastResult.Detail));

            return new SyntaxAnalysisResult(
                endResult,
                grabbedPhrases);
        }
    }
}
