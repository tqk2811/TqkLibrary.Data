using System;
using System.Collections.Generic;
using System.Text;
using TqkLibrary.Data.Excel.Attributes;

namespace TqkLibrary.Data.Excel
{
    public class ExcelReadOption
    {
        public bool IsReadAll { get; set; } = false;
        public bool StopAtEmptyLine { get; set; } = false;
        public SheetIndexAttribute? ForceSheetIndex { get; set; } = null;
    }
}
