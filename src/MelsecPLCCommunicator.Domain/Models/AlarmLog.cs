using System;

namespace MelsecPLCCommunicator.Domain.Models
{
    /// <summary>
    /// 通讯异常记录模型
    /// </summary>
    public class AlarmLog
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 异常时间戳(毫秒精度)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 错误代码
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 严重程度(1:警告, 2:错误, 3:严重)
        /// </summary>
        public int Severity { get; set; } = 1;

        /// <summary>
        /// 是否已解决(0:未解决, 1:已解决)
        /// </summary>
        public bool IsResolved { get; set; } = false;

        /// <summary>
        /// 解决时间
        /// </summary>
        public DateTime? ResolvedAt { get; set; }
    }
}
