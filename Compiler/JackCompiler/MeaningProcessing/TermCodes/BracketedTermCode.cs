namespace JackCompiler.MeaningProcessing.TermCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ExpressionCodes;
    using PreMeaningProcessing.Syntactical;

    internal class BracketedTermCode : TermCode
    {
        private readonly ExpressionCode expressionCode;

        public BracketedTermCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName, 
            IEnumerable<VariableCode> variables, 
            IBranchPhrase termPhrase)
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

            if (termPhrase == null)
            {
                throw new ArgumentNullException("termPhrase");
            }
            else if (termPhrase.CategoryName != "term")
            {
                throw new ArgumentException("Phrase is not 'term'.");
            }

            var children = termPhrase.Children;
            var openBracketPhrase = children.ElementAt(0) as ILeafPhrase;
            var expressionPhrase = children.ElementAt(1) as IBranchPhrase;
            var closeBracketPhrase = children.ElementAt(2) as ILeafPhrase;

            if (
                openBracketPhrase == null || 
                openBracketPhrase.CategoryName != "symbol" || 
                openBracketPhrase.Value != "(")
            {
                throw new ArgumentException("First child must be '('");
            }
            else if (
                expressionPhrase == null || 
                expressionPhrase.CategoryName != "expression")
            {
                throw new ArgumentException("Second child must be an expression");
            }
            else if (
                closeBracketPhrase == null || 
                closeBracketPhrase.CategoryName != "symbol" || 
                closeBracketPhrase.Value != ")")
            {
                throw new ArgumentException("Third child must be ')'");
            }

            this.expressionCode = new ExpressionCode(
                compilerErrors,
                className,
                subRoutineName,
                variables,
                expressionPhrase);
        }

        internal override string GetDescription()
        {
            return
                "(\r\n" +
                CodeDescriptionHelper.AddTab(this.expressionCode.GetDescription()) + "\r\n" +
                ")";
        }

        internal override string PushOntoStackCompile()
        {
            return this.expressionCode.Compile();
        }
    }
}
