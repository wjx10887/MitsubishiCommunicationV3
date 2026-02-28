using System;
using System.Collections.Generic;
using System.Text;
using MelsecPLCCommunicator.Application.Interfaces;

namespace MelsecPLCCommunicator.Application.Services
{
    /// <summary>
    /// 日志服务实现
    /// </summary>
    public class LogService : ILogService
    {
        private readonly List<string> _logs;
        private readonly object _lock = new object();

        /// <summary>
        /// 构造函数
        /// </summary>
        public LogService()
        {
            _logs = new List<string>();
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="details">详细信息</param>
        public void Info(string message, string details = null)
        {
            Log("INFO", message, details);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="details">详细信息</param>
        public void Warning(string message, string details = null)
        {
            Log("WARNING", message, details);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常对象</param>
        /// <param name="details">详细信息</param>
        public void Error(string message, Exception exception = null, string details = null)
        {
            var errorDetails = details;
            if (exception != null)
            {
                errorDetails = $"{details}{Environment.NewLine}Exception: {exception.Message}{Environment.NewLine}Stack Trace: {exception.StackTrace}";
            }
            Log("ERROR", message, errorDetails);
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="details">详细信息</param>
        public void Debug(string message, string details = null)
        {
            Log("DEBUG", message, details);
        }

        /// <summary>
        /// 获取所有日志
        /// </summary>
        /// <returns>日志内容</returns>
        public string GetAllLogs()
        {
            lock (_lock)
            {
                return string.Join(Environment.NewLine, _logs);
            }
        }

        /// <summary>
        /// 清除所有日志
        /// </summary>
        public void ClearLogs()
        {
            lock (_lock)
            {
                _logs.Clear();
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="details">详细信息</param>
        private void Log(string level, string message, string details = null)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] {message}";
            
            if (!string.IsNullOrEmpty(details))
            {
                logEntry += $"{Environment.NewLine}Details: {details}";
            }

            lock (_lock)
            {
                _logs.Add(logEntry);
                // 限制日志数量，防止内存溢出
                if (_logs.Count > 1000)
                {
                    _logs.RemoveAt(0);
                }
            }
        }
    }
}