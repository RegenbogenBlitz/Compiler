namespace JackCompiler.PreMeaningProcessing.Syntactical
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class BookmarkedArray<T>
    {
        private readonly T[] array;
        private readonly List<int> bookmarks;

        private int bookmark;
        
        internal BookmarkedArray(T[] array)
        {
            this.array = array;

            this.bookmark = 0;
            this.bookmarks = new List<int>(0);
        }

        internal bool IsAtEnd
        {
            get
            {
                return this.bookmark == this.array.Length;
            }
        }

        internal T GetValueIncreaseBookmark()
        {
            if (this.IsAtEnd)
            {
                throw new InvalidOperationException("No more values.");
            }

            var value = this.array[this.bookmark];
            this.bookmark++;
            return value;
        }

        internal void RevertAndRemoveBookmark()
        {
            var count = this.bookmarks.Count;
            if (count == 1)
            {
                throw new InvalidOperationException("Cannot remove last bookmark");
            }

            this.bookmark = this.bookmarks.Last();
            this.bookmarks.RemoveAt(count - 1);
        }

        internal void AddBookmark()
        {
            this.bookmarks.Add(this.bookmark);
        }
    }
}