namespace JackCompiler.MeaningProcessing
{
    internal class CompilerError
    {
        private readonly string className;
        private readonly string subRoutineName;
        private readonly int lineNumber;
        private readonly string message;

        public CompilerError(string className, string subRoutineName, int lineNumber, string message)
        {
            this.className = className;
            this.subRoutineName = subRoutineName;
            this.lineNumber = lineNumber;
            this.message = message;
        }

        public object Description()
        {
            return 
                this.className + "." + this.subRoutineName + "(" + this.lineNumber + ")" + "\r\n"+
                "\t" + this.message;
        }
    }
}
