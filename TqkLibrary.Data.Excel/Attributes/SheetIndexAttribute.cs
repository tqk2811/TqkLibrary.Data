using OfficeOpenXml;
using System;

namespace TqkLibrary.Data.Excel.Attributes
{
    public class SheetIndexAttribute : Attribute
    {
        public SheetIndexAttribute(int index, int startRow = 1)
        {
            this.Index = index;
            this.StartRow = startRow;
        }
        public SheetIndexAttribute(string name, int startRow = 1)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            this.Name = name;
            this.StartRow = startRow;
        }

        public int? Index { get; }
        public string? Name { get; }
        public int StartRow { get; }

        public override string ToString()
        {
            return Index.HasValue ? Index.Value.ToString() : Name!;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="worksheets"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public ExcelWorksheet GetSheet(ExcelWorksheets worksheets)
        {
            ExcelWorksheet? excelWorksheet = null;
            if (!string.IsNullOrWhiteSpace(Name))
            {
                excelWorksheet = worksheets[Name];
            }
            else
            {
                excelWorksheet = worksheets[Index!.Value];//IndexOutOfRangeException
            }
            if (excelWorksheet is null)
                throw new InvalidOperationException($"Sheet '{this.ToString()}' not found");

            return excelWorksheet;
        }
    }
}
