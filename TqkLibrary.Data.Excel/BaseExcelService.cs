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
    public partial class BaseExcelService
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
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            this._filePath = filePath;
        }

        public virtual Task ResetLineIndexAsync(CancellationToken cancellationToken = default)
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

        public void ResetLastInsertEmptyRow() => lastEmptyRow = null;
        public virtual Task AppendNewDataAsync<T>(T data, CancellationToken cancellationToken = default) where T : BaseData
            => AppendNewDatasAsync<T>(Enumerable.Repeat(data, 1), cancellationToken);
        public virtual async Task AppendNewDatasAsync<T>(IEnumerable<T> datas, CancellationToken cancellationToken = default) where T : BaseData
        {
            using var l = await _asyncLock.LockAsync(cancellationToken);
            SheetIndexAttribute? sheetIndexAttribute = typeof(T).GetCustomAttribute<SheetIndexAttribute>();
            if (sheetIndexAttribute is null)
                throw new InvalidOperationException($"'{typeof(T).FullName}' must contain attribute {nameof(SheetIndexAttribute)}");

            await _RunInTask(() => _AppendNewDatasAsync(sheetIndexAttribute, datas, cancellationToken));
        }
        int? lastEmptyRow = null;
        protected virtual async Task _AppendNewDatasAsync<T>(SheetIndexAttribute sheetIndexAttribute, IEnumerable<T> datas, CancellationToken cancellationToken = default) where T : BaseData
        {
            FileInfo fileInfo = new FileInfo(_filePath);
            using ExcelPackage package = fileInfo.Exists ? new ExcelPackage(_filePath) : new ExcelPackage();
            ExcelWorksheet excelWorksheet = sheetIndexAttribute.GetSheet(package.Workbook.Worksheets);
            PropertyInfo[] propertyInfos = typeof(T).GetProperties();
            ColAttribute[] colAttributes = propertyInfos
                .Select(x => x.GetCustomAttribute<ColAttribute>())
                .Where(x => x is not null)
                .ToArray();
            ColRangeAttribute[] colRangeAttributes = propertyInfos
                .Select(x => x.GetCustomAttribute<ColRangeAttribute>())
                .Where(x => x is not null)
                .ToArray();
            string[] cols = colAttributes.Select(x => x.Col).Append(colRangeAttributes.SelectMany(x => x.Cols)).Distinct().ToArray();
            bool CheckIsRowEmpty(int row)
            {
                bool isEmptyRow = true;
                foreach (string col in cols)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string? val = excelWorksheet.Cells[$"{col}{row}"].Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        isEmptyRow = false;
                        break;
                    }
                }
                return isEmptyRow;
            }

            if (!lastEmptyRow.HasValue)
            {
                lastEmptyRow = excelWorksheet.Rows.StartRow + sheetIndexAttribute.StartRowOffset;
            }


            bool isNeedSaved = false;
            foreach (var data in datas)
            {
                //find empty row
                for (int row = lastEmptyRow.Value; row <= excelWorksheet.Rows.EndRow; row++)
                {
                    if (CheckIsRowEmpty(row))
                    {
                        lastEmptyRow = row;
                        break;
                    }
                }

                bool isDataInserted = false;
                foreach (PropertyInfo propertyInfo in propertyInfos.Where(x => x.CanRead))
                {
                    object? value = propertyInfo.GetValue(data);
                    if (value is null)
                        continue;

                    ColAttribute? colAttribute = propertyInfo.GetCustomAttribute<ColAttribute>();
                    if (colAttribute is not null)
                    {
                        string? valueStr = value?.ToString();
                        if (string.IsNullOrWhiteSpace(valueStr))
                            continue;

                        excelWorksheet.Cells[$"{colAttribute.Col}{lastEmptyRow.Value}"].Value = valueStr;
                        isDataInserted = true;
                    }
                    ColRangeAttribute? colRangeAttribute = propertyInfo.GetCustomAttribute<ColRangeAttribute>();
                    if (colRangeAttribute is not null &&
                        typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType)
                        )
                    {
                        bool isIEnumerable = typeof(IEnumerable<string>).IsAssignableFrom(propertyInfo.PropertyType);
                        bool isDictionary = typeof(IDictionary<string, string>).IsAssignableFrom(propertyInfo.PropertyType);
                        bool isIReadOnlyDictionary = typeof(IReadOnlyDictionary<string, string>).IsAssignableFrom(propertyInfo.PropertyType);

                        IEnumerable<string> strings;
                        if (isDictionary)
                        {
                            strings = ((IDictionary<string, string>)value!).Values;
                        }
                        else if (isIReadOnlyDictionary)
                        {
                            strings = ((IReadOnlyDictionary<string, string>)value!).Values;
                        }
                        else if (isIEnumerable)
                        {
                            strings = (IEnumerable<string>)value!;
                        }
                        else
                        {
                            IEnumerable<string> _convert()
                            {
                                foreach (var item in (IEnumerable)value!)
                                {
                                    yield return item?.ToString()!;
                                }
                            }
                            strings = _convert();
                        }

                        var Liststrings = strings.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                        for (int i = 0; i < Liststrings.Count && i < colRangeAttribute.Cols.Count(); i++)
                        {
                            excelWorksheet.Cells[$"{colRangeAttribute.Cols.Skip(i).First()}{lastEmptyRow.Value}"].Value = Liststrings[i];
                            isDataInserted = true;
                        }
                    }
                }
                if (isDataInserted)
                {
                    lastEmptyRow++;
                    isNeedSaved = true;
                }
            }
            if (isNeedSaved)
            {
                if (fileInfo.Exists)
                {
                    await package.SaveAsync();
                }
                else
                {
                    await package.SaveAsAsync(fileInfo);
                }
            }
        }


        public virtual async Task<IReadOnlyList<T>> GetDatasAsync<T>(
            ExcelReadOption? excelReadOption = null,
            CancellationToken cancellationToken = default
            ) where T : BaseData, new()
        {
            using var l = await _asyncLock.LockAsync(cancellationToken);
            return await _RunInTask(() => _GetDatas<T>(excelReadOption));
        }
        protected virtual IReadOnlyList<T> _GetDatas<T>(ExcelReadOption? excelReadOption = null) where T : BaseData, new()
        {
            SheetIndexAttribute? sheetIndexAttribute = typeof(T).GetCustomAttribute<SheetIndexAttribute>();
            if (excelReadOption?.ForceSheetIndex is not null)
                sheetIndexAttribute = excelReadOption.ForceSheetIndex;
            if (sheetIndexAttribute is null)
                throw new InvalidOperationException($"'{typeof(T).FullName}' must contain attribute {nameof(SheetIndexAttribute)}");

            using ExcelPackage package = new ExcelPackage(_filePath);
            ExcelWorksheet excelWorksheet = sheetIndexAttribute.GetSheet(package.Workbook.Worksheets);

            List<T> values = new List<T>();
            if (excelWorksheet is not null)
            {
                for (int i = excelWorksheet.Rows.StartRow + Math.Max(0, sheetIndexAttribute.StartRowOffset); i < excelWorksheet.Rows.EndRow; i++)
                {
                    T? instance = _ReadRow<T>(excelWorksheet, i, excelReadOption?.IsReadAll == true, out bool isEmptyLine);
                    if (instance is not null)
                        values.Add(instance);
                    else if (excelReadOption?.StopAtEmptyLine == true && isEmptyLine)
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
                if (isSkip)
                    break;
                CellAttribute? cellAttribute = propertyInfo.GetCustomAttribute<CellAttribute>();
                if (cellAttribute is not null && propertyInfo.CanWrite)
                {
                    string? data = excelWorksheet.Cells[cellAttribute.Cell].Value?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(data))
                    {
                        if (!isReadAll && cellAttribute.Flag.HasFlag(ColFlag.SkipReadLineIfCell_Empty))
                        {
                            isSkip = true;
                            break;
                        }
                    }
                    else
                    {
                        if (!isReadAll && cellAttribute.Flag.HasFlag(ColFlag.SkipReadLineIfCell_NotEmpty))
                        {
                            isSkip = true;
                            break;
                        }
                        propertyInfo.SetValue(instance, data);
                    }
                }
                if (isSkip)
                    break;

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
                if (isSkip)
                    break;

                ColRangeAttribute? colRangeAttribute = propertyInfo.GetCustomAttribute<ColRangeAttribute>();
                if (colRangeAttribute is not null && propertyInfo.CanRead)
                {
                    bool isCollection = typeof(ICollection<string>).IsAssignableFrom(propertyInfo.PropertyType);
                    bool isIEnumerable = typeof(IEnumerable<string>).IsAssignableFrom(propertyInfo.PropertyType);
                    bool isDictionary = typeof(IDictionary<string, string>).IsAssignableFrom(propertyInfo.PropertyType);
                    bool isIReadOnlyDictionary = typeof(IReadOnlyDictionary<string, string>).IsAssignableFrom(propertyInfo.PropertyType);
                    if (isCollection ||
                        isDictionary ||
                        (propertyInfo.PropertyType.IsInterface && (isIEnumerable || isIReadOnlyDictionary))
                        )
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
                                if (isCollection || isIEnumerable)
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
