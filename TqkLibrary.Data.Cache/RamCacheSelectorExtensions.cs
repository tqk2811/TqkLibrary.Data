using System;
using System.Linq.Expressions;

namespace TqkLibrary.Data.Cache
{
    public static class RamCacheSelectorExtensions
    {
        public static RamCacheSelector<TKey, TValue> CreateRamCacheSelector<TKey, TValue>(this TValue value, Expression<Func<TValue, TKey>> selector)
        {
            return value.CreateRamCacheSelector(selector, TimeSpan.Zero);
        }
        public static RamCacheSelector<TKey, TValue> CreateRamCacheSelector<TKey, TValue>(this TValue value, Expression<Func<TValue, TKey>> selector, TimeSpan storeTimeout)
        {
            var result = new RamCacheSelector<TKey, TValue>(selector.Compile(), storeTimeout);
            return result;
        }
    }
}
