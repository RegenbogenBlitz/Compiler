namespace JackCompiler.MeaningProcessing.StatementCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ExpressionCodes;
    using PreMeaningProcessing.Syntactical;

    internal class WhileStatementCode : StatementCode
    {
        private readonly ExpressionCode expression;
        private readonly IEnumerable<StatementCode> statements;

        internal WhileStatementCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            IBranchPhrase whileStatementPhrase)
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

            if (whileStatementPhrase == null)
            {
                throw new ArgumentNullException("whileStatementPhrase");
            }
            else if (whileStatementPhrase.CategoryName != "whileStatement")
            {
                throw new ArgumentException("Phrase is not 'whileStatement'.");
            }

            var expressionPhrase = whileStatementPhrase.BranchChild("expression");
            if (expressionPhrase == null)
            {
                throw new InvalidOperationException("Expression Phrase is null.");
            }

            this.expression =
                new ExpressionCode(
                    compilerErrors,
                    className,
                    subRoutineName,
                    variables,
                    expressionPhrase);

            var statementPhrases = whileStatementPhrase.BranchChildren("statements");
            if (statementPhrases == null || !statementPhrases.Any())
            {
                throw new InvalidOperationException("Statements Phrase is null or empty.");
            }

            var statementPhrase = statementPhrases.Single();
            this.statements =
                StatementsProcessor.Process(
                    compilerErrors,
                    className,
                    subRoutineName,
                    variables,
                    statementPhrase);
        }

        internal override string GetDescription()
        {
            return 
                "while\r\n"+
                CodeDescriptionHelper.AddTab(
                    "():\r\n" +
                    CodeDescriptionHelper.AddTab(this.expression.GetDescription()) + "\r\n" +
                    "{}:\r\n" +
                    CodeDescriptionHelper.AddTab(
                        StatementsProcessor.GetDescription(this.statements)));
        }

        internal override string Compile()
        {
            var compiledCode = new StringBuilder();

            string whileExpressionLabel;
            var whileExpressionStatement = VmWriterHelper.WriteLabel("WHILE_EXP", out whileExpressionLabel);

            string whileEndLabel;
            var whileEndLabelStatement = VmWriterHelper.WriteLabel("WHILE_END", out whileEndLabel);

            compiledCode.Append(whileExpressionStatement);
            compiledCode.Append(this.expression.Compile());
            compiledCode.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.NotType));
            compiledCode.Append(VmWriterHelper.WriteIf(whileEndLabel));
            foreach (var statement in this.statements)
            {
                compiledCode.Append(statement.Compile());
            }
            compiledCode.Append(VmWriterHelper.WriteGoto(whileExpressionLabel));
            compiledCode.Append(whileEndLabelStatement);

            return compiledCode.ToString();
        }
    }
}
