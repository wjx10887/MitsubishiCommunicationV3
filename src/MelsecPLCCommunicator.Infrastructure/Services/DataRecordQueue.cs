using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MelsecPLCCommunicator.Domain.Models;

namespace MelsecPLCCommunicator.Infrastructure.Services
{
    /// <summary>
    /// 数据记录队列
    /// 用于缓冲待写入的PLC变量读取记录
    /// </summary>
    public class DataRecordQueue : IDisposable
    {
        /// <summary>
        /// 队列容量
        /// </summary>
        private readonly int _capacity;

        /// <summary>
        /// 环形缓冲区
        /// </summary>
        private readonly ReadRecord[] _buffer;

        /// <summary>
        /// 写入位置
        /// </summary>
        private int _writeIndex = 0;

        /// <summary>
        /// 读取位置
        /// </summary>
        private int _readIndex = 0;

        /// <summary>
        /// 队列中的元素数量
        /// </summary>
        private int _count = 0;

        /// <summary>
        /// 同步锁
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// 信号量，用于通知有数据可用
        /// </summary>
        private readonly SemaphoreSlim _semaphore;

        /// <summary>
        /// 是否已 disposed
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="capacity">队列容量，默认10000</param>
        public DataRecordQueue(int capacity = 10000)
        {
            _capacity = capacity;
            _buffer = new ReadRecord[capacity];
            _semaphore = new SemaphoreSlim(0, capacity);
        }

        /// <summary>
        /// 队列中的元素数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _count;
                }
            }
        }

        /// <summary>
        /// 队列是否为空
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return _count == 0;
                }
            }
        }

        /// <summary>
        /// 队列是否已满
        /// </summary>
        public bool IsFull
        {
            get
            {
                lock (_lock)
                {
                    return _count == _capacity;
                }
            }
        }

        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="record">读取记录</param>
        public void Enqueue(ReadRecord record)
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(DataRecordQueue));
                }

                // 如果队列已满，覆盖最旧的数据
                if (_count == _capacity)
                {
                    // 移动读取位置，相当于丢弃最旧的数据
                    _readIndex = (_readIndex + 1) % _capacity;
                    _count--;
                }

                // 写入数据
                _buffer[_writeIndex] = record;
                _writeIndex = (_writeIndex + 1) % _capacity;
                _count++;

                // 释放信号量，通知有数据可用
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 出队
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>读取记录</returns>
        public async Task<ReadRecord> DequeueAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(DataRecordQueue));
                }

                if (_count == 0)
                {
                    return null;
                }

                // 读取数据
                var record = _buffer[_readIndex];
                _readIndex = (_readIndex + 1) % _capacity;
                _count--;

                return record;
            }
        }

        /// <summary>
        /// 批量出队
        /// </summary>
        /// <param name="maxCount">最大数量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>读取记录列表</returns>
        public async Task<List<ReadRecord>> DequeueBatchAsync(int maxCount, CancellationToken cancellationToken = default)
        {
            var records = new List<ReadRecord>();

            // 先获取可用的信号量数量
            int available = Math.Min(_semaphore.CurrentCount, maxCount);
            if (available > 0)
            {
                // 批量获取信号量
                for (int i = 0; i < available; i++)
                {
                    if (!_semaphore.Wait(100)) // 增加等待时间
                    {
                        break;
                    }

                    lock (_lock)
                    {
                        if (_disposed || _count == 0)
                        {
                            // 如果队列已空，将信号量放回
                            _semaphore.Release();
                            break;
                        }

                        var record = _buffer[_readIndex];
                        _readIndex = (_readIndex + 1) % _capacity;
                        _count--;
                        records.Add(record);
                    }
                }
            }
            else
            {
                // 没有可用信号量，等待一段时间
                await Task.Delay(100, cancellationToken);
            }

            return records;
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _writeIndex = 0;
                _readIndex = 0;
                _count = 0;
                // 清空信号量
                while (_semaphore.CurrentCount > 0)
                {
                    _semaphore.Wait();
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _semaphore.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
