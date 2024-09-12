using Nito.AsyncEx;
using OfficeOpenXml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TqkLibrary.Data.Excel.Attributes;
using TqkLibrary.Data.Excel.Enums;

namespace TqkLibrary.Data.Excel
{
    public abstract partial class BaseExcelService
    {
        static BaseExcelService()
        {
            ExcelPackage.LicenseContext = LicenseContext.Commercial;
        }

        protected readonly AsyncLock _asyncLock = new AsyncLock();
        protected readonly string _filePath;
        public bool RunInLongRunningTask { get; set; } = true;
        public BaseExcelService(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);
            this._filePath = filePath;
        }

        public virtual Task ResetAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        protected virtual Task _RunInTask(Action action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            if (RunInLongRunningTask)
            {
                return Task.Factory.StartNew(action, TaskCreationOptions.LongRunning);
            }
            else
            {
                action.Invoke();
                return Task.CompletedTask;
            }
        }
        protected virtual Task<T> _RunInTask<T>(Func<T> func)
        {
            if (func is null) throw new ArgumentNullException(nameof(func));
            if (RunInLongRunningTask)
            {
                return Task.Factory.StartNew(func, TaskCreationOptions.LongRunning);
            }
            else
            {
                return Task.FromResult<T>(func.Invoke());
            }
        }


        public virtual Task SaveDataAsync<T>(T data, CancellationToken cancellationToken = default) where T : BaseData, new()
            => SaveDatasAsync<T>(Enumerable.Repeat(data, 1), cancellationToken);
        public virtual async Task SaveDatasAsync<T>(IEnumerable<T> datas, CancellationToken cancellationToken = default) where T : BaseData, new()
        {
            using var l = await _asyncLock.LockAsync(cancellationToken);
            SheetIndexAttribute? sheetIndexAttribute = typeof(T).GetCustomAttribute<SheetIndexAttribute>();
            if (sheetIndexAttribute is null)
                throw new InvalidOperationException($"'{typeof(T).FullName}' must contain attribute {nameof(SheetIndexAttribute)}");

            await _RunInTask(() => _SaveDataAsync(sheetIndexAttribute, datas, cancellationToken));
        }
        protected virtual void _SaveDataAsync<T>(SheetIndexAttribute sheetIndexAttribute, IEnumerable<T> datas, CancellationToken cancellationToken = default) where T : BaseData, new()
        {
            using ExcelPackage package = new ExcelPackage(_filePath);
            ExcelWorksheet excelWorksheet = sheetIndexAttribute.GetSheet(package.Workbook.Worksheets);

            bool isChanged = false;

            foreach (T data in datas)
            {
                foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ColAttribute? colAttribute = propertyInfo.GetCustomAttribute<ColAttribute>();
                    if (colAttribute is not null && colAttribute.Flag.HasFlag(ColFlag.IsUpdateBack))
                    {
                        object? pData = propertyInfo.GetValue(data);
                        if (pData is not null)
                        {
                            excelWorksheet.Cells[$"{colAttribute.Col}{data.LineIndex}"].Value = pData;
                            isChanged = true;
                        }
                    }

                    ColRangeAttribute? colRangeAttribute = propertyInfo.GetCustomAttribute<ColRangeAttribute>();
                    if (colRangeAttribute is not null && colRangeAttribute.Flag.HasFlag(ColFlag.IsUpdateBack))
                    {
                        object? pData = propertyInfo.GetValue(data);
                        if (pData is IDictionary<string, string> dict)
                        {
                            foreach (var pair in dict)
                            {
                                if (colRangeAttribute.Cols.Contains(pair.Key))
                                {
                                    excelWorksheet.Cells[$"{pair.Key}{data.LineIndex}"].Value = pair.Value;
                                    isChanged = true;
                                }
                            }
                        }
                        else if (pData is IEnumerable collection)
                        {
                            var cols = colRangeAttribute.Cols.ToList();
                            int index = 0;
                            foreach (object? item in collection)
                            {
                                if (index >= cols.Count)
                                    break;

                                excelWorksheet.Cells[$"{cols[index]}{data.LineIndex}"].Value = item?.ToString();

                                isChanged = true;
                                index++;
                            }
                        }

                    }
                }
            }

            if (isChanged)
                package.Save();
        }



        public virtual async Task<IReadOnlyList<T>> GetDatasAsync<T>(
            bool isReadAll = false,
            bool stopAtEmptyLine = false,
            CancellationToken cancellationToken = default) where T : BaseData, new()
        {
            using var l = await _asyncLock.LockAsync(cancellationToken);
            return await _RunInTask(() => _GetDatas<T>(isReadAll, stopAtEmptyLine));
        }
        protected virtual IReadOnlyList<T> _GetDatas<T>(
            bool isReadAll = false,
            bool stopAtEmptyLine = false
            ) where T : BaseData, new()
        {
            SheetIndexAttribute? sheetIndexAttribute = typeof(T).GetCustomAttribute<SheetIndexAttribute>();
            if (sheetIndexAttribute is null)
                throw new InvalidOperationException($"'{typeof(T).FullName}' must contain attribute {nameof(SheetIndexAttribute)}");

            using ExcelPackage package = new ExcelPackage(_filePath);
            ExcelWorksheet excelWorksheet = sheetIndexAttribute.GetSheet(package.Workbook.Worksheets);

            List<T> values = new List<T>();
            if (excelWorksheet is not null)
            {
                for (int i = excelWorksheet.Rows.StartRow + 1; i < excelWorksheet.Rows.EndRow; i++)
                {
                    T? instance = _ReadRow<T>(excelWorksheet, i, isReadAll, out bool isEmptyLine);
                    if (instance is not null)
                        values.Add(instance);
                    else if (stopAtEmptyLine && isEmptyLine)
                        break;
                }
            }
            return values;
        }
        protected virtual T? _ReadRow<T>(ExcelWorksheet excelWorksheet, int lineIndex, bool isReadAll, out bool isEmptyLine) where T : BaseData, new()
        {
            bool isSkip = false;
            isEmptyLine = true;

            T instance = new T();
            instance.LineIndex = lineIndex;
            instance.ExcelFilePath = _filePath;
            foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
            {
                ColAttribute? colAttribute = propertyInfo.GetCustomAttribute<ColAttribute>();
                if (colAttribute is not null &&
                    propertyInfo.CanWrite &&
                    propertyInfo.PropertyType.Equals(typeof(string))
                    )
                {
                    string? data = excelWorksheet.Cells[$"{colAttribute.Col}{lineIndex}"].Value?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(data))
                    {
                        if (!isReadAll && colAttribute.Flag.HasFlag(ColFlag.SkipReadLineIfCell_Empty))
                        {
                            isSkip = true;
                            break;
                        }
                    }
                    else
                    {
                        if (!isReadAll && colAttribute.Flag.HasFlag(ColFlag.SkipReadLineIfCell_NotEmpty))
                        {
                            isSkip = true;
                            break;
                        }
                        propertyInfo.SetValue(instance, data);
                        isEmptyLine = false;
                    }
                }

                ColRangeAttribute? colRangeAttribute = propertyInfo.GetCustomAttribute<ColRangeAttribute>();
                if (colRangeAttribute is not null && propertyInfo.CanRead)
                {
                    bool isCollection = typeof(ICollection<string>).IsAssignableFrom(propertyInfo.PropertyType);
                    bool isDictionary = typeof(IDictionary<string, string>).IsAssignableFrom(propertyInfo.PropertyType);
                    if (isCollection || isDictionary)
                    {
                        object? pInstance = propertyInfo.GetValue(instance);
                        ICollection<string>? collection = null;
                        IDictionary<string, string>? dictionary = null;
                        if (pInstance is null)
                        {
                            if (!propertyInfo.CanWrite)
                                throw new InvalidOperationException($"Can't write to property '{propertyInfo.Name}'");

                            if (propertyInfo.PropertyType.IsInterface)
                            {
                                //create
                                if (isCollection)
                                {
                                    collection = new List<string>();
                                    pInstance = collection;
                                }
                                else
                                {
                                    dictionary = new Dictionary<string, string>();
                                    pInstance = dictionary;
                                }
                            }
                            else
                            {
                                //create with default ctor
                                pInstance = Activator.CreateInstance(propertyInfo.PropertyType);
                                if (isCollection)
                                {
                                    collection = (ICollection<string>)pInstance;
                                }
                                else
                                {
                                    dictionary = (IDictionary<string, string>)pInstance;
                                }
                            }
                            propertyInfo.SetValue(instance, pInstance);
                        }
                        else
                        {
                            if (isCollection)
                            {
                                collection = (ICollection<string>)pInstance;
                            }
                            else
                            {
                                pInstance = (IDictionary<string, string>)pInstance;
                            }
                        }
                        if (dictionary is null)
                            dictionary = new Dictionary<string, string>();
                        foreach (string col in colRangeAttribute.Cols)
                        {
                            string? data = excelWorksheet.Cells[$"{col}{lineIndex}"].Value?.ToString()?.Trim();
                            if (string.IsNullOrWhiteSpace(data))
                            {
                                if (!isReadAll && colRangeAttribute.Flag.HasFlag(ColFlag.SkipReadLineIfCell_Empty))
                                {
                                    isSkip = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (!isReadAll && colRangeAttribute.Flag.HasFlag(ColFlag.SkipReadLineIfCell_NotEmpty))
                                {
                                    isSkip = true;
                                    break;
                                }
                                dictionary.Add(col, data!);
                                isEmptyLine = false;
                            }
                        }
                        if (collection is not null)
                        {
                            foreach (string val in dictionary.Values)
                                collection.Add(val);
                        }

                    }
                }
            }

            if (isSkip)
                return null;
            if (isEmptyLine)
                return null;

            return instance;
        }




        public abstract class BaseData
        {
            public virtual int LineIndex { get; set; }
            public virtual string? ExcelFilePath { get; set; }
        }
    }
}
