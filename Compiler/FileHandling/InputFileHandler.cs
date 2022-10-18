namespace FileHandling;

public class InputFileHandler
{
    private readonly string _fileExtension;

    public InputFileHandler(string fileExtension)
    {
        _fileExtension = fileExtension;
    }
    
    public IEnumerable<InputFileInfo> ReadInputFileContent(
        string inputFullPath,
        out string folderParent)
    {
        IEnumerable<string> filePaths;

        if (IsDirectory(inputFullPath))
        {
            if (!Directory.Exists(inputFullPath))
            {
                throw new IOException(inputFullPath + " not found.");
            }

            filePaths = 
                Directory.GetFiles(inputFullPath)
                .Where(filepath => Path.GetExtension(filepath) == _fileExtension)
                .ToArray();

            if (!filePaths.Any())
            {
                throw new IOException(string.Format(
                    "No files of extension {0} found in folder {1}.",
                    _fileExtension,
                    inputFullPath));
            }

            folderParent = inputFullPath;
        }
        else
        {
            var file = new FileInfo(inputFullPath);
            if (file.Extension != _fileExtension)
            {
                throw new ArgumentNullException(
                    inputFullPath,
                    "Must supply " + _fileExtension + " file name.");
            }

            if (!file.Exists)
            {
                throw new IOException(inputFullPath + " not found.");
            }

            filePaths = new[] { inputFullPath };

            var directory = file.Directory;
            if (directory == null)
            {
                throw new IOException($"File '{inputFullPath}' existed but directory now does not.");
            }
            folderParent = directory.FullName;
        }

        return filePaths.Select(filepath => new InputFileInfo(
            Path.GetFileNameWithoutExtension(filepath),
            File.ReadAllLines(filepath)));
    }   
    
    private static bool IsDirectory(string inputFullPath)
    {
        return (File.GetAttributes(inputFullPath) & FileAttributes.Directory) == FileAttributes.Directory;
    }
}


