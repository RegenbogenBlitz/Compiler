namespace JackCompiler.MeaningProcessing.TermCodes
{
    using System;
    using System.Collections.Generic;
    using PreMeaningProcessing.Syntactical;

    internal class IntegerConstantTermCode : TermCode
    {
        private readonly int value;

        public IntegerConstantTermCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            ILeafPhrase integerConstantTermPhrase)
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

            if (integerConstantTermPhrase == null)
            {
                throw new ArgumentNullException("integerConstantTermPhrase");
            }
            else if (integerConstantTermPhrase.CategoryName != "integerConstant")
            {
                throw new ArgumentException("Phrase is not 'integerConstant'.");
            }

            this.value = Convert.ToInt32(integerConstantTermPhrase.Value);
        }
        
        internal override string GetDescription()
        {
            return this.value.ToString();
        }

        internal override string PushOntoStackCompile()
        {
            return VmWriterHelper.WritePush(VmSegmentType.ConstantType, this.value);
        }
    }
}
