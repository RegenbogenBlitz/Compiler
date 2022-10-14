namespace JackCompiler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using MeaningProcessing;
    using PreMeaningProcessing.Lexical;
    using PreMeaningProcessing.Syntactical;

    internal static class Compiler
    {
        internal static IEnumerable<OutputFileInfo> Compile(IEnumerable<InputFileInfo> fileInfos)
        {
            var outputFileInfos = new List<OutputFileInfo>();

            var invalidLexicalElementMessages = new List<string>();

            var classPhrases = new List<IPhrase>();

            foreach (InputFileInfo inputFileInfo in fileInfos)
            {
                IEnumerable<LexicalElement> lexicalElements = JackTokenizer.Tokenize(inputFileInfo.Content);
                var theseInvalidLexicalElementMessages =
                    lexicalElements
                    .Where(element => !element.IsValid)
                    .Select(element =>
                        string.Format(
                            "Filename: '{0}', Line Number: {1}, Value: '{2}'",
                            inputFileInfo.FileName,
                            element.LineNumber,
                            element.Value)).ToArray();

                invalidLexicalElementMessages.AddRange(theseInvalidLexicalElementMessages);

                if (!invalidLexicalElementMessages.Any())
                {
                    var bookmarkedLexicalElements = new BookmarkedArray<LexicalElement>(lexicalElements.ToArray());
                    var syntaxAnalysisResult = JackSyntacticalAnalyiser.Analyse(bookmarkedLexicalElements);

                    if (syntaxAnalysisResult.Success)
                    {
                        classPhrases.AddRange(syntaxAnalysisResult.GrabbedPhrases);
                    }
                    else
                    {
                        var outFile = new OutputFileInfo(
                            inputFileInfo.FileName,
                            syntaxAnalysisResult.Detail.ToString());

                        outputFileInfos.Add(outFile);
                    }
                }
            }

            if (invalidLexicalElementMessages.Any())
            {
                throw new Exception("Invalid Lexical Elements:\n" + string.Join("\n", invalidLexicalElementMessages));
            }

            if (!outputFileInfos.Any())
            {
                var compilerErrors = new List<CompilerError>();
                var program = new ProgramCode(compilerErrors, classPhrases);

                if (compilerErrors.Any())
                {
                    var report = string.Join("\r\n", compilerErrors.Select(e=>e.Description()));
                    outputFileInfos.Add(new OutputFileInfo("CompilerErrors", report));
                }
                else
                {
                    var classDescriptions = program.GetClassDescriptions();
                    foreach (var className in classDescriptions.Keys)
                    {
                        var content = classDescriptions[className];

                        outputFileInfos.Add(new OutputFileInfo(className, "txt", content));
                    }

                    var classVmCodes = program.Compile();
                    foreach (var className in classVmCodes.Keys)
                    {
                        var content = classVmCodes[className];

                        outputFileInfos.Add(new OutputFileInfo(className, content));
                    }
                }

                outputFileInfos.Add(new OutputFileInfo("Analysis", "xml", GetSyntaxXml(classPhrases)));
            }

            return outputFileInfos;
        }
        
        private static string GetSyntaxXml(IEnumerable<IPhrase> phrases)
        {
            if (phrases == null)
            {
                throw new ArgumentNullException("phrases");
            }

            var xml = new XDocument();

            var elements = GetSyntaxXmlElement(phrases);

            if (elements.Count() == 1)
            {
                xml.Add(elements.Single());
            }
            else
            {
                var syntaxElement = new XElement("syntax");
                xml.Add(syntaxElement);
                syntaxElement.Add(elements);
            }

            return xml.ToString();
        }

        private static IEnumerable<XElement> GetSyntaxXmlElement(IEnumerable<IPhrase> phrases)
        {
            if (phrases == null)
            {
                throw new ArgumentNullException("phrases");
            }

            var elements = new List<XElement>();

            foreach (IPhrase phrase in phrases)
            {
                var leafPhrase = phrase as ILeafPhrase;
                var branchPhrase = phrase as IBranchPhrase;

                if (leafPhrase != null)
                {
                    elements.Add(new XElement(
                        leafPhrase.CategoryName,
                        " " + leafPhrase.Value + " "));
                }
                else if (branchPhrase != null)
                {
                    var element = new XElement(branchPhrase.CategoryName);

                    var subElements = GetSyntaxXmlElement(branchPhrase.Children);

                    element.Add(subElements);
                    elements.Add(element);
                }
                else
                {
                    throw new InvalidOperationException("phrase is neither leaf nor branch");
                }
            }

            return elements;
        }
    }
}
