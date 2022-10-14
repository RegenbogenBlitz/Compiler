namespace JackCompiler.MeaningProcessing.TermCodes
{
    using System;
    using System.Collections.Generic;
    using PreMeaningProcessing.Syntactical;

    internal class KeywordTermCode : TermCode
    {
        private readonly KeywordEnum value;

        public KeywordTermCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            ILeafPhrase keywordTermPhrase)
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

            if (keywordTermPhrase == null)
            {
                throw new ArgumentNullException("keywordTermPhrase");
            }
            else if (keywordTermPhrase.CategoryName != "keyword")
            {
                throw new ArgumentException("Phrase is not 'keyword'.");
            }

            switch (keywordTermPhrase.Value)
            {
                case "true":
                    this.value = KeywordEnum.trueValue;
                    break;
                case "false":
                    this.value  = KeywordEnum.falseValue; 
                    break;
                case "null":
                    this.value  = KeywordEnum.nullValue;
                    break;
               case "this":
                    this.value = KeywordEnum.thisValue; 
                    break;
                default:
                    throw new InvalidOperationException(
                        "Keyword '" + keywordTermPhrase.Value + "' not recognised. " +
                        "'true', 'false', 'null', 'this' expected.");
            }
        }
        
        internal override string GetDescription()
        {
            return this.value.ToString();
        }

        internal override string PushOntoStackCompile()
        {
            switch (this.value)
            {
                case KeywordEnum.trueValue:
                    return
                        VmWriterHelper.WritePush(VmSegmentType.ConstantType, 0) +
                        VmWriterHelper.WriteArithmetic(VmArithmeticType.NotType);

                case KeywordEnum.falseValue:
                case KeywordEnum.nullValue:
                    return VmWriterHelper.WritePush(VmSegmentType.ConstantType, 0);
                    
                case KeywordEnum.thisValue:
                    return VmWriterHelper.WritePush(VmSegmentType.PointerType, 0);

                default:
                    throw new InvalidOperationException("Enum value not defined");
            }
        }
    }
}
