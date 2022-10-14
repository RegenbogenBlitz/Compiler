namespace JackCompiler
{
    internal class OutputFileInfo
    {
        internal OutputFileInfo(string fileName, string fileContent)
        {
            this.FileName = fileName;
            this.Extension = null;
            this.Content = fileContent;
        }

        internal OutputFileInfo(string fileName, string extension, string fileContent)
        {
            this.FileName = fileName;
            this.Extension = extension;
            this.Content = fileContent;
        }

        internal string FileName { get; private set; }
        internal string Extension { get; private set; }
        internal string Content { get; private set; }
    }
}
