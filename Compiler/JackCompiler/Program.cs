namespace JackCompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class Program
    {
        private const string InputFileExtension = ".jack";
        private const string OutputFileExtension = "vm";

        public static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                throw new ArgumentNullException("args", "Must supply file name or directory.");
            }

            string fileOrFolderPath = args[0];
            string folderParent;
            IEnumerable<InputFileInfo> inputFilesContent = 
                ReadInputFileContent(fileOrFolderPath, out folderParent);

            IEnumerable<OutputFileInfo> outputFileContent = Compiler.Compile(inputFilesContent);

            WriteOutputFileContent(
                folderParent,
                outputFileContent);
        }

        private static IEnumerable<InputFileInfo> ReadInputFileContent(
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
                    .Where(filepath => Path.GetExtension(filepath) == InputFileExtension);

                if (!filePaths.Any())
                {
                    throw new IOException(string.Format(
                        "No files of extension {0} found in folder {1}.",
                        InputFileExtension,
                        inputFullPath));
                }

                folderParent = inputFullPath;
            }
            else
            {
                if (Path.GetExtension(inputFullPath) != InputFileExtension)
                {
                    throw new ArgumentNullException(
                        inputFullPath,
                        "Must supply " + InputFileExtension + " file name.");
                }

                if (!File.Exists(inputFullPath))
                {
                    throw new IOException(inputFullPath + " not found.");
                }

                filePaths = new[] { inputFullPath };

                folderParent = Path.GetDirectoryName(inputFullPath);
            }

            return filePaths.Select(filepath => new InputFileInfo(
                Path.GetFileNameWithoutExtension(filepath),
                File.ReadAllLines(filepath)));
        }

        private static bool IsDirectory(string inputFullPath)
        {
            return (File.GetAttributes(inputFullPath) & FileAttributes.Directory) == FileAttributes.Directory;
        }

        private static void WriteOutputFileContent(
            string outputDirectory,
            IEnumerable<OutputFileInfo> outputFileInfos)
        {
            foreach (OutputFileInfo outputFileInfo in outputFileInfos)
            {
                string extension = outputFileInfo.Extension ?? OutputFileExtension;
                string outputFilePath = Path.Combine(outputDirectory, outputFileInfo.FileName + "." + extension);
                File.WriteAllText(outputFilePath, outputFileInfo.Content);
            }
        }
    }
}
