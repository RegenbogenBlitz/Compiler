namespace JackCompiler.MeaningProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PreMeaningProcessing.Syntactical;

    internal class VariableDeclarationsCode
    {
        private readonly VariableScope scope;

        private int nextNumber;

        internal VariableDeclarationsCode(VariableScope scope)
        {
            this.scope = scope;
            this.nextNumber = 0;
        }

        internal static VariableDeclarationsCode NewFunctionParameterVariableDeclarationsCode()
        {
            var variableDeclarationsCode = new VariableDeclarationsCode(VariableScope.parameterScope);
            variableDeclarationsCode.nextNumber++;
            return variableDeclarationsCode;
        }

        internal IEnumerable<VariableCode> ProcessVarDec(
            List<CompilerError> compilerErrors,
            IEnumerable<string> takenNames,
            IReadOnlyDictionary<string, VariableTypeCode> variableTypes,
            string className,
            string subRoutineName,
            IBranchPhrase varDecPhrase)
        {
            var variables = new List<VariableCode>();

            var variableType = GetVariableTypeCode(
                compilerErrors,
                variableTypes,
                className,
                subRoutineName,
                varDecPhrase);

            var identifierPhrases = varDecPhrase.LeafChildren("identifier");
            foreach (var identifierPhrase in identifierPhrases)
            {
                string variableName = identifierPhrase.Value;
                if (takenNames.Contains(variableName))
                {
                    compilerErrors.Add(new CompilerError(
                        className,
                        subRoutineName,
                        identifierPhrase.LineNumber,
                        "Variable: Name '" + variableName + "' already used"));
                }

                var variable = new VariableCode(this.scope, this.nextNumber, variableType, variableName);
                this.nextNumber++;

                variables.Add(variable);
            }

            return variables;
        }

        internal static VariableTypeCode GetSubRoutineVariableTypeCode(
            List<CompilerError> compilerErrors,
            IReadOnlyDictionary<string, VariableTypeCode> variableTypes,
            string className,
            string subRoutineName,
            IBranchPhrase typedDecPhrase)
        {
            var secondPhrase = typedDecPhrase.Child(1);
            if (secondPhrase.CategoryName == "keyword")
            {
                return null;
            }
            else
            {
                return GetVariableTypeCode(
                    compilerErrors,
                    variableTypes,
                    className,
                    subRoutineName,
                    typedDecPhrase);
            }
        }

        private static VariableTypeCode GetVariableTypeCode(
            List<CompilerError> compilerErrors,
            IReadOnlyDictionary<string, VariableTypeCode> variableTypes,
            string className,
            string subRoutineName,
            IBranchPhrase typedDecPhrase)
        {
            var typePhrase = typedDecPhrase.BranchChild("type");

            var typeKeywordPhrase = typePhrase.TryLeafChild("keyword");
            var typeIdentifierPhrase = typePhrase.TryLeafChild("identifier");

            VariableTypeCode variableType;
            if (typeIdentifierPhrase != null)
            {
                if (!variableTypes.TryGetValue(typeIdentifierPhrase.Value, out variableType))
                {
                    compilerErrors.Add(new CompilerError(
                        className,
                        subRoutineName,
                        typeIdentifierPhrase.LineNumber,
                        "Variable Type: Class Name '" + typeIdentifierPhrase.Value + "' not found"));
                }
            }
            else if (typeKeywordPhrase != null)
            {
                if (!(typeKeywordPhrase.Value == "int" ||
                    typeKeywordPhrase.Value == "char" ||
                    typeKeywordPhrase.Value == "boolean"))
                {
                    throw new InvalidOperationException(
                        "keyword type is none of 'int', 'char', 'boolean'");
                }

                if (!variableTypes.TryGetValue(typeKeywordPhrase.Value, out variableType))
                {
                    throw new InvalidOperationException(
                        "keyword type '" + typeKeywordPhrase.Value + "' not found in Type dictionary");
                }
            }
            else
            {
                throw new InvalidOperationException("type keyword/identifier not found");
            }

            return variableType;
        }
    }
}
