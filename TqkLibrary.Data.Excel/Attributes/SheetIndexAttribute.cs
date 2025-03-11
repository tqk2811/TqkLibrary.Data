using OfficeOpenXml;
using System;

namespace TqkLibrary.Data.Excel.Attributes
{
    public class SheetIndexAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="startRow">Note: startRow + <see cref="ExcelWorksheets.Rows.StartRow"/></param>
        public SheetIndexAttribute(int index, int startRow = 0)
        {
            this.Index = index;
            this.StartRow = startRow;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="startRow">Note: startRow + <see cref="ExcelWorksheets.Rows.StartRow"/></param>
        /// <exception cref="ArgumentNullException"></exception>
        public SheetIndexAttribute(string name, int startRow = 0)
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
