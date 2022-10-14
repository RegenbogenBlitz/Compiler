namespace JackCompiler.MeaningProcessing.TermCodes
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using PreMeaningProcessing.Syntactical;

    internal class UnaryOpModfiedTermCode : TermCode
    {
        private readonly OperatorEnum unaryOperator;
        private readonly TermCode termCode;

        public UnaryOpModfiedTermCode(
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

            var operatorPhrase = termPhrase.LeafChild("symbol"); 
            var subTermPhrase = termPhrase.BranchChild("term");

            var operatorText = operatorPhrase.Value;

            switch (operatorText)
            {
                case "-":
                    this.unaryOperator = OperatorEnum.minus;
                    break;
                case "~":
                    this.unaryOperator = OperatorEnum.not;
                    break;
                default:
                    throw new InvalidOperationException(
                        "Unary operator '" + operatorText + "' not recognised. " +
                        "'-', '~' expected.");
            }

            this.termCode = TermProcessor.Process(
                compilerErrors,
                className,
                subRoutineName,
                variables,
                subTermPhrase);
        }

        internal override string GetDescription()
        {
            return
                this.unaryOperator + "\r\n" + 
                CodeDescriptionHelper.AddTab(this.termCode.GetDescription());
        }

        internal override string PushOntoStackCompile()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.termCode.PushOntoStackCompile());
            
            switch (this.unaryOperator)
            {
                case OperatorEnum.plus:
                    throw new InvalidOperationException("'plus' is not a unary operation.");
                    
                case OperatorEnum.minus:
                    stringBuilder.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.NegateType));
                    break;

                case OperatorEnum.multiply:
                    throw new InvalidOperationException("'multiply' is not a unary operation.");

                case OperatorEnum.divide:
                    throw new InvalidOperationException("'divide' is not a unary operation.");

                case OperatorEnum.and:
                    throw new InvalidOperationException("'and' is not a unary operation.");

                case OperatorEnum.or:
                    throw new InvalidOperationException("'or' is not a unary operation.");

                case OperatorEnum.lessThan:
                    throw new InvalidOperationException("'lessThan' is not a unary operation.");

                case OperatorEnum.greaterThan:
                    throw new InvalidOperationException("'greaterThan' is not a unary operation.");

                case OperatorEnum.equals:
                    throw new InvalidOperationException("'equals' is not a unary operation.");

                case OperatorEnum.not:
                    stringBuilder.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.NotType));
                    break;

                default:
                    throw new InvalidOperationException("Enum value not defined");
            }

            return stringBuilder.ToString();
        }
    }
}
