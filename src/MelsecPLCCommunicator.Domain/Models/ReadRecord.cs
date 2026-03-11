using System;

namespace MelsecPLCCommunicator.Domain.Models
{
    /// <summary>
    /// PLC变量读取记录模型
    /// </summary>
    public class ReadRecord
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 变量ID
        /// </summary>
        public int VariableId { get; set; }

        /// <summary>
        /// 读取时间戳(毫秒精度)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 原始值
        /// </summary>
        public string RawValue { get; set; }

        /// <summary>
        /// 工程值
        /// </summary>
        public double? EngineeringValue { get; set; }

        /// <summary>
        /// 质量标志(1:良好, 0:异常)
        /// </summary>
        public bool QualityFlag { get; set; } = true;

        /// <summary>
        /// 变量信息（非数据库字段，用于查询时关联）
        /// </summary>
        public PlcVariable Variable { get; set; }

        /// <summary>
        /// 变量地址（非数据库字段，用于查询时过滤）
        /// </summary>
        public string VariableAddress => Variable?.Address;
    }
}
