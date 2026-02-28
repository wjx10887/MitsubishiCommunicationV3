using System.Threading.Tasks;
using MelsecPLCCommunicator.Domain.Enums;
using MelsecPLCCommunicator.Domain.Shared;

namespace MelsecPLCCommunicator.Application.Interfaces
{
    /// <summary>
    /// PLC读写服务接口
    /// </summary>
    public interface IPlcReadWriteService
    {
        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="length">长度</param>
        /// <returns>读取结果</returns>
        Task<Result<object>> ReadAsync(string address, DataType dataType, ushort length);

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="value">值</param>
        /// <returns>写入结果</returns>
        Task<Result> WriteAsync(string address, DataType dataType, object value);

        /// <summary>
        /// 批量读取数据
        /// </summary>
        /// <param name="readRequests">读取请求列表</param>
        /// <returns>批量读取结果</returns>
        Task<Result<object[]>> BatchReadAsync(BatchReadRequest[] readRequests);

        /// <summary>
        /// 批量写入数据
        /// </summary>
        /// <param name="writeRequests">写入请求列表</param>
        /// <returns>批量写入结果</returns>
        Task<Result> BatchWriteAsync(BatchWriteRequest[] writeRequests);
    }

    /// <summary>
    /// 批量读取请求
    /// </summary>
    public class BatchReadRequest
    {
        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// 长度
        /// </summary>
        public ushort Length { get; set; }
    }

    /// <summary>
    /// 批量写入请求
    /// </summary>
    public class BatchWriteRequest
    {
        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        public object Value { get; set; }
    }
}
