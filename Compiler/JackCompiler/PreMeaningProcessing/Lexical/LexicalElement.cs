namespace JackCompiler.PreMeaningProcessing.Lexical
{
    using Syntactical;

    internal class LexicalElement : ILeafPhrase
    {
        private LexicalElement(bool isValid, string categoryName, string value, int lineNumber)
        {
            this.IsValid = isValid;
            this.CategoryName = categoryName;
            this.Value = value;
            this.LineNumber = lineNumber;
        }

        internal bool IsValid { get; private set; }
        public string CategoryName { get; private set; }
        public string Value { get; private set; }
        public int LineNumber { get; private set; }

        internal static LexicalElement CreateLexicalElement(string type, string value, int lineNumber)
        {
            return new LexicalElement(true, type, value, lineNumber);
        }

        internal static LexicalElement CreateInvalidLexicalElement(string value, int lineNumber)
        {
            return new LexicalElement(false, null, value, lineNumber);
        }
    }
}
