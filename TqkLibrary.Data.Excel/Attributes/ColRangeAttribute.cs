using System;
using System.Collections.Generic;
using System.Linq;
using TqkLibrary.Data.Excel.Enums;

namespace TqkLibrary.Data.Excel.Attributes
{
    /// <summary>
    /// for IDictionary&lt;string, string&gt; or ICollection&lt;string&gt; only 
    /// </summary>
    public class ColRangeAttribute : Attribute
    {
        public ColRangeAttribute(ColFlag colFlag = ColFlag.None, params string[] cols)
        {
            if (cols is null || cols.Length == 0) throw new ArgumentNullException(nameof(cols));
            if (cols.Any(x => string.IsNullOrWhiteSpace(x))) throw new ArgumentNullException($"An item in {nameof(cols)} IsNullOrWhiteSpace");
            Cols = cols.ToList();
            this.Flag = colFlag;
        }

        public IEnumerable<string> Cols { get; }
        public ColFlag Flag { get; }
    }
}
