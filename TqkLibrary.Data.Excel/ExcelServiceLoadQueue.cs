﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TqkLibrary.Data.Excel
{
    public class ExcelServiceLoadQueue : BaseExcelService
    {
        protected readonly Dictionary<Type, Queue<object>> _dict_Queues = new();
        public ExcelServiceLoadQueue(string filePath) : base(filePath)
        {
        }

        public override async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            using var l = await _asyncLock.LockAsync(cancellationToken);
            _dict_Queues.Clear();
        }

        protected virtual async Task<Queue<object>> _EnsureQueueAsync<T>() where T : BaseData, new()
        {
            Queue<object>? queue = null;
            if (!_dict_Queues.ContainsKey(typeof(T)))
            {
                queue = new Queue<object>();
                _dict_Queues[typeof(T)] = queue;
                var datas = await _RunInTask(() => _GetDatas<T>(false, true));
                foreach (var data in datas)
                {
                    queue.Enqueue(data);
                }
            }
            else
            {
                queue = _dict_Queues[typeof(T)];
            }
            return queue;
        }


        public virtual async Task<T?> DeQueueAsync<T>(CancellationToken cancellationToken = default) where T : BaseData, new()
        {
            using var l = await _asyncLock.LockAsync(cancellationToken);

            Queue<object> queue = await _EnsureQueueAsync<T>();
            if (queue.Count > 0)
            {
                var data = queue.Dequeue();
                return (T)data;
            }
            else
            {
                return default;
            }
        }
        public virtual async Task ReQueueAsync<T>(T data, CancellationToken cancellationToken = default) where T : BaseData, new()
        {
            using var l = await _asyncLock.LockAsync(cancellationToken);

            Queue<object> queue = await _EnsureQueueAsync<T>();
            queue.Enqueue(data);
        }

    }
}
