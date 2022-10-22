using FileHandling;
using VmHackAsmTranslator.AsmWriter;
using VmHackAsmTranslator.Parsing;

const string inputFileExtension = ".vm";

if (args == null)
{
    throw new ArgumentNullException("args", "Args missing");
}

var options = args.Where(a => a.StartsWith("-")).ToArray();
var writeComments = options.Any(o=> o == "-c");

var arguments = args.Where(a => !a.StartsWith("-")).ToArray();
if (arguments.Length == 0)
{
    throw new ArgumentException("Must supply file name or directory.", nameof(args));
}

string fileOrFolderPath = arguments[0];
var inputFileHandler = new InputFileHandler(inputFileExtension);
var inputFilesContent = 
    inputFileHandler.ReadInputFileContent(fileOrFolderPath, out var folderParent);

try
{
    var directory = new DirectoryInfo(folderParent);
    var vmCode = new VmCodeParser().Parse(inputFilesContent);
    var outputFileInfo = AsmWriter.Write(directory.Name, vmCode, writeComments);

    OutputFileHandler.WriteOutputFileContent(
        folderParent,
        new[] { outputFileInfo });
}
catch (ParserException ex)
{
    Console.Error.WriteLine(ex.Message);
}