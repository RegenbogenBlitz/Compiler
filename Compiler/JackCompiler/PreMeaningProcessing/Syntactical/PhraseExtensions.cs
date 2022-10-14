namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using System.Collections.Generic;
    using System.Linq;

    internal static class PhraseExtensions
    {
        internal static IPhrase Child(this IBranchPhrase phrase, int index)
        {
            return phrase.Children.Skip(index).Take(1).Single();
        }
        
        internal static ILeafPhrase LeafChild(this IBranchPhrase phrase, string categoryName)
        {
            return phrase.Child(categoryName) as ILeafPhrase;
        }

        internal static IBranchPhrase BranchChild(this IBranchPhrase phrase, string categoryName)
        {
            return phrase.Child(categoryName) as IBranchPhrase;
        }

        private static IPhrase Child(this IBranchPhrase phrase, string categoryName)
        {
            return phrase.Children.Single(p => p.CategoryName == categoryName);
        }

        internal static ILeafPhrase TryLeafChild(this IBranchPhrase phrase, string categoryName)
        {
            return phrase.Children.SingleOrDefault(p => p.CategoryName == categoryName) as ILeafPhrase;
        }

        internal static IBranchPhrase TryBranchChild(this IBranchPhrase phrase, string categoryName)
        {
            return phrase.Children.SingleOrDefault(p => p.CategoryName == categoryName) as IBranchPhrase;
        }

        internal static IEnumerable<ILeafPhrase> LeafChildren(this IBranchPhrase phrase, string categoryName)
        {
            return phrase
                .Children(categoryName)
                .Select(p => p as ILeafPhrase);
        }

        internal static IEnumerable<IBranchPhrase> BranchChildren(this IBranchPhrase phrase, string categoryName)
        {
            return phrase
                .Children(categoryName)
                .Select(p => p as IBranchPhrase);
        }

        internal static IEnumerable<IBranchPhrase> BranchChildren(this IBranchPhrase phrase)
        {
            return phrase.Children.Select(p => p as IBranchPhrase);
        }

        private static IEnumerable<IPhrase> Children(this IBranchPhrase phrase, string categoryName)
        {
            return phrase
                .Children
                .Where(p => p.CategoryName == categoryName);
        }
    }
}
