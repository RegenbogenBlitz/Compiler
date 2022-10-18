namespace FileHandling;

public static class OutputFileHandler
{
    public static void WriteOutputFileContent(
        string outputDirectory,
        IEnumerable<OutputFileInfo> outputFileInfos)
    {
        foreach (var outputFileInfo in outputFileInfos)
        {
            string outputFilePath = Path.Combine(outputDirectory, outputFileInfo.FileName + "." + outputFileInfo.Extension);
            File.WriteAllText(outputFilePath, outputFileInfo.Content);
        }
    }
}