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

var output = new List<string>();
var labels = new Dictionary<string, int>();
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
        output.Add(line);
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