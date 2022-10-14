namespace JackCompiler.MeaningProcessing.TermCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ExpressionCodes;
    using PreMeaningProcessing.Syntactical;

    internal class VariableUseTermCode : TermCode
    {
        private readonly VariableCode identifier;
        private readonly ExpressionCode arrayIndexExpression;

        internal VariableUseTermCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            ILeafPhrase identifierPhrase,
            IBranchPhrase arrayIndexExpressionPhrase)
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

            if (identifierPhrase == null)
            {
                throw new ArgumentNullException("identifierPhrase");
            }
            else if (identifierPhrase.CategoryName != "identifier")
            {
                throw new ArgumentException("Phrase is not 'identifier'.");
            }

            var identifierName = identifierPhrase.Value;
            if (string.IsNullOrWhiteSpace(identifierName))
            {
                throw new InvalidOperationException("Identifier is null, empty or whitespace.");
            }

            this.identifier = variables.SingleOrDefault(v => v.Name == identifierName);
            if (this.identifier == null)
            {
                var compileError = new CompilerError(
                    className,
                    subRoutineName,
                    identifierPhrase.LineNumber,
                    "Identifier '" + identifierName + "' not declared.");
                compilerErrors.Add(compileError);

                return;
            }

            if (arrayIndexExpressionPhrase == null)
            {
                this.arrayIndexExpression = null;
            }
            else
            {
                if (arrayIndexExpressionPhrase.CategoryName != "expression")
                {
                    throw new ArgumentException("Phrase is not 'expression'.");
                }

                this.arrayIndexExpression =
                    new ExpressionCode(
                        compilerErrors,
                        className,
                        subRoutineName,
                        variables,
                        arrayIndexExpressionPhrase);
            }
        }

        internal override string GetDescription()
        {
            var variableName =
                this.identifier == null
                ? "!error!"
                : this.identifier.Name;

            var arrayIndexExpressionDescription =
                this.arrayIndexExpression == null
                ? string.Empty
                :
                    "\r\n" +
                    "[]:\r\n" +
                    CodeDescriptionHelper.AddTab(this.arrayIndexExpression.GetDescription());

            return
               "variable: " + CodeDescriptionHelper.AddTab(variableName) +
               arrayIndexExpressionDescription;

        }

        internal override string PushOntoStackCompile()
        {
            if (this.arrayIndexExpression == null)
            {
                return this.identifier.Compile();
            }
            else
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.Append(this.arrayIndexExpression.Compile());
                stringBuilder.Append(this.identifier.Compile());
                stringBuilder.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.AddType));
                stringBuilder.Append(VmWriterHelper.WritePop(VmSegmentType.PointerType, 1));
                stringBuilder.Append(VmWriterHelper.WritePush(VmSegmentType.ThatType, 0));

                return stringBuilder.ToString();
            }
        }

        internal Tuple<string, string> PopOffFromStackCompile()
        {
            string preRhs;
            string postRhs;

            if (this.arrayIndexExpression == null)
            {
                preRhs = string.Empty;

                var vmSegmentType = GetVmSegmentType(this.identifier.VariableScope);
                
                postRhs = VmWriterHelper.WritePop(vmSegmentType, this.identifier.Number);
            }
            else
            {
                var preRhsStringBuilder = new StringBuilder();
                
                preRhsStringBuilder.Append(this.arrayIndexExpression.Compile());
                preRhsStringBuilder.Append(this.identifier.Compile());
                preRhsStringBuilder.Append(VmWriterHelper.WriteArithmetic(VmArithmeticType.AddType));

                preRhs = preRhsStringBuilder.ToString();

                var postRhsStringBuilder = new StringBuilder();

                postRhsStringBuilder.Append(VmWriterHelper.WritePop(VmSegmentType.TempType, 0));
                postRhsStringBuilder.Append(VmWriterHelper.WritePop(VmSegmentType.PointerType, 1));
                postRhsStringBuilder.Append(VmWriterHelper.WritePush(VmSegmentType.TempType, 0)); 
                postRhsStringBuilder.Append(VmWriterHelper.WritePop(VmSegmentType.ThatType, 0));

                postRhs = postRhsStringBuilder.ToString();
            }

            return new Tuple<string, string>(preRhs, postRhs);
        }

        private static VmSegmentType GetVmSegmentType(VariableScope variableScope)
        {
            VmSegmentType vmSegmentType;
            switch (variableScope)
            {
                case VariableScope.fieldScope:
                    vmSegmentType = VmSegmentType.ThisType;
                    break;

                case VariableScope.localScope:
                    vmSegmentType = VmSegmentType.LocalType;
                    break;

                case VariableScope.parameterScope:
                    vmSegmentType = VmSegmentType.ArgumentType;
                    break;

                case VariableScope.staticScope:
                    vmSegmentType = VmSegmentType.StaticType;
                    break;

                default:
                    throw new InvalidOperationException("Enum value not defined");
            }
            return vmSegmentType;
        }
    }
}
