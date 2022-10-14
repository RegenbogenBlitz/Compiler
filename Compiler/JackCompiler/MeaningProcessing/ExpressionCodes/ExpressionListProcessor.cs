namespace JackCompiler.MeaningProcessing.ExpressionCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PreMeaningProcessing.Syntactical;

    internal static class ExpressionListProcessor
    {
        internal static IEnumerable<ExpressionCode> Process(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables, 
            IBranchPhrase expressionListPhrase)
        {
            if (compilerErrors == null)
            {
                throw new ArgumentNullException("compilerErrors");
            }

            if (className == null)
            {
                throw new ArgumentNullException("className");
            }

            if (subRoutineName == null)
            {
                throw new ArgumentNullException("subRoutineName");
            }

            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }

            if (expressionListPhrase == null)
            {
                throw new ArgumentNullException("expressionListPhrase");
            }
            else if (expressionListPhrase.CategoryName != "expressionList")
            {
                throw new ArgumentException("Phrase is not 'expressionList'.");
            }

            var expressionPhrases = expressionListPhrase.BranchChildren("expression");
            if (expressionPhrases == null)
            {
                throw new InvalidOperationException("Expression Phrases is null.");
            }

            var expressions = new List<ExpressionCode>();
            foreach (var expressionPhrase in expressionPhrases)
            {
                var expression = new ExpressionCode(
                    compilerErrors,
                    className,
                    subRoutineName,
                    variables,
                    expressionPhrase);

                expressions.Add(expression);
            }

            return expressions;
        }

        public static string GetDescription(IEnumerable<ExpressionCode> parameters)
        {
            var parameterStrings = parameters.Select(
                p => "Parameter: \r\n" + CodeDescriptionHelper.AddTab(p.GetDescription()));

            return string.Join("\r\n", parameterStrings);
        }
    }
}
