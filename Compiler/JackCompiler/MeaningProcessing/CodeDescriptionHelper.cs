namespace JackCompiler.MeaningProcessing
{
    internal static class CodeDescriptionHelper
    {
        internal static string AddTab(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "\t" + text;
            }
            else
            {
                return "\t" + string.Join("\n\t", text.Split('\n'));
            }
        }
    }
}
