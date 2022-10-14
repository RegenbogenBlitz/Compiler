namespace JackCompiler.MeaningProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using PreMeaningProcessing.Syntactical;
    using StatementCodes;

    internal class SubRoutineCode
    {
        private readonly ClassCode classCode;
        private readonly string subRoutineName;
        private readonly SubRoutineType subRoutineType;
        private readonly VariableTypeCode returnType;
        private readonly List<VariableCode> parameters;
        private readonly List<VariableCode> locals;

        private readonly IBranchPhrase statementsPhrase;
        private IEnumerable<StatementCode> statementCodes;

        public SubRoutineCode(
            List<CompilerError> compilerErrors,
            IEnumerable<string> takenNames,
            IReadOnlyDictionary<string, VariableTypeCode> variableTypes,
            ClassCode classCode,
            SubRoutineType subRoutineType,
            IBranchPhrase subRoutineDecPhrase)
        {
            if (subRoutineDecPhrase.CategoryName != "subroutineDec")
            {
                throw new ArgumentException("Phrase is not 'subroutineDec'.");
            }

            this.classCode = classCode;
            this.subRoutineType = subRoutineType;

            var subRoutineNamePhrase = subRoutineDecPhrase.LeafChild("identifier");
            this.subRoutineName = subRoutineNamePhrase.Value;

            if (takenNames.Contains(this.subRoutineName))
            {
                compilerErrors.Add(
                    new CompilerError(
                        this.classCode.Name,
                        this.subRoutineName,
                        subRoutineNamePhrase.LineNumber,
                        "SubRoutine Name: Name '" + this.subRoutineName + "' already taken"));
            }

            this.returnType = VariableDeclarationsCode.GetSubRoutineVariableTypeCode(
                compilerErrors,
                variableTypes,
                this.classCode.Name,
                this.subRoutineName,
                subRoutineDecPhrase);

            this.parameters = new List<VariableCode>();
            this.locals = new List<VariableCode>();

            VariableDeclarationsCode parameterVariableProcessor;
            if (this.subRoutineType == SubRoutineType.Method)
            {
                parameterVariableProcessor = 
                    VariableDeclarationsCode.NewFunctionParameterVariableDeclarationsCode();
            }
            else
            {
                parameterVariableProcessor = new VariableDeclarationsCode(VariableScope.parameterScope);
            }
            
            var localVariableProcessor = new VariableDeclarationsCode(VariableScope.localScope);

            var parameterListPhrase = subRoutineDecPhrase.BranchChild("parameterList");
            var parameterPhrases = parameterListPhrase.BranchChildren("parameter");
            foreach (var parameterPhrase in parameterPhrases)
            {
                var parameterVariable = parameterVariableProcessor.ProcessVarDec(
                    compilerErrors,
                    this.GetTakenNames(takenNames),
                    variableTypes,
                    this.classCode.Name,
                    this.subRoutineName,
                    parameterPhrase);

                this.parameters.AddRange(parameterVariable);
            }

            var subRoutineBodyPhrase = subRoutineDecPhrase.BranchChild("subroutineBody");
            var varDecPhrases = subRoutineBodyPhrase.BranchChildren("varDec");
            
            foreach (var varDecPhrase in varDecPhrases)
            {
                var localVariables = localVariableProcessor.ProcessVarDec(
                    compilerErrors,
                    this.GetTakenNames(takenNames),
                    variableTypes,
                    this.classCode.Name,
                    this.subRoutineName,
                    varDecPhrase);

                this.locals.AddRange(localVariables);
            }

            this.statementsPhrase = subRoutineBodyPhrase.BranchChild("statements");
        }

        public string Name
        {
            get { return this.subRoutineName; }
        }

        private SubRoutineType RoutineType
        {
            get { return this.subRoutineType; }
        }

        internal void ProcessStatements(
            List<CompilerError> compilerErrors,
            IEnumerable<VariableCode> classVariables)
        {
            this.statementCodes = StatementsProcessor.Process(
                compilerErrors,
                this.classCode.Name,
                this.Name,
                classVariables.Concat(this.parameters).Concat(this.locals),
                this.statementsPhrase);
        }

        private IEnumerable<string> GetTakenNames(IEnumerable<string> takenNames)
        {
            return takenNames
                .Concat(this.parameters.Concat(this.locals).Select(v => v.Name))
                .ToArray();
        }

        internal string GetDescription()
        {
            var returnTypeDescription =
                this.returnType == null
                    ? "void"
                    : this.returnType.GetDescription();

            var signature = this.subRoutineType.ToString() + ": " + this.Name + " => " + returnTypeDescription + "\r\n";

            var content = string.Empty;

            content +=
                string.Join(
                    "",
                    this.parameters
                    .Select(v => v.GetDescription() + "\r\n")
                    );

            content +=
                string.Join(
                    "",
                    this.locals
                    .Select(v => v.GetDescription() + "\r\n")
                    );

            string statementContent = StatementsProcessor.GetDescription(this.statementCodes);

            return
                signature + CodeDescriptionHelper.AddTab(
                    content + CodeDescriptionHelper.AddTab(
                        statementContent));
        }

        internal string Compile()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(
                VmWriterHelper.WriteFunction(this.classCode.Name, this.Name, this.locals.Count));

            if (this.RoutineType == SubRoutineType.Constructor)
            {
                stringBuilder.Append(
                    VmWriterHelper.WritePush(VmSegmentType.ConstantType, this.classCode.NumberOfFields));
                stringBuilder.Append(
                   VmWriterHelper.WriteCall("Memory", "alloc", 1));
                stringBuilder.Append(
                    VmWriterHelper.WritePop(VmSegmentType.PointerType, 0));
            }
            else if (this.RoutineType == SubRoutineType.Method)
            {
                stringBuilder.Append(
                    VmWriterHelper.WritePush(VmSegmentType.ArgumentType, 0));
                stringBuilder.Append(
                    VmWriterHelper.WritePop(VmSegmentType.PointerType, 0));
            }

            foreach (var statement in this.statementCodes)
            {
                stringBuilder.Append(statement.Compile());
            }

            return stringBuilder.ToString();
        }
    }
}
