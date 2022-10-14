namespace JackCompiler.MeaningProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using PreMeaningProcessing.Syntactical;

    internal class ClassCode
    {
        private readonly IBranchPhrase classPhrase;
        private readonly string className;
        
        private readonly List<VariableCode> variables;
        private readonly List<SubRoutineCode> subRoutines;

        internal ClassCode(
            List<CompilerError> compilerErrors,
            IEnumerable<string> takenNames,
            IBranchPhrase classPhrase)
        {
            if (classPhrase.CategoryName != "class")
            {
                throw new ArgumentException("Phrase is not 'class'.");
            }

            this.classPhrase = classPhrase;

            var classNamePhrase = classPhrase.LeafChild("identifier");
            this.className = classNamePhrase.Value;

            if (takenNames.Contains(this.className))
            {
                compilerErrors.Add(
                    new CompilerError(
                        this.className,
                        null,
                        classNamePhrase.LineNumber,
                        "Class Name already taken"));
            }

            this.variables = new List<VariableCode>();
            this.subRoutines = new List<SubRoutineCode>();
        }

        public string Name
        {
            get { return this.className; }
        }

        public int NumberOfFields
        {
            get { return this.variables.Count(v => v.VariableScope == VariableScope.fieldScope); }
        }

        internal void ProcessClassSubRoutineDeclarations(
            List<CompilerError> compilerErrors,
            IEnumerable<string> takenNames,
            IReadOnlyDictionary<string, VariableTypeCode> variableTypes)
        {
            var classVarDecs = this.classPhrase.BranchChildren("classVarDec");

            var staticVariableProcessor = new VariableDeclarationsCode(VariableScope.staticScope);
            var fieldVariableProcessor = new VariableDeclarationsCode(VariableScope.fieldScope);

            foreach (var classVarDec in classVarDecs)
            {
                var keywordPhrase = classVarDec.LeafChild("keyword");

                if (keywordPhrase.Value == "static")
                {
                    var staticVariable = staticVariableProcessor.ProcessVarDec(
                        compilerErrors,
                        this.GetTakenNames(takenNames),
                        variableTypes,
                        this.className,
                        null,
                        classVarDec);

                    this.variables.AddRange(staticVariable);
                }
                else if (keywordPhrase.Value == "field")
                {
                    var fieldVariable = fieldVariableProcessor.ProcessVarDec(
                        compilerErrors,
                        this.GetTakenNames(takenNames),
                        variableTypes,
                        this.className,
                        null,
                        classVarDec);

                    this.variables.AddRange(fieldVariable);
                }
                else
                {
                    throw new InvalidOperationException("scope is neither 'static' nor 'field'");
                }
            }

            var subRoutineDecs = this.classPhrase.BranchChildren("subroutineDec");

            foreach (var subRoutineDec in subRoutineDecs)
            {
                var keywordPhrase = subRoutineDec.Child(0) as ILeafPhrase;

                if (keywordPhrase == null)
                {
                    throw new InvalidOperationException("subRoutine type not found");
                }

                SubRoutineType subRoutineType;

                if (keywordPhrase.Value == "function")
                {
                    subRoutineType = SubRoutineType.Function;
                }
                else if (keywordPhrase.Value == "constructor")
                {
                    subRoutineType = SubRoutineType.Constructor;
                }
                else if (keywordPhrase.Value == "method")
                {
                    subRoutineType = SubRoutineType.Method; 
                }
                else
                {
                    throw new InvalidOperationException(
                        "subRoutine type is none of 'constructor', 'function', 'method'");
                }

                var subRoutineCode = new SubRoutineCode(
                    compilerErrors,
                    this.GetTakenNames(takenNames),
                    variableTypes,
                    this,
                    subRoutineType,
                    subRoutineDec);

                this.subRoutines.Add(subRoutineCode);
            }
        }

        internal void ProcessClassSubRoutineBodies(List<CompilerError> compilerErrors)
        {
            foreach (var subRoutine in this.subRoutines)
            {
                subRoutine.ProcessStatements(compilerErrors, this.variables);
            }
        }

        private IEnumerable<string> GetTakenNames(IEnumerable<string> takenNames)
        {
            return takenNames
                .Concat(this.variables.Select(v => v.Name))
                .Concat(this.subRoutines.Select(sr => sr.Name))
                .ToArray();
        }

        internal string GetDescription()
        {
            var content = "Class: " + this.Name + "\r\n";

            content += string.Join("",
                this.variables
                .Where(v=>v.VariableScope == VariableScope.fieldScope)
                .Select(v => CodeDescriptionHelper.AddTab(v.GetDescription()) + "\r\n"));

            content += string.Join("",
                this.variables
                .Where(v => v.VariableScope == VariableScope.staticScope)
                .Select(v => CodeDescriptionHelper.AddTab(v.GetDescription()) + "\r\n"));

            content += "\r\n";

            foreach (var subRoutineCode in this.subRoutines)
            {
                content += subRoutineCode.GetDescription() + "\r\n";
            }

            return content;
        }

        internal string Compile()
        {
            var stringBuilder = new StringBuilder();

            foreach (var methodCode in this.subRoutines)
            {
                stringBuilder.Append(methodCode.Compile());
            }

            return stringBuilder.ToString();
        }
    }
}
