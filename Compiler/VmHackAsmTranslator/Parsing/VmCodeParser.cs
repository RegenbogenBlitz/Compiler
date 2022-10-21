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

        var code =
            inputFiles
                .Select(ParseFile)
                .SelectMany(c => c)
                .Select(lc => lc.command)
                .ToArray();
        
        return new VmCode(code);
    }

    private IEnumerable<(LineInfo lineInfo, ICommand command)> ParseFile(InputFileInfo inputFile)
    {
        var lineNumber = (uint)0;
        var commands = new List<(LineInfo lineInfo, ICommand command)>();

        foreach (var lineContent in inputFile.Content)
        {
            lineNumber++;
            var lineInfo = new LineInfo(inputFile.FileName, lineNumber, lineContent);
            commands.Add((lineInfo, ParseLine(lineInfo)));
        }

        return commands;
    }
    
    private ICommand ParseLine(LineInfo lineInfo)
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
                
                return new PushCommand(segment, index);
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
                
                return new PopCommand(segment, index);
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
                return new LabelCommand(symbol);
            }
            
            case "if-goto":
            {
                if (lineComponents.Length != 2)
                {
                    throw new ParserException(lineInfo, "expected 'if-goto SYMBOL'");
                }
        
                var symbol = lineComponents[1];
                return new IfGotoCommand(symbol);
            }
                

            case "goto":
            {
                if (lineComponents.Length != 2)
                {
                    throw new ParserException(lineInfo, "expected 'goto SYMBOL'");
                }
        
                var symbol = lineComponents[1];
                return new GotoCommand(symbol);
            }
            
            case "return":
                return new ReturnCommand();
            
            case "function":{
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
                
                var functionName = lineComponents[1];
                return new FunctionDeclarationCommand(functionName, numLocals);
            }
                
            
            case "call":
                return new FunctionCallCommand(trimmedLine);

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