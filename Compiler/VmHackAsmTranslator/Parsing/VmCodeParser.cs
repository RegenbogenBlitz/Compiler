using FileHandling;

namespace VmHackAsmTranslator.Parsing;

public class VmCodeParser
{
    // ReSharper disable once RedundantDefaultMemberInitializer
    private bool _hasParsed = false;
    
    public VmCode Parse(IEnumerable<InputFileInfo> inputFiles)
    {
        if (_hasParsed)
        {
            throw new InvalidOperationException("This Parser has already been run");
        }
        _hasParsed = true;

        var inputFilesArray = inputFiles.ToArray();
        var osFiles = new[]
        {
            "Array",
            "KeyBoard",
            "Math",
            "Memory",
            "Output",
            "Screen",
            "String",
            "Sys"
        };
        
        var orderedFiles =
            inputFilesArray.Where(f => !osFiles.Contains(f.FileName))
                .Concat(inputFilesArray.Where(f => osFiles.Contains(f.FileName)));
            
        var linesAndParsedCommands =
            orderedFiles
                .Select(ParseFile)
                .SelectMany(c => c)
                .ToArray();

        if (!linesAndParsedCommands.Any(lc =>
            {
                if (!(lc.command is FunctionDeclarationCommand functionDeclarationCommand))
                {
                    return false;
                }

                return functionDeclarationCommand.FunctionName == "sys.init";
            }))
        {
            throw new ParserException("No function sys.init found.");
        }

        var declaredFunctionNames = new Dictionary<string, LineInfo>();
        foreach (var lineAndFunctionDeclaration in linesAndParsedCommands.Where(lc=> lc.command is FunctionDeclarationCommand))
        {
            var functionDeclaration = (FunctionDeclarationCommand)lineAndFunctionDeclaration.command;
            if (declaredFunctionNames.ContainsKey(functionDeclaration.FunctionName))
            {
                var otherLineInfo = declaredFunctionNames[functionDeclaration.FunctionName];
                throw new ParserException(
                    lineAndFunctionDeclaration.lineInfo,
                    $"Function name {functionDeclaration.FunctionName} already declared. " +
                    $"File {otherLineInfo.FileName} Line Number {otherLineInfo.LineNumber}");
            }
            declaredFunctionNames.Add(functionDeclaration.FunctionName, lineAndFunctionDeclaration.lineInfo);
        }

        foreach (var lineAndFunctionCall in linesAndParsedCommands.Where(lc=> lc.command is FunctionCallCommand))
        {
            var functionCall = (FunctionCallCommand)lineAndFunctionCall.command;
            if (!declaredFunctionNames.ContainsKey(functionCall.FunctionName))
            {
                throw new ParserException(
                    lineAndFunctionCall.lineInfo,
                    $"Function name {functionCall.FunctionName} not declared.");
            }
        }
        
        return new VmCode(
            linesAndParsedCommands
                .Select(lc => lc.command)
                .ToArray());
    }

    private IEnumerable<(LineInfo lineInfo, ICommand command)> ParseFile(InputFileInfo inputFile)
    {
        var lineNumber = (uint)0;
        var commands = new List<(LineInfo lineInfo, ICommand command)>();

        var functionName = string.Empty;
        foreach (var lineContent in inputFile.Content)
        {
            lineNumber++;
            var lineInfo = new LineInfo(inputFile.FileName, lineNumber, lineContent);
            commands.Add((lineInfo, ParseLine(lineInfo, ref functionName)));
        }

        return commands;
    }
    
    private ICommand ParseLine(LineInfo lineInfo, ref string functionName)
    {
        var trimmedLine = TrimLine(lineInfo.OriginalLine);
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
                    throw new ParserException(lineInfo, "expected 'push SEGMENT INDEX'");
                }
        
                if (!uint.TryParse(lineComponents[2], out var index))
                {
                    throw new ParserException(lineInfo, "expected 'push SEGMENT INDEX', where INDEX is positive integer");
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
                    _ => throw new ParserException(lineInfo, "expected 'push SEGMENT INDEX', where SEGMENT is in {argument, local, static, constant, this, that, pointer, temp}")
                };
                
                return new PushCommand(lineInfo.FileName, segment, index);
            }

            case "pop":
            {
                if (lineComponents.Length != 3)
                {
                    throw new ParserException(lineInfo, "expected 'pop SEGMENT INDEX'");
                }

                if (!uint.TryParse(lineComponents[2], out var index))
                {
                    throw new ParserException(lineInfo, "expected 'pop SEGMENT INDEX', where INDEX is positive integer");
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
                    _ => throw new ParserException(lineInfo, "expected 'pop SEGMENT INDEX', where SEGMENT is in {argument, local, static, this, that, pointer, temp}")
                };
                
                return new PopCommand(lineInfo.FileName, segment, index);
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
            {
                if (lineComponents.Length != 1)
                {
                    throw new ParserException(lineInfo, $"expected '{lineComponents[0]}'");
                }

                var arithmeticCommandType = lineComponents[0] switch
                {
                    "add" => ArithmeticCommandType.Add,
                    "sub" => ArithmeticCommandType.Sub,
                    "neg" => ArithmeticCommandType.Neg,
                    "and" => ArithmeticCommandType.And,
                    "or" => ArithmeticCommandType.Or,
                    "not" => ArithmeticCommandType.Not,
                    "eq" => ArithmeticCommandType.Eq,
                    "lt" => ArithmeticCommandType.Lt,
                    "gt" => ArithmeticCommandType.Gt,
                    _ => throw new InvalidOperationException("Should not be reachable")
                };
                return new ArithmeticCommand(arithmeticCommandType);
            }
            case "label":
            {
                if (lineComponents.Length != 2)
                {
                    throw new ParserException(lineInfo, "expected 'label SYMBOL'");
                }

                var symbol = lineComponents[1];
                return new LabelCommand(functionName, symbol);
            }
            
            case "if-goto":
            {
                if (lineComponents.Length != 2)
                {
                    throw new ParserException(lineInfo, "expected 'if-goto SYMBOL'");
                }
        
                var symbol = lineComponents[1];
                return new IfGotoCommand(functionName, symbol);
            }
                

            case "goto":
            {
                if (lineComponents.Length != 2)
                {
                    throw new ParserException(lineInfo, "expected 'goto SYMBOL'");
                }
        
                var symbol = lineComponents[1];
                return new GotoCommand(functionName, symbol);
            }
            
            case "return":
                return new ReturnCommand();
            
            case "function":
            {
                if (lineComponents.Length != 3)
                {
                    throw new ParserException(lineInfo, "expected 'function FUNCTION_NAME NUMBER_OF_LOCALS'");
                }
                
                if (!uint.TryParse(lineComponents[2], out var numLocals))
                {
                    throw new ParserException(
                        lineInfo,
                        "expected 'function FUNCTION_NAME NUMBER_OF_LOCALS', where NUMBER_OF_LOCALS is positive integer");
                }
                
                functionName = lineComponents[1].ToLower();
                return new FunctionDeclarationCommand(functionName, numLocals);
            }
                
            
            case "call":
            {
                if (lineComponents.Length != 3)
                {
                    throw new ParserException(lineInfo, "expected 'call FUNCTION_NAME NUMBER_OF_ARGUMENTS'");
                }
                
                if (!uint.TryParse(lineComponents[2], out var numArguments))
                {
                    throw new ParserException(
                        lineInfo,
                        "expected 'call FUNCTION_NAME NUMBER_OF_ARGUMENTS', where NUMBER_OF_ARGUMENTS is positive integer");
                }
                
                var calledFunctionName = lineComponents[1].ToLower();
                return new FunctionCallCommand(calledFunctionName, numArguments);
            }

            default:
                throw new ParserException(lineInfo,
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