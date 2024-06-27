using System;
using TqkLibrary.Data.Excel.Enums;

namespace TqkLibrary.Data.Excel.Attributes
{
    public class ColAttribute : Attribute
    {
        public ColAttribute(string col, ColFlag colFlag = ColFlag.None)
        {
            if (string.IsNullOrWhiteSpace(col)) throw new ArgumentNullException(nameof(col));
            this.Col = col;
            this.Flag = colFlag;
        }

        public string Col { get; }
        public ColFlag Flag { get; }
    }
}
