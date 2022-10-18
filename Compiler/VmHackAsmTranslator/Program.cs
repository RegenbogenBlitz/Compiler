using FileHandling;
using VmHackAsmTranslator;

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
    var outputFileInfo = VmTranslator.Translate(directory.Name, inputFilesContent);

    OutputFileHandler.WriteOutputFileContent(
        folderParent,
        new[] { outputFileInfo });
}
catch (TranslationException ex)
{
    Console.Error.WriteLine(ex.Message);
}