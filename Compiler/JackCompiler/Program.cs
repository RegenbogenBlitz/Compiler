using FileHandling;

namespace JackCompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class Program
    {
        private const string InputFileExtension = ".jack";

        public static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                throw new ArgumentNullException("args", "Must supply file name or directory.");
            }

            string fileOrFolderPath = args[0];
            var inputFileHandler = new InputFileHandler(InputFileExtension);
            var inputFilesContent = 
                inputFileHandler.ReadInputFileContent(fileOrFolderPath, out var folderParent);

            var outputFileContent = Compiler.Compile(inputFilesContent);

            OutputFileHandler.WriteOutputFileContent(
                folderParent,
                outputFileContent);
        }
    }
}
