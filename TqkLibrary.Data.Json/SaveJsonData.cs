using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace TqkLibrary.Data.Json
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SaveJsonData<T> : IDisposable, ISaveJsonDataControl
        where T : class
    {
        const double _defaultDelaySaving = 500;



        private readonly string _savePath;
        private readonly System.Timers.Timer _timer;
        protected readonly JsonSerializerSettings? _jsonSerializerSettings;
        private T? _data;

        public bool TrySaveOnError { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        public T Data
        {
            get
            {
                if (_data is null)
                    Load();
                return _data ?? throw new InvalidOperationException("data was not load");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public double DelaySaving
        {
            get { return _timer.Interval; }
            set { _timer.Interval = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        public event Action<T>? OnSaved;
        /// <summary>
        /// 
        /// </summary>
        public event Action<Exception>? OnSaveError;

        public SaveJsonData(string SavePath, JsonSerializerSettings? jsonSerializerSettings = null)
        {
            if (string.IsNullOrEmpty(SavePath)) throw new ArgumentNullException(nameof(SavePath));
            this._jsonSerializerSettings = jsonSerializerSettings;
            this._savePath = SavePath;
            _timer = new System.Timers.Timer(_defaultDelaySaving);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = false;
        }
        /// <summary>
        /// 
        /// </summary>
        ~SaveJsonData()
        {
            Dispose(false);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
        protected virtual void Dispose(bool disposing)
        {
            _timer.Dispose();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ForceSave();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ForceSave()
        {
            try
            {
                File.WriteAllText(_savePath, JsonConvert.SerializeObject(Data, Formatting.Indented, _jsonSerializerSettings));
                OnSaved?.Invoke(Data);
            }
            catch (Exception ex)
            {
                OnSaveError?.Invoke(ex);
                if (TrySaveOnError)
                {
                    _timer.Stop();
                    _timer.Start();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void TriggerSave()
        {
            _timer.Stop();
            _timer.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<T> result = new TaskCompletionSource<T>();
            Action<T> onSaved = (o) => result.TrySetResult(o);
            Action<Exception> onError = (e) => result.TrySetException(e);
            using var register = cancellationToken.Register(() => result.TrySetCanceled());
            try
            {
                OnSaved += onSaved;
                OnSaveError += onError;
                TriggerSave();
                await result.Task.ConfigureAwait(false);
            }
            finally
            {
                OnSaved -= onSaved;
                OnSaveError -= onError;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Load()
        {
            Load(_savePath);

            if (_data is null)
            {
                T defaultData = (T)Activator.CreateInstance(typeof(T));//throw if not have Parameterless
                Load(defaultData);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defaultData">default Data if file not exist</param>
        public virtual void Load(T defaultData)
        {
            Load(_savePath);

            if (_data is null)
                _data = defaultData ?? throw new ArgumentNullException(nameof(defaultData));
        }

        protected virtual void Load(string filePath)
        {
            if (File.Exists(filePath))
                _data = JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath), _jsonSerializerSettings);
        }
    }
}