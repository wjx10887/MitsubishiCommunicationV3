using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Data.SQLite;

namespace MelsecPLCCommunicator.Infrastructure.Services
{
    /// <summary>
    /// 数据库连接池
    /// 用于管理MySQL数据库连接，优化数据库性能
    /// </summary>
    public class ConnectionPool : IDisposable
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        private readonly string _connectionString;

        /// <summary>
        /// 连接池
        /// </summary>
        private readonly Queue<IDbConnection> _pool;

        /// <summary>
        /// 最大连接数
        /// </summary>
        private readonly int _maxConnections;

        /// <summary>
        /// 当前连接数
        /// </summary>
        private int _currentConnections;

        /// <summary>
        /// 同步锁
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// 是否已 disposed
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="maxConnections">最大连接数，默认10</param>
        public ConnectionPool(string connectionString, int maxConnections = 10)
        {
            _connectionString = connectionString;
            _maxConnections = maxConnections;
            _pool = new Queue<IDbConnection>();
            _currentConnections = 0;
        }

        /// <summary>
        /// 获取连接
        /// </summary>
        /// <returns>数据库连接</returns>
        public IDbConnection GetConnection()
        {
            lock (_lock)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(ConnectionPool));
                }

                // 如果池中有可用连接，直接返回
                if (_pool.Count > 0)
                {
                    return _pool.Dequeue();
                }

                // 如果未达到最大连接数，创建新连接
                if (_currentConnections < _maxConnections)
                {
                    _currentConnections++;
                    return CreateConnection();
                }

                // 等待连接释放
                while (_pool.Count == 0)
                {
                    Monitor.Wait(_lock);
                }

                return _pool.Dequeue();
            }
        }

        /// <summary>
        /// 归还连接
        /// </summary>
        /// <param name="connection">数据库连接</param>
        public void ReturnConnection(IDbConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            lock (_lock)
            {
                if (_disposed)
                {
                    connection.Dispose();
                    return;
                }

                // 检查连接是否有效
                if (connection.State == ConnectionState.Open)
                {
                    _pool.Enqueue(connection);
                }
                else
                {
                    // 连接已关闭，减少连接数
                    _currentConnections--;
                    connection.Dispose();
                }

                // 通知等待的线程
                Monitor.Pulse(_lock);
            }
        }

        /// <summary>
        /// 创建新连接
        /// </summary>
        /// <returns>数据库连接</returns>
        private IDbConnection CreateConnection()
        {
            try
            {
                // 创建SQLite连接
                var connection = new SQLiteConnection();
                connection.ConnectionString = _connectionString;
                connection.Open();
                return connection;
            }
            catch (SQLiteException ex)
            {
                // 提供更详细的错误信息
                string errorMessage = "SQLite连接失败: ";
                switch (ex.ErrorCode)
                {
                    case 1:
                        errorMessage += "SQL错误。" + ex.Message;
                        break;
                    case 2:
                        errorMessage += "内部逻辑错误。" + ex.Message;
                        break;
                    case 3:
                        errorMessage += "访问权限错误。" + ex.Message;
                        break;
                    case 14:
                        errorMessage += "无法打开数据库文件。" + ex.Message;
                        break;
                    default:
                        errorMessage += ex.Message;
                        break;
                }
                throw new Exception(errorMessage, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("创建数据库连接失败: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 清空连接池
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                while (_pool.Count > 0)
                {
                    var connection = _pool.Dequeue();
                    try
                    {
                        connection.Dispose();
                    }
                    catch { }
                }
                _currentConnections = 0;
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
                    Clear();
                }

                _disposed = true;
            }
        }
    }
}
