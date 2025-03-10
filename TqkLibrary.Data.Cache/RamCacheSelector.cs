using System;

namespace TqkLibrary.Data.Cache
{
    public class RamCacheSelector<TKey, TValue> : RamCache<TKey, TValue>
    {
        readonly Func<TValue, TKey> _func_selector;
        public RamCacheSelector(Func<TValue, TKey> func_selector) : this(func_selector, TimeSpan.Zero)
        {

        }
        public RamCacheSelector(Func<TValue, TKey> func_selector, TimeSpan storeTimeout) : base(storeTimeout)
        {
            this._func_selector = func_selector ?? throw new ArgumentNullException(nameof(func_selector));
        }

        /// <summary>
        /// Add or update
        /// </summary>
        /// <param name="value"></param>
        public void Add(TValue value) => base.Add(_func_selector.Invoke(value), value);
    }
}
