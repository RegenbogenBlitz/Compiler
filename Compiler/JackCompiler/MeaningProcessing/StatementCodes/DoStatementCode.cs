namespace JackCompiler.MeaningProcessing.StatementCodes
{
    using System;
    using System.Collections.Generic;
    using PreMeaningProcessing.Syntactical;
    using TermCodes;

    internal class DoStatementCode : StatementCode
    {
        private readonly SubRoutineCallTermCode subRoutineCall;

        internal DoStatementCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            IBranchPhrase doStatementPhrase)
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

            if (doStatementPhrase == null)
            {
                throw new ArgumentNullException("doStatementPhrase");
            }
            else if (doStatementPhrase.CategoryName != "doStatement")
            {
                throw new ArgumentException("Phrase is not 'doStatement'.");
            }

            var subRoutineCallPhrase = doStatementPhrase.BranchChild("subroutineCall");
            if (subRoutineCallPhrase == null)
            {
                throw new InvalidOperationException("subRoutineCallPhrase Phrase is null.");
            }

            this.subRoutineCall =
                new SubRoutineCallTermCode(
                    compilerErrors,
                    className,
                    subRoutineName,
                    variables,
                    subRoutineCallPhrase);
        }

        internal override string GetDescription()
        {
            return 
                "do\r\n" +
                CodeDescriptionHelper.AddTab(this.subRoutineCall.GetDescription());
        }

        internal override string Compile()
        {
            return 
                this.subRoutineCall.PushOntoStackCompile() +
                VmWriterHelper.WritePop(VmSegmentType.TempType, 0);
        }
    }
}
