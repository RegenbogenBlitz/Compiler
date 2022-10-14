namespace JackCompiler.MeaningProcessing
{
    using System;

    internal class VariableCode
    {
        private readonly VariableScope variableScope;
        private readonly int number;
        private readonly VariableTypeCode variableTypeCode;
        private readonly string name;


        public VariableCode(
            VariableScope variableScope,
            int number,
            VariableTypeCode variableTypeCode,
            string name)
        {
            this.variableScope = variableScope;
            this.number = number;
            this.variableTypeCode = variableTypeCode;
            this.name = name;
        }

        public VariableScope VariableScope
        {
            get { return this.variableScope; }
        }

        public int Number
        {
            get { return this.number; }
        }

        public VariableTypeCode VariableTypeCode
        {
            get { return this.variableTypeCode; }
        }

        public string Name
        {
            get { return this.name; }
        }

        internal string GetDescription()
        {
            return
                this.variableScope + " " + this.number + ": " +
                this.variableTypeCode.GetDescription() + " " +
                this.name;
        }

        public string Compile()
        {
            switch (this.VariableScope)
            {
                case VariableScope.fieldScope:
                    return VmWriterHelper.WritePush(VmSegmentType.ThisType, this.Number);

                case VariableScope.localScope:
                    return VmWriterHelper.WritePush(VmSegmentType.LocalType, this.Number);

                case VariableScope.parameterScope:
                    return VmWriterHelper.WritePush(VmSegmentType.ArgumentType, this.Number);

                case VariableScope.staticScope:
                    return VmWriterHelper.WritePush(VmSegmentType.StaticType, this.Number);

                default:
                    throw new InvalidOperationException("Enum value not defined");
            }
        }
    }
}
