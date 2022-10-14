namespace JackCompiler.PreMeaningProcessing
{
    using System;
    using System.Xml.Linq;
    using Lexical;
    using Syntactical;

    internal class FlexibleLexicalElement : ILexicalElementDefinition, IPhraseGrabber
    {
        private readonly string categoryName;
        private readonly RegexCriterion regexCriterion;

        public FlexibleLexicalElement(string categoryName, string regexCriterionString)
            : this(categoryName, regexCriterionString, regexCriterionString)
        {  
        }

        public FlexibleLexicalElement(string categoryName, string outerPattern, string innerPattern)
        {
            this.categoryName = categoryName;
            this.regexCriterion = new RegexCriterion(outerPattern, innerPattern);
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
            else if (remainingLexcialElements.IsAtEnd)
            {
                return new SyntaxAnalysisResult(this.GetFailElement("End of Lexical Elements"));
            }

            var firstElement = remainingLexcialElements.GetValueIncreaseBookmark();
            if (firstElement.CategoryName != this.CategoryName)
            {
                return new SyntaxAnalysisResult(this.GetFailElement(
                    "Category Name Mismatch, " +
                    "Category Name: '" + firstElement.CategoryName + "'"));
            }
            else
            {
                var grabbedPhrases = new[] { firstElement };

                return new SyntaxAnalysisResult(
                    this.GetSuccessElement(firstElement.Value),
                    grabbedPhrases);
            }
        }

        private XElement GetFailElement(string content)
        {
            return new XElement(
                this.CategoryName,
                content);
        }

        private XElement GetSuccessElement(string value)
        {
            return new XElement(
                this.CategoryName,
                new XAttribute("Value", value));
        }
    }
}
