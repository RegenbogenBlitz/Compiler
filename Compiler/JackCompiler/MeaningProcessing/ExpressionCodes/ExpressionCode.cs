namespace JackCompiler.MeaningProcessing.ExpressionCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using PreMeaningProcessing.Syntactical;
    using TermCodes;

    internal class ExpressionCode
    {
        private readonly List<TermCode> termCodes;
        private readonly List<OperatorEnum> operators;

        public ExpressionCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            IBranchPhrase expressionPhrase)
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

            if (expressionPhrase == null)
            {
                throw new ArgumentNullException("expressionPhrase");
            }
            else if (expressionPhrase.CategoryName != "expression")
            {
                throw new ArgumentException("Phrase is not 'expression'.");
            }
            
            var termPhrases = expressionPhrase.BranchChildren("term");
            var operatorPhrases = expressionPhrase.LeafChildren("symbol");

            if (termPhrases.Count() - 1 != operatorPhrases.Count())
            {
                throw new InvalidOperationException("Invalid number of Terms relative to Operators");
            }

            this.termCodes = new List<TermCode>();
            foreach (var termPhrase in termPhrases)
            {
                this.termCodes.Add(
                    TermProcessor.Process(
                        compilerErrors,
                        className,
                        subRoutineName,
                        variables,
                        termPhrase));
            }

            this.operators = new List<OperatorEnum>();
            foreach (var operatorPhrase in operatorPhrases)
            {
                var operatorText = operatorPhrase.Value;

                switch (operatorText)
                {
                    case "+":
                        this.operators.Add(OperatorEnum.plus);
                        break;
                    case "-":
                        this.operators.Add(OperatorEnum.minus);
                        break;
                    case "*":
                        this.operators.Add(OperatorEnum.multiply);
                        break;
                    case "/":
                        this.operators.Add(OperatorEnum.divide);
                        break;
                    case "&":
                        this.operators.Add(OperatorEnum.and);
                        break;
                    case "|":
                        this.operators.Add(OperatorEnum.or);
                        break;
                    case "<":
                        this.operators.Add(OperatorEnum.lessThan);
                        break;
                    case ">":
                        this.operators.Add(OperatorEnum.greaterThan);
                        break;
                    case "=":
                        this.operators.Add(OperatorEnum.equals);
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Binary operator '" + operatorText + "' not recognised. " +
                            "'+', '-', '*', '/', '&', '|', '<', '>', '=' expected.");
                }
            }
        }

        internal string GetDescription()
        {
            var lastTermIndex = this.termCodes.Count - 1;

            var description = string.Empty;

            for (int i = 0; i <= lastTermIndex; i++)
            {
                description +=
                    this.termCodes.ElementAt(i).GetDescription();

                if (i < lastTermIndex)
                {
                    description +=
                    "\r\n"+
                    this.operators.ElementAt(i) + "\r\n";
                }
            }

            return description;
        }

        internal string Compile()
        {
            if (this.termCodes.Count - 1 != this.operators.Count)
            {
                throw new InvalidOperationException("Invalid number of Terms relative to Operators");
            }

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.termCodes.ElementAt(0).PushOntoStackCompile());
            for (int i = 0; i < this.operators.Count; i++)
            {
                stringBuilder.Append(this.termCodes.ElementAt(i+1).PushOntoStackCompile());

                switch (this.operators.ElementAt(i))
                {
                    case OperatorEnum.plus:
                        stringBuilder.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.AddType));
                        break;

                    case OperatorEnum.minus:
                        stringBuilder.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.SubtractType));
                        break;

                    case OperatorEnum.multiply:
                        stringBuilder.Append(VmWriterHelper.WriteCall("Math", "multiply", 2));
                        break;

                    case OperatorEnum.divide:
                        stringBuilder.Append(VmWriterHelper.WriteCall("Math", "divide", 2));
                        break;

                    case OperatorEnum.and:
                        stringBuilder.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.AndType));
                        break;

                    case OperatorEnum.or:
                        stringBuilder.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.OrType));
                        break;

                    case OperatorEnum.lessThan:
                        stringBuilder.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.LessThanType));
                        break;

                    case OperatorEnum.greaterThan:
                        stringBuilder.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.GreaterThanType));
                        break;

                    case OperatorEnum.equals:
                        stringBuilder.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.EqualityType));
                        break;

                    case OperatorEnum.not:
                        throw new InvalidOperationException("'not' is not a binary operation.");

                    default:
                        throw new InvalidOperationException("Enum value not defined");
                }
            }

            return stringBuilder.ToString();
        }
    }
}
