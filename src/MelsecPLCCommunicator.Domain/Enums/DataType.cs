namespace MelsecPLCCommunicator.Domain.Enums
{
    /// <summary>
    /// 数据类型枚举
    /// </summary>
    public enum DataType
    {
        /// <summary>
        /// 位数据
        /// </summary>
        M = 0, // 中间寄存器
        X = 1, // 输入寄存器
        Y = 2, // 输出寄存器
        B = 3, // 连接继电器
        T = 4, // 定时器
        C = 5, // 计数器
        S = 6, // 状态寄存器
        F = 7, // 报警器
         
        /// <summary>
        /// 字数据
        /// </summary>
        D = 8, // 数据寄存器
        W = 9, // 链接寄存器
        R = 10, // 文件寄存器
        TN = 11, // 定时器当前值
        CN = 12, // 计数器当前值
        
        /// <summary>
        /// 双字数据
        /// </summary>
        D32 = 13, // 32位整型
        Float = 14, // 浮点数
        DFloat = 15 // 双精度浮点
    }
}