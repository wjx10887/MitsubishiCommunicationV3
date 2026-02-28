using System;

namespace MelsecPLCCommunicator.Application.Interfaces
{
    /// <summary>
    /// 日志服务接口
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="details">详细信息</param>
        void Info(string message, string details = null);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="details">详细信息</param>
        void Warning(string message, string details = null);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常对象</param>
        /// <param name="details">详细信息</param>
        void Error(string message, Exception exception = null, string details = null);

        /// <summary>
        /// 记录调试日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="details">详细信息</param>
        void Debug(string message, string details = null);

        /// <summary>
        /// 获取所有日志
        /// </summary>
        /// <returns>日志内容</returns>
        string GetAllLogs();

        /// <summary>
        /// 清除所有日志
        /// </summary>
        void ClearLogs();
    }
}