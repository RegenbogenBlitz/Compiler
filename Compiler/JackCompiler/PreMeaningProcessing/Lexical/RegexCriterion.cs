namespace JackCompiler.PreMeaningProcessing.Lexical
{
    using System;
    using System.Text.RegularExpressions;

    internal class RegexCriterion
    {
        private readonly Regex outerRegex;
        private readonly Regex innerRegex;

        internal RegexCriterion(string pattern) : this(pattern, pattern)
        {
        }

        internal RegexCriterion(string outerPattern, string innerPattern)
        {
            if (string.IsNullOrEmpty(outerPattern))
            {
                throw new ArgumentNullException("outerPattern");
            }

            if (string.IsNullOrEmpty(innerPattern))
            {
                throw new ArgumentNullException("innerPattern");
            }

            this.outerRegex = new Regex(outerPattern);
            this.innerRegex = new Regex(innerPattern);
        }

        internal bool IsMatch(string input, int startPosition, out string matchedText, out int removeLength)
        {
            matchedText = null;
            removeLength = 0;

            var outerMatch = this.outerRegex.Match(input, startPosition);
            if (outerMatch.Success && outerMatch.Index == startPosition)
            {
                var innerMatch = this.innerRegex.Match(outerMatch.Value);
                if (innerMatch.Success)
                {
                    matchedText = innerMatch.Value;
                    removeLength = outerMatch.Length;
                    return true;
                }
                else
                {
                    throw new InvalidOperationException(
                        "Inner Regex should always succeed if the Outer Regex succeeds.");
                }
            }

            return false;
        }
    }
}
