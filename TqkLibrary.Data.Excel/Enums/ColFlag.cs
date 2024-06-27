using System;

namespace TqkLibrary.Data.Excel.Enums
{
    [Flags]
    public enum ColFlag
    {
        None = 0,
        IsUpdateBack = 1 << 0,
        SkipReadLineIfCell_Empty = 1 << 1,
        SkipReadLineIfCell_NotEmpty = 1 << 2,
    }
}
