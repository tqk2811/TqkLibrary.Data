using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TqkLibrary.Data.Json
{
    public class SaveJsonDataAutoBackup<T> : SaveJsonData<T>
        where T : class
    {
        public string BackupDir { get; }
        public DateTime LastTimeBackup { get; private set; }
        public TimeSpan BackupInterval { get; set; } = TimeSpan.FromDays(1);//backup perday

        public SaveJsonDataAutoBackup(string SavePath, string backupDir, JsonSerializerSettings? jsonSerializerSettings = null)
            : base(SavePath, jsonSerializerSettings)
        {
            BackupDir = backupDir;
            Init();
        }

        public SaveJsonDataAutoBackup(string SavePath, string backupDir, T defaultData, JsonSerializerSettings? jsonSerializerSettings = null)
            : base(SavePath, defaultData, jsonSerializerSettings)
        {
            BackupDir = backupDir;
            Init();
        }
        const string _backup = "Backup ";
        void Init()
        {
            Directory.CreateDirectory(BackupDir);
            this.OnSaved += SaveJsonDataAutoBackup_OnSaved;

            //check lastTime
            foreach (string file in GetBackupFiles())
            {
                FileInfo fileInfo = new FileInfo(file);
                if (
                    fileInfo.Name.Length > _backup.Length + fileInfo.Extension.Length &&
                    DateTime.TryParseExact(
                        fileInfo.Name.Substring(_backup.Length, fileInfo.Name.Length - fileInfo.Extension.Length),
                        "yyyy-MM-dd HH-mm-ss.ffffff",
                        CultureInfo.CurrentCulture,
                        DateTimeStyles.None,
                        out DateTime result
                    ))
                {
                    LastTimeBackup = result;
                    break;
                }
            }
        }



        IEnumerable<string> GetBackupFiles()
        {
            return Directory.GetFiles(BackupDir, $"{_backup}*.json")
                .OrderByDescending(x => x);
        }

        private void SaveJsonDataAutoBackup_OnSaved(T obj)
        {
            DateTime current = DateTime.Now;
            if (LastTimeBackup.Add(BackupInterval) < current)
            {
                try
                {
                    File.WriteAllText(
                        Path.Combine(BackupDir, $"{current:yyyy-MM-dd HH-mm-ss.ffffff}.json"),
                        JsonConvert.SerializeObject(Data, Formatting.Indented, _jsonSerializerSettings)
                        );
                    LastTimeBackup = current;
                }
                catch { }
            }
        }


        protected override void Load(string filePath)
        {
            try
            {
                base.Load(filePath);
            }
            catch
            {
                bool isSuccess = false;
                foreach (var file in GetBackupFiles())
                {
                    try
                    {
                        base.Load(filePath);
                        isSuccess = true;
                        break;
                    }
                    catch { }
                }

                if (!isSuccess)
                    throw;
            }
        }
    }
}
