using System;
using System.Threading.Tasks;
using MelsecPLCCommunicator.Domain.DTOs;
using MelsecPLCCommunicator.Domain.Shared;
using MelsecPLCCommunicator.Infrastructure.Adapters;

namespace MelsecPLCCommunicator.Application.Interfaces
{
    /// <summary>
    /// PLC连接服务接口
    /// </summary>
    public interface IPlcConnectionService
    {
        /// <summary>
        /// 当前通信适配器
        /// </summary>
        ICommunicationAdapter CurrentAdapter { get; }

        /// <summary>
        /// 通讯帧事件
        /// </summary>
        event EventHandler<MelsecPLCCommunicator.Infrastructure.Adapters.FrameEventArgs> FrameReceived;

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="config">连接配置</param>
        /// <returns>测试结果</returns>
        Task<Result<bool>> TestConnectionAsync(ConnectionConfigDto config);

        /// <summary>
        /// 建立连接
        /// </summary>
        /// <param name="config">连接配置</param>
        /// <returns>连接结果</returns>
        Task<Result> ConnectAsync(ConnectionConfigDto config);

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns>断开结果</returns>
        Task<Result> DisconnectAsync();

        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <returns>连接状态</returns>
        Result<bool> GetConnectionStatus();

        /// <summary>
        /// 获取当前连接配置
        /// </summary>
        /// <returns>当前连接配置</returns>
        ConnectionConfigDto GetCurrentConfig();
    }
}
