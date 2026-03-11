using System;

namespace MelsecPLCCommunicator.Domain.Models
{
    /// <summary>
    /// PLC变量配置模型
    /// </summary>
    public class PlcVariable
    {
        /// <summary>
        /// 变量ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 变量地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 变量名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 采样周期(ms)
        /// </summary>
        public int SamplingPeriod { get; set; }

        /// <summary>
        /// 工程单位
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// 最小值
        /// </summary>
        public double? MinValue { get; set; }

        /// <summary>
        /// 最大值
        /// </summary>
        public double? MaxValue { get; set; }

        /// <summary>
        /// 缩放因子
        /// </summary>
        public double ScaleFactor { get; set; } = 1.0;

        /// <summary>
        /// 偏移量
        /// </summary>
        public double Offset { get; set; } = 0.0;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 计算工程值
        /// </summary>
        /// <param name="rawValue">原始值</param>
        /// <returns>工程值</returns>
        public double CalculateEngineeringValue(double rawValue)
        {
            return rawValue * ScaleFactor + Offset;
        }
    }
}
