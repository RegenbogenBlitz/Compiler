namespace JackCompiler.MeaningProcessing
{
    using System;

    internal class VariableTypeCode
    {
        private readonly VariableType variableType;
        private readonly ClassCode classCode;

        public VariableTypeCode(VariableType variableType)
        {
            this.variableType = variableType;
            this.classCode = null;
        }

        public VariableTypeCode(ClassCode classCode)
        {
            this.variableType = VariableType.classType;
            this.classCode = classCode;
        }

        public VariableType VariableType
        {
            get { return this.variableType; }
        }

        public ClassCode ClassCode
        {
            get { return this.classCode; }
        }

        internal string GetDescription()
        {
            switch (this.VariableType)
            {
                case VariableType.intType:
                    return "int";
                case VariableType.charType:
                    return "char";
                case VariableType.booleanType:
                    return "boolean";
                case VariableType.classType:
                    return this.classCode.Name;
                default:
                    throw new InvalidOperationException("Code not reachable");
            }
        }
    }
}
