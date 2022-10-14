namespace JackCompiler
{
    internal class InputFileInfo
    {
        internal InputFileInfo(string fileName, string[] fileContent)
        {
            this.FileName = fileName;
            this.Content = fileContent;
        }

        internal string FileName { get; private set; }
        internal string[] Content { get; private set; }
    }
}
