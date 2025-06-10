using System;
using TqkLibrary.Data.Excel.Enums;

namespace TqkLibrary.Data.Excel.Attributes
{
    public class CellAttribute : Attribute
    {
        public CellAttribute(string cell, ColFlag colFlag = ColFlag.None)
        {
            if (string.IsNullOrWhiteSpace(cell)) throw new ArgumentNullException(nameof(cell));
            this.Cell = cell;
            this.Flag = colFlag;
        }
        public string Cell { get; }
        public ColFlag Flag { get; }
    }
}
