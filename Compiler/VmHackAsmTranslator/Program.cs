using FileHandling;
using VmHackAsmTranslator.AsmWriter;
using VmHackAsmTranslator.Parsing;

const string inputFileExtension = ".vm";

if (args == null || args.Length == 0)
{
    throw new ArgumentNullException("args", "Must supply file name or directory.");
}

string fileOrFolderPath = args[0];
var inputFileHandler = new InputFileHandler(inputFileExtension);
var inputFilesContent = 
    inputFileHandler.ReadInputFileContent(fileOrFolderPath, out var folderParent);

try
{
    var directory = new DirectoryInfo(folderParent);
    var vmCode = new VmCodeParser().Parse(inputFilesContent);
    var outputFileInfo = AsmWriter.Write(directory.Name, vmCode, true);

    OutputFileHandler.WriteOutputFileContent(
        folderParent,
        new[] { outputFileInfo });
}
catch (ParserException ex)
{
    Console.Error.WriteLine(ex.Message);
}