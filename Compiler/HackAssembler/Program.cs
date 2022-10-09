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

var filteredAndTrimmedFileLines = 
    fileLines
    .Where(line => 
        !string.IsNullOrWhiteSpace(line) && 
        !line.StartsWith("//"))
    .Select(line=> line.Trim());

const int baseRamAddress = 16;

var output = new List<string>();
var labels = new Dictionary<string, int>();
var lineNumber = 0;
foreach (var line in filteredAndTrimmedFileLines)
{
    if (line.StartsWith("@"))
    {
        // A-instruction
        output.Add(line);
        lineNumber++;
    }
    else if (line.Contains('='))
    {
        // C-instruction
        output.Add(line);
        lineNumber++;
    }
    else if (line.StartsWith("("))
    {
        var label = line.TrimStart('(').TrimEnd(')');
        if (labels.ContainsKey(label))
        {
            await Console.Error.WriteLineAsync($"Label {label} has been used more than once.");
            return ;
        }

        labels.Add(label, lineNumber + 1);
    }
    else
    {
        await Console.Error.WriteLineAsync($"Instruction {line} not recognised.");
        return;
    }
}

var outputFileInfo = new FileInfo(Path.Join(assemblerDirectory.FullName, Path.GetFileNameWithoutExtension(path) + ".hack"));
File.WriteAllLines(outputFileInfo.FullName, output);