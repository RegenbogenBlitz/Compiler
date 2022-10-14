namespace JackCompiler.MeaningProcessing.StatementCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ExpressionCodes;
    using PreMeaningProcessing.Syntactical;
    using TermCodes;

    internal class LetStatementCode : StatementCode
    {
        private readonly VariableUseTermCode leftHandSideCode;
        private readonly ExpressionCode rightHandSideExpression;
        
        internal LetStatementCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            IBranchPhrase letStatementPhrase)
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

            if (letStatementPhrase == null)
            {
                throw new ArgumentNullException("letStatementPhrase");
            }
            else if (letStatementPhrase.CategoryName != "letStatement")
            {
                throw new ArgumentException("Phrase is not 'letStatement'.");
            }

            var identifierPhrase = letStatementPhrase.LeafChild("identifier");
            if (identifierPhrase == null)
            {
                throw new InvalidOperationException("Identifier Phrase is null.");
            }

            var expressionPhrases = letStatementPhrase.BranchChildren("expression");
            IBranchPhrase arrayIndexExpressionPhrase;
            if (expressionPhrases.Count() == 1)
            {
                arrayIndexExpressionPhrase = null;

                var rightHandSideExpressionPhrase = expressionPhrases.Single();
                this.rightHandSideExpression = 
                    new ExpressionCode(
                        compilerErrors,
                        className,
                        subRoutineName,
                        variables,
                        rightHandSideExpressionPhrase);
            }
            else if (expressionPhrases.Count() == 2)
            {
                arrayIndexExpressionPhrase = expressionPhrases.First();
                
                var rightHandSideExpressionPhrase = expressionPhrases.Last();
                this.rightHandSideExpression =
                    new ExpressionCode(
                        compilerErrors,
                        className,
                        subRoutineName,
                        variables,
                        rightHandSideExpressionPhrase);
            }
            else
            {
                throw new InvalidOperationException("Incorrect number of expression sections.");
            }

            this.leftHandSideCode = new VariableUseTermCode(
                compilerErrors,
                className,
                subRoutineName,
                variables,
                identifierPhrase,
                arrayIndexExpressionPhrase);
        }

        internal override string GetDescription()
        {
            var righHandSideDescription = 
                "=:\r\n" +
                CodeDescriptionHelper.AddTab(this.rightHandSideExpression.GetDescription());

            return 
                "let\r\n" +
                CodeDescriptionHelper.AddTab(
                    this.leftHandSideCode.GetDescription() + "\r\n" +
                    righHandSideDescription);
        }

        internal override string Compile()
        {
            var rightHandSideCompiledCode = this.rightHandSideExpression.Compile();

            var leftHandSideCompiledCode = this.leftHandSideCode.PopOffFromStackCompile();

            return leftHandSideCompiledCode.Item1 + rightHandSideCompiledCode + leftHandSideCompiledCode.Item2;
        }
    }
}