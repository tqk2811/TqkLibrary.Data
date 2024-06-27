using TqkLibrary.Data.Json;

namespace TestProject
{
    public class TestSaveJsonDataAutoBackup
    {
        class TestData
        {
            public int Id { get; set; }
        }

        SaveJsonDataAutoBackup<TestData> saveJsonDataAutoBackup;

        [SetUp]
        public void Setup()
        {
            saveJsonDataAutoBackup = new SaveJsonDataAutoBackup<TestData>(
               Path.Combine(Directory.GetCurrentDirectory(), $"{nameof(TestSaveJsonDataAutoBackup)}.json"),
               Path.Combine(Directory.GetCurrentDirectory(), $"{nameof(TestSaveJsonDataAutoBackup)}Backup")
           );
            saveJsonDataAutoBackup.BackupInterval = TimeSpan.FromSeconds(0.5);
        }

        [Test]
        public async Task Test1()
        {
            saveJsonDataAutoBackup.TriggerSave();
            await Task.Delay(1000);
            for(int i = 0; i < 10; i++)
            {
                saveJsonDataAutoBackup.Data.Id++;
                saveJsonDataAutoBackup.TriggerSave();
                await Task.Delay(1000);
            }
        }
    }
}