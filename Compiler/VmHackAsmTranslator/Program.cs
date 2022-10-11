var path = args[0];

if (Path.GetExtension(path) != ".vm")
{
    await Console.Error.WriteLineAsync($"File path '{path}' does not have the .vm extension.");
    return ;
}

var fileInfo = new FileInfo(path);
if (!fileInfo.Exists)
{
    await Console.Error.WriteLineAsync($"File '{path}' does not exist.");
    return ;
}

var vmFilesDirectory = fileInfo.Directory;
if (vmFilesDirectory == null)
{
    throw new IOException($"File '{path}' existed but directory now does not.");
}

var fileLines = await File.ReadAllLinesAsync(path);


var outputFileInfo = new FileInfo(Path.Join(vmFilesDirectory.FullName, Path.GetFileNameWithoutExtension(path) + ".asm"));
File.WriteAllLines(outputFileInfo.FullName, fileLines);

Console.Write(outputFileInfo.FullName);