namespace JackCompiler.MeaningProcessing.TermCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ExpressionCodes;
    using PreMeaningProcessing.Syntactical;

    internal class SubRoutineCallTermCode : TermCode
    {
        private readonly string ownerClassName; 
        private readonly ObjectReference ownerObject;

        private readonly string subRoutineName;

        private readonly IEnumerable<ExpressionCode> parameters;

        internal SubRoutineCallTermCode(
            List<CompilerError> compilerErrors,
            string className,
            string subRoutineName,
            IEnumerable<VariableCode> variables,
            IBranchPhrase subRoutineCallPhrase)
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

            if (subRoutineCallPhrase == null)
            {
                throw new ArgumentNullException("subRoutineCallPhrase");
            }
            else if (subRoutineCallPhrase.CategoryName != "subroutineCall")
            {
                throw new ArgumentException("Phrase is not 'subroutineCall'.");
            }

            var identifierPhrases = subRoutineCallPhrase.LeafChildren("identifier");
            if (identifierPhrases == null || !identifierPhrases.Any())
            {
                throw new InvalidOperationException("Identifier Phrases is null or empty.");
            }

            ILeafPhrase ownerIdentifierPhrase;
            ILeafPhrase subRoutineNamePhrase;
            if (identifierPhrases.Count() == 1)
            {
                ownerIdentifierPhrase = null;
                subRoutineNamePhrase = identifierPhrases.Single();
                
            }
            else if (identifierPhrases.Count() == 2)
            {
                ownerIdentifierPhrase = identifierPhrases.First(); 
                subRoutineNamePhrase = identifierPhrases.Last();
            }
            else
            {
                throw new InvalidOperationException("Incorrect number of identifier sections.");
            }

            if (ownerIdentifierPhrase == null)
            {
                this.ownerClassName = className;
                this.ownerObject = ObjectReference.This();
            }
            else
            {
                var identifierValue = ownerIdentifierPhrase.Value;
                var matchingVariable = variables.SingleOrDefault(v => v.Name == identifierValue);
                if (matchingVariable == null)
                {
                    this.ownerClassName = identifierValue;
                    this.ownerObject = null;
                }
                else
                {
                    this.ownerClassName = matchingVariable.VariableTypeCode.ClassCode.Name;
                    this.ownerObject = ObjectReference.That(matchingVariable);
                }
            }

            this.subRoutineName = subRoutineNamePhrase.Value;

            var expressionList = subRoutineCallPhrase.BranchChild("expressionList");
            this.parameters = 
                ExpressionListProcessor.Process(
                    compilerErrors,
                    className,
                    subRoutineName,
                    variables,
                    expressionList);
        }

        internal override string GetDescription()
        {
            string ownerObjectDescription =
                this.ownerObject != null
                ?
                    "Owner Object:\r\n" +
                    CodeDescriptionHelper.AddTab(this.ownerObject.GetDescription()) + "\r\n"
                : string.Empty;

            return
                "subroutine call\r\n" +
                CodeDescriptionHelper.AddTab(
                    "Owner Class:\r\n" +
                    CodeDescriptionHelper.AddTab(this.ownerClassName) + "\r\n" +
                    ownerObjectDescription+
                    "SubRoutine:\r\n" +
                    CodeDescriptionHelper.AddTab(this.subRoutineName) + "\r\n" +
                    "Parameters:\r\n" +
                    CodeDescriptionHelper.AddTab(
                        ExpressionListProcessor.GetDescription(this.parameters)));
        }

        internal override string PushOntoStackCompile()
        {
            return CompileCall(
                this.ownerObject,
                this.parameters,
                this.ownerClassName,
                this.subRoutineName);
        }

        private static string CompileCall(
            ObjectReference ownerObject,
            IEnumerable<ExpressionCode> parameters,
            string ownerClassName,
            string subRoutineName)
        {
            var stringBuilder = new StringBuilder();

            int numOfParameters;

            if (ownerObject == null)
            {
                numOfParameters = parameters.Count();
            }
            else
            {
                numOfParameters = parameters.Count() + 1; 
                stringBuilder.Append(ownerObject.Compile());
            }

            foreach (var parameter in parameters)
            {
                stringBuilder.Append(parameter.Compile());
            }

            stringBuilder.Append(VmWriterHelper.WriteCall(ownerClassName, subRoutineName, numOfParameters));
            
            return stringBuilder.ToString();
        }
    }
}
