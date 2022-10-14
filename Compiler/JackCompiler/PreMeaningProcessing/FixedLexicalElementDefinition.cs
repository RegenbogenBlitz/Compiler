namespace JackCompiler.PreMeaningProcessing
{
    using System;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Lexical;
    using Syntactical;

    internal class FixedLexicalElement : ILexicalElementDefinition, IPhraseGrabber
    {
        private readonly string categoryName;
        private readonly string fixedValue;
        private readonly RegexCriterion regexCriterion;

        public FixedLexicalElement(string categoryName, string fixedValue)
        {
            this.categoryName = categoryName;
            this.fixedValue = fixedValue;
            this.regexCriterion = new RegexCriterion(Regex.Escape(this.fixedValue));
        }

        public bool IsOutputted
        {
            get { return true; }
        }

        public string CategoryName
        {
            get { return this.categoryName; }
        }

        public RegexCriterion RegexCriterion
        {
            get { return this.regexCriterion; }
        }

        public SyntaxAnalysisResult GrabPhrases(BookmarkedArray<LexicalElement> remainingLexcialElements)
        {
            if (remainingLexcialElements == null)
            {
                throw new ArgumentNullException("remainingLexcialElements");
            }
            else
            {
                if (remainingLexcialElements.IsAtEnd)
                {
                    return new SyntaxAnalysisResult(this.GetElement("End of Lexical Elements"));
                }
            }

            var firstElement = remainingLexcialElements.GetValueIncreaseBookmark();
            if (firstElement.CategoryName != this.CategoryName)
            {
                return new SyntaxAnalysisResult(this.GetElement(
                    "Category Name Mismatch, " +
                    "Actual Category Name: '" + firstElement.CategoryName + "' " +
                    "Expected Fixed Value: '" + this.fixedValue + "'"));
            }
            else if (firstElement.Value != this.fixedValue)
            {
                return new SyntaxAnalysisResult(this.GetElement(
                    "Fixed Value Mismatch, " +
                    "Actual Value: '" + firstElement.Value + "' " +
                    "Expected Fixed Value: '" + this.fixedValue + "'"));
            }
            else
            {
                var grabbedPhrases = new[] {firstElement};

                return new SyntaxAnalysisResult(
                    this.GetElement(string.Empty),
                    grabbedPhrases);
            }
        }

        private XElement GetElement(string content)
        {
            return new XElement(
                this.CategoryName,
                new XAttribute("FixedValue", this.fixedValue),
                content);
        }
    }
}