namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using System.Collections.Generic;
    using System.Xml.Linq;

    internal class SyntaxAnalysisResult
    {
        private readonly bool success;
        private readonly XElement detail;
        private readonly IEnumerable<IPhrase> grabbedPhrases;

        public SyntaxAnalysisResult(
            XElement failureReason)
        {
            this.success = false;
            failureReason.Name = "Failure_" + failureReason.Name;
            this.detail = failureReason;
            this.grabbedPhrases = null;
        }

        public SyntaxAnalysisResult(
            XElement successDescription,
            IEnumerable<IPhrase> grabbedPhrases)
        {
            this.success = true;
            successDescription.Name = successDescription.Name + "_Success";
            this.detail = successDescription;
            this.grabbedPhrases = grabbedPhrases;
        }

        public bool Success
        {
            get { return this.success; }
        }

        public XElement Detail
        {
            get { return this.detail; }
        }

        public IEnumerable<IPhrase> GrabbedPhrases
        {
            get { return this.grabbedPhrases; }
        }
    }
}
