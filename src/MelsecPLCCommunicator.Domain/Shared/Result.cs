namespace MelsecPLCCommunicator.Domain.Shared
{
    /// <summary>
    /// 操作结果封装类
    /// </summary>
    /// <typeparam name="T">结果类型</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 结果数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public Error Error { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        /// <param name="data">结果数据</param>
        /// <returns>成功结果</returns>
        public static Result<T> SuccessResult(T data)
        {
            return new Result<T>
            {
                Success = true,
                Data = data,
                Error = null
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <param name="error">错误信息</param>
        /// <returns>失败结果</returns>
        public static Result<T> FailureResult(Error error)
        {
            return new Result<T>
            {
                Success = false,
                Data = default(T),
                Error = error
            };
        }
    }

    /// <summary>
    /// 无数据的操作结果
    /// </summary>
    public class Result
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public Error Error { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        /// <returns>成功结果</returns>
        public static Result SuccessResult()
        {
            return new Result
            {
                Success = true,
                Error = null
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <param name="error">错误信息</param>
        /// <returns>失败结果</returns>
        public static Result FailureResult(Error error)
        {
            return new Result
            {
                Success = false,
                Error = error
            };
        }
    }
}
