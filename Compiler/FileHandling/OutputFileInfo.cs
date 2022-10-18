namespace FileHandling;

public class OutputFileInfo
{
    public OutputFileInfo(string fileName, string extension, string fileContent)
    {
        FileName = fileName;
        Extension = extension;
        Content = fileContent;
    }

    public string FileName { get; }
    public string Extension { get; }
    public string Content { get; }
}