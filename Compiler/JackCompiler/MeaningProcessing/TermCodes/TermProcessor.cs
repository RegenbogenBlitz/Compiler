namespace JackCompiler.MeaningProcessing.TermCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PreMeaningProcessing.Syntactical;

    internal static class TermProcessor
    {
        internal static TermCode Process(
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

            switch (termPhrase.Children.Count())
            {
                case 1:
                    var integerConstantPhrase = termPhrase.TryLeafChild("integerConstant");
                    var stringConstantPhrase = termPhrase.TryLeafChild("stringConstant");
                    var keywordPhrase = termPhrase.TryLeafChild("keyword");
                    var varNamePhrase = termPhrase.TryLeafChild("identifier");
                    var subRoutineCallPhrase = termPhrase.TryBranchChild("subroutineCall");

                    if (integerConstantPhrase != null)
                    {
                        return new IntegerConstantTermCode(
                            compilerErrors,
                            className,
                            subRoutineName,
                            variables,
                            integerConstantPhrase);
                    }
                    else if (stringConstantPhrase != null)
                    {
                        return new StringConstantTermCode(
                            compilerErrors,
                            className,
                            subRoutineName,
                            variables,
                            stringConstantPhrase);
                    }
                    else if (keywordPhrase != null)
                    {
                        return new KeywordTermCode(
                            compilerErrors,
                            className,
                            subRoutineName,
                            variables,
                            keywordPhrase);
                    }
                    else if (varNamePhrase != null)
                    {
                        return new VariableUseTermCode(
                            compilerErrors,
                            className,
                            subRoutineName,
                            variables,
                            varNamePhrase,
                            null);
                    }
                    else if (subRoutineCallPhrase != null)
                    {
                        return new SubRoutineCallTermCode(
                            compilerErrors,
                            className,
                            subRoutineName,
                            variables,
                            subRoutineCallPhrase);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Term Category '" + termPhrase.Children.Single().CategoryName + "' unexpected. " +
                            "'integerConstant', 'stringConstant', 'keywordConstant', 'subroutineCall' excepted.");
                    }
                case 2:
                    return new UnaryOpModfiedTermCode(
                        compilerErrors,
                        className,
                        subRoutineName,
                        variables,
                        termPhrase);
                case 3:
                    return new BracketedTermCode(
                        compilerErrors,
                        className,
                        subRoutineName,
                        variables,
                        termPhrase);
                case 4:
                    var children = termPhrase.Children;
                    var arrayedVarNamePhrase = children.ElementAt(0) as ILeafPhrase;
                    var openBracketPhrase = children.ElementAt(1) as ILeafPhrase;
                    var expressionPhrase = children.ElementAt(2) as IBranchPhrase;
                    var closeBracketPhrase = children.ElementAt(3) as ILeafPhrase;

                    if (
                        arrayedVarNamePhrase == null ||
                        arrayedVarNamePhrase.CategoryName != "identifier")
                    {
                        throw new ArgumentException("First child must be an identifier");
                    }
                    else if (
                        openBracketPhrase == null || 
                        openBracketPhrase.CategoryName != "symbol" || 
                        openBracketPhrase.Value != "[")
                    {
                        throw new ArgumentException("Second child must be '['");
                    }
                    else if (
                        expressionPhrase == null || 
                        expressionPhrase.CategoryName != "expression")
                    {
                        throw new ArgumentException("Third child must be an expression");
                    }
                    else if (
                        closeBracketPhrase == null || 
                        closeBracketPhrase.CategoryName != "symbol" || 
                        closeBracketPhrase.Value != "]")
                    {
                        throw new ArgumentException("Forth child must be ']'");
                    }

                    return new VariableUseTermCode(
                        compilerErrors,
                        className,
                        subRoutineName,
                        variables,
                        arrayedVarNamePhrase,
                        expressionPhrase);
                default:
                    throw new InvalidOperationException("Unexpected number of Term components");
            }
        }
    }
}
