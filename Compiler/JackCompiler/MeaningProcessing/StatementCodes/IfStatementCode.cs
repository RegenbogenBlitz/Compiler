namespace JackCompiler.MeaningProcessing.StatementCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ExpressionCodes;
    using PreMeaningProcessing.Syntactical;

    internal class IfStatementCode : StatementCode
    {
        private readonly ExpressionCode expression;
        private readonly IEnumerable<StatementCode> thenCaseStatements;
        private readonly IEnumerable<StatementCode> elseCaseStatements;

        internal IfStatementCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            IBranchPhrase ifStatementPhrase)
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

            if (ifStatementPhrase == null)
            {
                throw new ArgumentNullException("ifStatementPhrase");
            }
            else if (ifStatementPhrase.CategoryName != "ifStatement")
            {
                throw new ArgumentException("Phrase is not 'ifStatement'.");
            }

            var expressionPhrase = ifStatementPhrase.BranchChild("expression");
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

            var statementPhrases = ifStatementPhrase.BranchChildren("statements");
            if (statementPhrases == null || !statementPhrases.Any())
            {
                throw new InvalidOperationException("Statement Phrases is null or empty.");
            }

            var ifCaseStatementPhrase = statementPhrases.First();
            this.thenCaseStatements =
                StatementsProcessor.Process(
                    compilerErrors,
                    className,
                    subRoutineName,
                    variables,
                    ifCaseStatementPhrase);

            if (statementPhrases.Count() == 2)
            {
                var elseCaseStatementPhrase = statementPhrases.Last(); 
                this.elseCaseStatements =
                    StatementsProcessor.Process(
                        compilerErrors,
                        className,
                        subRoutineName,
                        variables,
                        elseCaseStatementPhrase);
            }
            else
            {
                this.elseCaseStatements = null;
            }
        }

        internal override string GetDescription()
        {
            string elseSection=
                this.elseCaseStatements != null
                ? 
                    "\r\n" + 
                    "else {}:\r\n" +
                    CodeDescriptionHelper.AddTab(
                        StatementsProcessor.GetDescription(this.elseCaseStatements))
                : string.Empty;

            return 
                "if\r\n"+
                CodeDescriptionHelper.AddTab(
                    "():\r\n" +
                    CodeDescriptionHelper.AddTab(this.expression.GetDescription()) + "\r\n" +
                    "then {}:\r\n" +
                    CodeDescriptionHelper.AddTab(
                        StatementsProcessor.GetDescription(this.thenCaseStatements)) + 
                    elseSection);
        }

        internal override string Compile()
        {
            var compiledCode = new StringBuilder();

            string ifTrueLabel;
            var ifTrueStatement = VmWriterHelper.WriteLabel("IF_TRUE", out ifTrueLabel);

            string ifFalseLabel;
            var ifFalseStatement = VmWriterHelper.WriteLabel("IF_FALSE", out ifFalseLabel);

            string ifEndLabel;
            var ifEndLabelStatement = VmWriterHelper.WriteLabel("IF_END", out ifEndLabel);

            compiledCode.Append(this.expression.Compile());
            compiledCode.Append(VmWriterHelper.WriteIf(ifTrueLabel));
            compiledCode.Append(VmWriterHelper.WriteGoto(ifFalseLabel));
            compiledCode.Append(ifTrueStatement);
            foreach (var statement in this.thenCaseStatements)
            {
                compiledCode.Append(statement.Compile());
            }
            
            if (this.elseCaseStatements == null)
            {
                compiledCode.Append(ifFalseStatement);
            }
            else
            {
                compiledCode.Append(VmWriterHelper.WriteGoto(ifEndLabel));

                compiledCode.Append(ifFalseStatement);

                foreach (var statement in this.elseCaseStatements)
                {
                    compiledCode.Append(statement.Compile());
                }

                compiledCode.Append(ifEndLabelStatement);
            }
            
            return compiledCode.ToString();
        }
    }
}
