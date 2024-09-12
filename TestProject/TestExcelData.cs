
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

            [ColRange(ColFlag.SkipReadLineIfCell_Empty, "A", "B", "C")]
            public List<string>? Ids { get; set; }

            [ColRange(ColFlag.None, "A", "B", "C")]
            public IEnumerable<string>? Ids2 { get; set; }

            [ColRange(ColFlag.SkipReadLineIfCell_Empty, "A", "B", "C")]
            public List<string> Ids3 { get; set; } = new();

            [ColRange(ColFlag.SkipReadLineIfCell_Empty, "A", "B", "C")]
            public IReadOnlyCollection<string>? Ids4 { get; set; }

            [ColRange(ColFlag.SkipReadLineIfCell_Empty, "A", "B", "C", "D")]
            public Dictionary<string, string>? DictIds { get; set; }

            [ColRange('A', 'Z')]
            public IDictionary<string, string>? DictIds2 { get; set; }

            [ColRange('A', 'Z')]
            public IReadOnlyDictionary<string, string>? DictIds3 { get; set; }

        }
        readonly ExcelServiceReadLine excelServiceReadLine = new ExcelServiceReadLine("ExcelTest.xlsx");

        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public async Task Test1()
        {
            var datas = await excelServiceReadLine.GetDatasAsync<TestData>();
            Assert.IsTrue(datas.All(x =>
            {
                return
                    x.Ids is not null && x.Ids.Any() &&
                    x.Ids2 is not null && x.Ids2.Any() &&
                    x.Ids3 is not null && x.Ids3.Any() &&
                    x.Ids4 is not null && x.Ids4.Any() &&
                    x.DictIds is not null && x.DictIds.Any() &&
                    x.DictIds2 is not null && x.DictIds2.Any() &&
                    x.DictIds3 is not null && x.DictIds3.Any()
                    ;
            }));
        }
    }
}