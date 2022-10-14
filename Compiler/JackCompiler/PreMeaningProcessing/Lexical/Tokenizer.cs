namespace JackCompiler.PreMeaningProcessing.Lexical
{
    using System.Collections.Generic;
    
    internal class Tokenizer
    {
        private readonly IEnumerable<ILexicalElementDefinition> lexicalElementDefinitions;

        internal Tokenizer(IEnumerable<ILexicalElementDefinition> lexicalElementDefinitions)
        {
            this.lexicalElementDefinitions = lexicalElementDefinitions;
        }

        internal IEnumerable<LexicalElement> Tokenize(string[] fileContent)
        {
            var lexicalElements = new List<LexicalElement>();

            string input = string.Join("\n", fileContent);
            int fullInputLength = input.Length;
            int startPosition = 0;

            string invalidText = string.Empty;
            int? invalidTextLineNumber = null;

            while (startPosition < fullInputLength)
            {
                string lexicalElementType;
                string matchedText;
                int removeLength;
                bool isOutputted;

                bool matchFound = this.HasSomeMatch(
                    input,
                    startPosition,
                    out lexicalElementType,
                    out matchedText,
                    out removeLength,
                    out isOutputted);

                if (matchFound)
                {
                    if (invalidTextLineNumber.HasValue)
                    {
                        lexicalElements.Add(LexicalElement.CreateInvalidLexicalElement(
                            invalidText,
                            invalidTextLineNumber.Value));

                        invalidTextLineNumber = null;
                        invalidText = string.Empty;
                    }

                    if (isOutputted)
                    {
                        lexicalElements.Add(LexicalElement.CreateLexicalElement(
                            lexicalElementType,
                            matchedText,
                            CalculateLineNumber(
                                startPosition,
                                fileContent)));
                    }

                    startPosition += removeLength;
                }
                else
                {
                    if (!invalidTextLineNumber.HasValue)
                    {
                        invalidTextLineNumber = CalculateLineNumber(
                            startPosition,
                            fileContent);
                    }

                    invalidText += input[startPosition];
                    startPosition++;
                }
            }

            if (invalidTextLineNumber.HasValue)
            {
                lexicalElements.Add(LexicalElement.CreateInvalidLexicalElement(
                    invalidText,
                    invalidTextLineNumber.Value));
            }

            return lexicalElements;
        }

        private static int CalculateLineNumber(
            int processedTextLength,
            IEnumerable<string> fileContent)
        {
            int currentLength = 0;
            int count = 1;

            foreach (string line in fileContent)
            {
                currentLength += line.Length + 1; // +1 for \n character

                if (processedTextLength < currentLength)
                {
                    return count;
                }

                count += 1;
            }

            return count;
        }

        private bool HasSomeMatch(
            string input,
            int startPosition,
            out string lexicalElementType,
            out string matchedText,
            out int removeLength,
            out bool isOutputted)
        {
            lexicalElementType = null;
            matchedText = null;
            removeLength = 0;
            isOutputted = false;

            foreach (var lexicalElementDefinition in this.lexicalElementDefinitions)
            {
                string currentMatchedText;
                int currentRemoveLength;

                var regexCriterion = lexicalElementDefinition.RegexCriterion;
                bool isMatch = regexCriterion.IsMatch(
                    input,
                    startPosition,
                    out currentMatchedText,
                    out currentRemoveLength);

                if (isMatch)
                {
                    if (currentRemoveLength > removeLength)
                    {
                        removeLength = currentRemoveLength;
                        lexicalElementType = lexicalElementDefinition.CategoryName;
                        matchedText = currentMatchedText;
                        isOutputted = lexicalElementDefinition.IsOutputted;
                    }
                }
            }

            return lexicalElementType != null;
        }
    }
}
