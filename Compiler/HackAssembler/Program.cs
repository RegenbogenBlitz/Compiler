var path = args[0];

if (Path.GetExtension(path) != ".asm")
{
    await Console.Error.WriteLineAsync($"File path '{path}' does not have the .asm extension.");
    return ;
}

var fileInfo = new FileInfo(path);
if (!fileInfo.Exists)
{
    await Console.Error.WriteLineAsync($"File '{path}' does not exist.");
    return ;
}

var assemblerDirectory = fileInfo.Directory;
if (assemblerDirectory == null)
{
    throw new IOException($"File '{path}' existed but directory now does not.");
}

var fileLines = await File.ReadAllLinesAsync(path);

const int baseRamAddress = 16;

async Task WriteErrorAsync(int inputLineNumber, string originalLine, string message)
{
    await Console.Error.WriteLineAsync($"Line {inputLineNumber}: '{originalLine}': {message}");
}

var labels = new Dictionary<string, int>();
var output = new List<string>();
var outputLineNumber = 0;
var inputLineNumber = 0;
foreach (var line in fileLines)
{
    var trimmedLine = line.Trim();
    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//"))
    {
        inputLineNumber++;
        continue;
    }

    if (trimmedLine.StartsWith("@"))
    {
        // A-instruction
        var value = trimmedLine.TrimStart('@');
        
        if (uint.TryParse(value, out var uintValue))
        {
            if (uintValue > 32767)
            {
                await WriteErrorAsync(inputLineNumber, line, "Value is greater than 32767");
                return;
            }
            
            var binaryValue = Convert.ToString(uintValue, 2).PadLeft(16, '0');
            output.Add(binaryValue);
        }
        else if (decimal.TryParse(value, out var decimalValue))
        {
            await WriteErrorAsync(inputLineNumber, line, "Numerical value is not an integer between 0 and 32767 inc.");
            return;
        }
        else
        {
            throw new NotImplementedException();
        }

        outputLineNumber++;
        inputLineNumber++;
    }
    else if (trimmedLine.Contains('='))
    {
        // C-instruction
        output.Add(line);
        outputLineNumber++;
        inputLineNumber++;
    }
    else if (trimmedLine.StartsWith("("))
    {
        var label = trimmedLine.TrimStart('(').TrimEnd(')');
        if (labels.ContainsKey(label))
        {
            await WriteErrorAsync(inputLineNumber, line, "Label has been used more than once.");
            return ;
        }

        labels.Add(label, outputLineNumber + 1);
        inputLineNumber++;
    }
    else
    {
        await WriteErrorAsync(inputLineNumber, line, "Instruction not recognised.");
        return;
    }
}

var outputFileInfo = new FileInfo(Path.Join(assemblerDirectory.FullName, Path.GetFileNameWithoutExtension(path) + ".hack"));
File.WriteAllLines(outputFileInfo.FullName, output);

Console.Write(outputFileInfo.FullName);