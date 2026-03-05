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
        M = 0, // 内部继电器
        X = 1, // 输入继电器
        Y = 2, // 输出继电器
        L = 3, // 锁存继电器
        B = 4, // 位数据寄存器
        TS = 5, // 定时器接点
        CS = 6, // 计数器接点
        TC = 7, // 定时器线圈
        CC = 8, // 计数器线圈
        
        
        /// <summary>
        /// 字数据
        /// </summary>
        D = 10, // 数据寄存器
        W = 11, // 特殊寄存器
        R = 12, // 文件寄存器
        ZR = 13, // 字数据寄存器
        T = 14, // 定时器
        C = 15, // 计数器

        
        /// <summary>
        /// 双字数据
        /// </summary>
        D32 = 20, // 双字数据寄存器
        Float = 21, // 单精度浮点数
        F64 = 23 // 双精度浮点数


    }
}