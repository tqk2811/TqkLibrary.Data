using System;
using System.Collections.Generic;
using System.Linq;

namespace TqkLibrary.Data.Cache
{
    public class RamCache<TKey, TValue>
    {
        class StoreData
        {
            public DateTime StoreTime { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
            public TValue Value { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        }


        readonly Dictionary<TKey, StoreData> _dict = new();
        public TimeSpan StoreTimeout { get; set; }

        public RamCache() : this(TimeSpan.Zero)
        {

        }
        public RamCache(TimeSpan storeTimeout)
        {
            this.StoreTimeout = storeTimeout;
        }

        void ClearTimeout()
        {
            if (StoreTimeout <= TimeSpan.Zero) 
                return;
            lock (_dict)
            {
                DateTime now = DateTime.UtcNow;
                foreach (var key in _dict.Keys.ToList())
                {
                    if (_dict[key].StoreTime + StoreTimeout < now)
                    {
                        _dict.Remove(key);
                    }
                }
            }
        }

        /// <summary>
        /// Add or update
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            lock (_dict)
            {
                if (_dict.TryGetValue(key, out StoreData? storeData))
                {
                    storeData.Value = value;
                    storeData.StoreTime = DateTime.UtcNow;
                }
                else
                {
                    storeData = new StoreData
                    {
                        Value = value,
                        StoreTime = DateTime.UtcNow
                    };
                }
            }
            ClearTimeout();
        }

        public TValue? TryGetValue(TKey key)
        {
            ClearTimeout();
            lock (_dict)
            {
                if (_dict.TryGetValue(key, out StoreData? storeData))
                {
                    return storeData.Value;
                }
                return default(TValue?);
            }
        }
    }
}
