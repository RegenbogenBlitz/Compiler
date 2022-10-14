namespace JackCompiler.MeaningProcessing.TermCodes
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using PreMeaningProcessing.Syntactical;

    internal class StringConstantTermCode : TermCode
    {
        private readonly string value;

        public StringConstantTermCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            ILeafPhrase stringConstantTermPhrase)
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

            if (stringConstantTermPhrase == null)
            {
                throw new ArgumentNullException("stringConstantTermPhrase");
            }
            else if (stringConstantTermPhrase.CategoryName != "stringConstant")
            {
                throw new ArgumentException("Phrase is not 'stringConstant'.");
            }

            this.value = stringConstantTermPhrase.Value;
        }
        
        internal override string GetDescription()
        {
            return "'" + this.value + "'";
        }

        internal override string PushOntoStackCompile()
        {
            var compiledCode = new StringBuilder();

            var length = this.value.Length;

            compiledCode.Append(VmWriterHelper.WritePush(VmSegmentType.ConstantType, length));
            compiledCode.Append(VmWriterHelper.WriteCall("String", "new", 1));

            foreach (var character  in this.value)
            {
                compiledCode.Append(VmWriterHelper.WritePush(VmSegmentType.ConstantType, character));
                compiledCode.Append(VmWriterHelper.WriteCall("String", "appendChar", 2));
            }

            return compiledCode.ToString();
        }
    }
}
