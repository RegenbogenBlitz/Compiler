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
            {
                if (lineComponents.Length != 3)
                {
                    throw new ParserException(lineNumber, line, "expected 'push SEGMENT INDEX'");
                }
        
                if (!uint.TryParse(lineComponents[2], out var index))
                {
                    throw new ParserException(lineNumber, line,
                        "expected 'push SEGMENT INDEX', where INDEX is positive integer");
                }

                var segment = lineComponents[1] switch
                {
                    "argument" => SegmentType.Argument,
                    "local" => SegmentType.Local,
                    "static" => SegmentType.Static,
                    "constant" => SegmentType.Constant,
                    "this" => SegmentType.This,
                    "that" => SegmentType.That,
                    "pointer" => SegmentType.Pointer,
                    "temp" => SegmentType.Temp,
                    _ => throw new ParserException(
                        lineNumber,
                        line,
                        "expected 'push SEGMENT INDEX', where SEGMENT is in {argument, local, static, constant, this, that, pointer, temp}")
                };
                
                return new PushCommand(segment, index, line);
            }

            case "pop":
            {
                if (lineComponents.Length != 3)
                {
                    throw new ParserException(lineNumber, line, "expected 'pop SEGMENT INDEX'");
                }

                if (!uint.TryParse(lineComponents[2], out var index))
                {
                    throw new ParserException(lineNumber, line,
                        "expected 'pop SEGMENT INDEX', where INDEX is positive integer");
                }

                var segment = lineComponents[1] switch
                {
                    "argument" => SegmentType.Argument,
                    "local" => SegmentType.Local,
                    "static" => SegmentType.Static,
                    "this" => SegmentType.This,
                    "that" => SegmentType.That,
                    "pointer" => SegmentType.Pointer,
                    "temp" => SegmentType.Temp,
                    _ => throw new ParserException(
                        lineNumber,
                        line,
                        "expected 'pop SEGMENT INDEX', where SEGMENT is in {argument, local, static, this, that, pointer, temp}")
                };
                
                return new PopCommand(segment, index, line);
            }
            
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
            {
                if (lineComponents.Length != 2)
                {
                    throw new TranslationException(lineNumber, line, "expected 'label SYMBOL'");
                }

                var symbol = lineComponents[1];
                return new LabelCommand(symbol);
            }
            
            case "if-goto":
            {
                if (lineComponents.Length != 2)
                {
                    throw new TranslationException(lineNumber, line, "expected 'if-goto SYMBOL'");
                }
        
                var symbol = lineComponents[1];
                return new IfGotoCommand(symbol);
            }
                

            case "goto":
            {
                if (lineComponents.Length != 2)
                {
                    throw new TranslationException(lineNumber, line, "expected 'goto SYMBOL'");
                }
        
                var symbol = lineComponents[1];
                return new GotoCommand(symbol);
            }
            
            case "return":
                return new ReturnCommand();
            
            case "function":{
                if (lineComponents.Length != 3)
                {
                    throw new TranslationException(lineNumber, line, "expected 'function FUNCTION_NAME NUMBER_OF_LOCALS'");
                }
                
                if (!uint.TryParse(lineComponents[2], out var numLocals))
                {
                    throw new ParserException(
                        lineNumber, 
                        line,
                        "expected 'function FUNCTION_NAME NUMBER_OF_LOCALS', where NUMBER_OF_LOCALS is positive integer");
                }
                
                var functionName = lineComponents[1];
                return new FunctionDeclarationCommand(functionName, numLocals);
            }
                
            
            case "call":
                return new FunctionCallCommand(trimmedLine);

            default:
                throw new ParserException(
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