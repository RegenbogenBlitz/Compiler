var path = args[0];

if (Path.GetExtension(path) != ".asm")
{
    await Console.Error.WriteLineAsync($"File path '{path}' does not have the .asm extension.");
    return ;
}
else if (!File.Exists(path))
{
    await Console.Error.WriteLineAsync($"File '{path}' does not exist.");
    return ;
}

var fileLines = await File.ReadAllLinesAsync(path);

foreach (var fileLine in fileLines)
{
    Console.WriteLine(fileLine);
}