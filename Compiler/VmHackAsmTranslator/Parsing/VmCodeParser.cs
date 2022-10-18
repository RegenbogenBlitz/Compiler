using FileHandling;

namespace VmHackAsmTranslator.Parsing;

public static class VmCodeParser
{
    public static VmCode Parse(IEnumerable<InputFileInfo> inputFiles)
    {
        var code =
            inputFiles
                .SelectMany(f => f.Content)
                .Select(line => new FunctionCallCommand(line))
                .ToArray();
        return new VmCode(code);
    }
}