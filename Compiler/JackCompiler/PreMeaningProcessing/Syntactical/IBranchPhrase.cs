namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using System.Collections.Generic;

    internal interface IBranchPhrase : IPhrase
    {
        IEnumerable<IPhrase> Children { get; }
    }
}
