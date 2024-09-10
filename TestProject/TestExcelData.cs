
using TqkLibrary.Data.Excel;
using TqkLibrary.Data.Excel.Attributes;
using TqkLibrary.Data.Excel.Enums;
using TqkLibrary.Data.Json;

namespace TestProject
{
    public class TestExcelData
    {
        [SheetIndex(0)]
        class TestData : BaseExcelService.BaseData
        {
            [Col("A")]
            public string Id { get; set; } = "";

            [ColRange(ColFlag.SkipReadLineIfCell_NotEmpty, "A", "B", "C")]
            public List<string>? Ids { get; set; }

            [ColRange(ColFlag.SkipReadLineIfCell_NotEmpty, "A", "B", "C", "D")]
            public Dictionary<string, string>? DictIds { get; set; }

        }
        readonly ExcelServiceReadLine excelServiceReadLine = new ExcelServiceReadLine("ExcelTest.xlsx");

        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public async Task Test1()
        {
            await excelServiceReadLine.GetDatasAsync<TestData>();
        }
    }
}