namespace JackCompiler.PreMeaningProcessing
{
    using Lexical;

    internal class IgnoredLexicalElement : ILexicalElementDefinition
    {
        private readonly string categoryName;
        private readonly RegexCriterion regexCriterion;

        public IgnoredLexicalElement(string categoryName, string regexCriterionString)
        {
            this.categoryName = categoryName;
            this.regexCriterion = new RegexCriterion(regexCriterionString);
        }

        public bool IsOutputted
        {
            get { return false; }
        }

        public string CategoryName
        {
            get { return this.categoryName; }
        }

        public RegexCriterion RegexCriterion
        {
            get { return this.regexCriterion; }
        }
    }
}
