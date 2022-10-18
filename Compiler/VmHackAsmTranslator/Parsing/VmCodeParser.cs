using FileHandling;

namespace VmHackAsmTranslator.Parsing;

public static class VmCodeParser
{
    public static VmCode Parse(IEnumerable<InputFileInfo> inputFiles)
    {
        var code =
            inputFiles
                .Select(ParseFile)
                .SelectMany(c=>c)
                .ToArray();
        
        return new VmCode(code);
    }

    private static IEnumerable<ICommand> ParseFile(InputFileInfo inputFile)
    {
        var lineNumber = 0;
        var commands = new List<ICommand>();
        
        foreach (var lineContent in inputFile.Content)
        {
            lineNumber++;
            commands.Add(ParseLine(lineNumber, lineContent));
        }

        return commands;
    }
    
    private static ICommand ParseLine(int lineNumber, string line)
    {
        var trimmedLine = TrimLine(line);
        if (string.IsNullOrWhiteSpace(trimmedLine))
        {
            return new NonCommand();
        }

        var lineComponents = trimmedLine.Split(' ');

        switch (lineComponents[0])
        {
            case "push":
                return new PushCommand(trimmedLine);

            case "pop":
                return new PopCommand(trimmedLine);

            case "add":
            case "sub":
            case "neg":
            case "and":
            case "or":
            case "not":
            case "eq":
            case "lt":
            case "gt":
                return new ArithmeticCommand(trimmedLine);

            case "label":
                return new LabelCommand(trimmedLine);

            case "if-goto":
                return new IfGotoCommand(trimmedLine);

            case "goto":
                return new GotoCommand(trimmedLine);

            case "return":
                return new ReturnCommand();
            
            case "function":
                return new FunctionDeclarationCommand(trimmedLine);
            
            case "call":
                return new FunctionCallCommand(trimmedLine);

            default:
                throw new TranslationException(
                    lineNumber, 
                    line, 
                    "Expected command to start with " +
                    "'push', 'pop', 'add', 'sub', 'neg', 'and', 'or', 'not', 'eq', 'lt', 'gt', 'label', 'if-goto', 'return', 'function', 'call'" +
                    " or be a comment");
        }
    }
    
    private static string TrimLine(string line)
    {
        if (line.Contains("//"))
        {
            var commentStart = line.IndexOf("//", StringComparison.OrdinalIgnoreCase);
            var trimmedLine = commentStart == 0
                ? ""
                : line.Substring(0, commentStart).Trim();
            return trimmedLine;
        }
        else
        {
            return line.Trim();
        }
    }
}