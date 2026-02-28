using System;

namespace MelsecPLCCommunicator.Domain.Shared
{
    /// <summary>
    /// 参数验证工具类
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// 验证参数不为空
        /// </summary>
        /// <param name="value">参数值</param>
        /// <param name="parameterName">参数名称</param>
        /// <exception cref="ArgumentNullException">参数为空时抛出</exception>
        public static void NotNull(object value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// 验证字符串不为空或空白
        /// </summary>
        /// <param name="value">字符串值</param>
        /// <param name="parameterName">参数名称</param>
        /// <exception cref="ArgumentNullException">字符串为空时抛出</exception>
        /// <exception cref="ArgumentException">字符串为空白时抛出</exception>
        public static void NotNullOrEmpty(string value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{parameterName} 不能为空或空白");
            }
        }

        /// <summary>
        /// 验证整数大于0
        /// </summary>
        /// <param name="value">整数值</param>
        /// <param name="parameterName">参数名称</param>
        /// <exception cref="ArgumentException">值小于等于0时抛出</exception>
        public static void GreaterThanZero(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentException($"{parameterName} 必须大于0");
            }
        }

        /// <summary>
        /// 验证整数在指定范围内
        /// </summary>
        /// <param name="value">整数值</param>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <param name="parameterName">参数名称</param>
        /// <exception cref="ArgumentException">值不在范围内时抛出</exception>
        public static void InRange(int value, int min, int max, string parameterName)
        {
            if (value < min || value > max)
            {
                throw new ArgumentException($"{parameterName} 必须在 {min} 到 {max} 之间");
            }
        }
    }
}
