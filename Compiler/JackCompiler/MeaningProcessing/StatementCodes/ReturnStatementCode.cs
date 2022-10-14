namespace JackCompiler.MeaningProcessing.StatementCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ExpressionCodes;
    using PreMeaningProcessing.Syntactical;

    internal class ReturnStatementCode : StatementCode
    {
        private readonly ExpressionCode expression;

        internal ReturnStatementCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            IBranchPhrase returnStatementPhrase)
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

            if (returnStatementPhrase == null)
            {
                throw new ArgumentNullException("returnStatementPhrase");
            }
            else if (returnStatementPhrase.CategoryName != "returnStatement")
            {
                throw new ArgumentException("Phrase is not 'returnStatement'.");
            }

            var expressionPhrases = returnStatementPhrase.BranchChildren("expression");
            if (expressionPhrases == null)
            {
                throw new InvalidOperationException("Expression Phrase is null.");
            }

            var expressionPhrase = expressionPhrases.SingleOrDefault();
            if (expressionPhrase == null)
            {
                this.expression = null;
            }
            else
            {
                this.expression =
                    new ExpressionCode(
                        compilerErrors,
                        className,
                        subRoutineName,
                        variables,
                        expressionPhrase);
            }
        }

        internal override string GetDescription()
        {
            var expressionDescription =
                this.expression == null
                ? string.Empty
                : "\r\n" + CodeDescriptionHelper.AddTab(this.expression.GetDescription());

            return 
                "return"+
                expressionDescription;
        }

        internal override string Compile()
        {
            if (this.expression == null)
            {
                return
                    VmWriterHelper.WritePush(VmSegmentType.ConstantType, 0) +
                    VmWriterHelper.WriteReturn();
            }
            else
            {
                return
                    this.expression.Compile() +
                    VmWriterHelper.WriteReturn();
            }
        }
    }
}
