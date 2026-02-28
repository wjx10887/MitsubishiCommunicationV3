namespace MelsecPLCCommunicator.Domain.Shared
{
    /// <summary>
    /// 错误信息封装类
    /// </summary>
    public class Error
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 详细信息
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="code">错误代码</param>
        /// <param name="message">错误消息</param>
        /// <param name="details">详细信息</param>
        public Error(string code, string message, string details = null)
        {
            Code = code;
            Message = message;
            Details = details;
        }

        /// <summary>
        /// 创建参数错误
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <returns>参数错误</returns>
        public static Error ParameterError(string message)
        {
            return new Error("PARAMETER_ERROR", message);
        }

        /// <summary>
        /// 创建连接错误
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <returns>连接错误</returns>
        public static Error ConnectionError(string message)
        {
            return new Error("CONNECTION_ERROR", message);
        }

        /// <summary>
        /// 创建读写错误
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <returns>读写错误</returns>
        public static Error ReadWriteError(string message)
        {
            return new Error("READ_WRITE_ERROR", message);
        }

        /// <summary>
        /// 创建系统错误
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <returns>系统错误</returns>
        public static Error SystemError(string message)
        {
            return new Error("SYSTEM_ERROR", message);
        }
    }
}
