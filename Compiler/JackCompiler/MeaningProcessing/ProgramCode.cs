namespace JackCompiler.MeaningProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PreMeaningProcessing.Syntactical;

    internal class ProgramCode
    {
        private readonly List<ClassCode> classes;

        internal ProgramCode(List<CompilerError> compilerErrors, IEnumerable<IPhrase> classPhrases)
        {
            if (classPhrases.Any(c => c.CategoryName != "class"))
            {
                throw new ArgumentException("Not all phrases are 'class'.");
            }
            else if (classPhrases.Any(c => (c as IBranchPhrase) == null))
            {
                throw new ArgumentException("Not all class phrases are branch phrases.");
            }
            
            this.classes = new List<ClassCode>();

            foreach (var classPhrase in classPhrases)
            {
                var newClass = new ClassCode(
                    compilerErrors,
                    this.ClassNames,
                    classPhrase as IBranchPhrase);

                this.classes.Add(newClass);
            }

            var types = new Dictionary<string, VariableTypeCode>
            {
                { "int", new VariableTypeCode(VariableType.intType) },
                { "char", new VariableTypeCode(VariableType.charType) },
                { "boolean", new VariableTypeCode(VariableType.booleanType) }
            };

            foreach (var classCode in this.classes)
            {
                types.Add(classCode.Name, new VariableTypeCode(classCode));
            }

            foreach (var classCode in this.classes)
            {
                classCode.ProcessClassSubRoutineDeclarations(compilerErrors, this.ClassNames, types);
            }

            foreach (var classCode in this.classes)
            {
                classCode.ProcessClassSubRoutineBodies(compilerErrors);
            }
        }

        internal IEnumerable<ClassCode> Classes
        {
            get { return this.classes; }
        }

        internal IEnumerable<string> ClassNames
        {
            get { return this.classes.Select(c => c.Name); }
        }

        internal IReadOnlyDictionary<string, string> GetClassDescriptions()
        {
            var classDescriptions = new Dictionary<string, string>();

            foreach (var classCode in this.Classes)
            {
                classDescriptions.Add(
                    classCode.Name,
                    classCode.GetDescription());
            }

            return classDescriptions;
        }

        internal IReadOnlyDictionary<string, string> Compile()
        {
            var classVmCode = new Dictionary<string, string>();

            foreach (var classCode in this.Classes)
            {
                classVmCode.Add(
                    classCode.Name,
                    classCode.Compile());
            }

            return classVmCode;
        }
    }
}
