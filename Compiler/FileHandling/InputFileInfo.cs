namespace FileHandling;

public class InputFileInfo
{
    public InputFileInfo(string fileName, string[] fileContent)
    {
        this.FileName = fileName;
        this.Content = fileContent;
    }

    public string FileName { get; }
    public string[] Content { get; }
}
