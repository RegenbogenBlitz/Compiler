namespace JackCompiler.MeaningProcessing.StatementCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PreMeaningProcessing.Syntactical;

    internal static class StatementsProcessor
    {
        internal static IEnumerable<StatementCode> Process(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            IBranchPhrase statementsPhrase)
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

            if (statementsPhrase == null)
            {
                throw new ArgumentNullException("statementsPhrase");
            }
            else if (statementsPhrase.CategoryName != "statements")
            {
                throw new ArgumentException("Phrase is not 'statements'.");
            }

            var statementPhrases = statementsPhrase.BranchChildren();
            var statementCodes = new List<StatementCode>();

            foreach (var statementPhrase in statementPhrases)
            {
                switch (statementPhrase.CategoryName)
                {
                    case "letStatement":
                        var letStatement = new LetStatementCode(
                            compilerErrors,
                            className,
                            subRoutineName,
                            variables,
                            statementPhrase);

                        statementCodes.Add(letStatement);
                        break;

                    case "ifStatement":
                        var ifStatement = new IfStatementCode(
                            compilerErrors,
                            className,
                            subRoutineName,
                            variables,
                            statementPhrase);

                        statementCodes.Add(ifStatement);
                        break;

                    case "whileStatement":
                        var whileStatement = new WhileStatementCode(
                            compilerErrors,
                            className,
                            subRoutineName,
                            variables,
                            statementPhrase);

                        statementCodes.Add(whileStatement);
                        break;

                    case "doStatement":
                        var doStatement = new DoStatementCode(
                            compilerErrors,
                            className,
                            subRoutineName,
                            variables,
                            statementPhrase);

                        statementCodes.Add(doStatement);
                        break;

                    case "returnStatement":
                        var returnStatement = new ReturnStatementCode(
                            compilerErrors,
                            className,
                            subRoutineName,
                            variables,
                            statementPhrase);

                        statementCodes.Add(returnStatement);
                        break;

                    default:
                        string error =
                            className + "\r\n" +
                            "Expected: " +
                            "'letStatement', " +
                            "'ifStatement', " +
                            "'whileStatement', " +
                            "'doStatement', " +
                            "'returnStatement'\r\n" +
                            "Actual: " + statementPhrase.CategoryName;

                        throw new InvalidOperationException(error);
                }
            }

            return statementCodes;
        }

        internal static string GetDescription(IEnumerable<StatementCode> statements)
        {
            if (statements == null)
            {
                return null;
            }

            return string.Join(
                "\r\n",
                statements.Select(s => s.GetDescription()));
        }
    }
}
