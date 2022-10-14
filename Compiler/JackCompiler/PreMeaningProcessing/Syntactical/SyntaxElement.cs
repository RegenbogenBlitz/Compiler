namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using System.Collections.Generic;

    internal class SyntaxElement : IBranchPhrase
    {
        private readonly string categoryName;
        private readonly IEnumerable<IPhrase> grabbedPhrases;

        public SyntaxElement(string categoryName, IEnumerable<IPhrase> grabbedPhrases)
        {
            this.categoryName = categoryName;
            this.grabbedPhrases = grabbedPhrases;
        }

        public string CategoryName
        {
            get { return this.categoryName; }
        }

        public IEnumerable<IPhrase> Children
        {
            get { return this.grabbedPhrases; }
        }
    }
}
