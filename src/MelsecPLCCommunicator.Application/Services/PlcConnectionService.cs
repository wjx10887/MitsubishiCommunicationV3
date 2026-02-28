using System;
using System.Threading.Tasks;
using MelsecPLCCommunicator.Application.Interfaces;
using MelsecPLCCommunicator.Domain.DTOs;
using MelsecPLCCommunicator.Domain.Shared;
using MelsecPLCCommunicator.Infrastructure.Adapters;

namespace MelsecPLCCommunicator.Application.Services
{
    /// <summary>
    /// PLC连接服务实现
    /// </summary>
    public class PlcConnectionService : IPlcConnectionService
    {
        private readonly ICommunicationAdapterFactory _adapterFactory;
        private readonly ILogService _logService;
        private ICommunicationAdapter _currentAdapter;

        /// <summary>
        /// 通讯帧事件
        /// </summary>
        public event EventHandler<MelsecPLCCommunicator.Infrastructure.Adapters.FrameEventArgs> FrameReceived;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="adapterFactory">通信适配器工厂</param>
        /// <param name="logService">日志服务</param>
        public PlcConnectionService(ICommunicationAdapterFactory adapterFactory, ILogService logService)
        {
            _adapterFactory = adapterFactory;
            _logService = logService;
        }

        /// <summary>
        /// 当前通信适配器
        /// </summary>
        public ICommunicationAdapter CurrentAdapter => _currentAdapter;

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="config">连接配置</param>
        /// <returns>测试结果</returns>
        public async Task<Result<bool>> TestConnectionAsync(ConnectionConfigDto config)
        {
            _logService.Info("开始测试连接", $"接口类型: {config.InterfaceType}, 协议类型: {config.ProtocolType}, IP地址: {config.IpAddress}, 端口: {config.Port}, 串口: {config.PortName}");
            
            try
            {
                var adapter = _adapterFactory.CreateAdapter(
                    config.InterfaceType,
                    config.ProtocolType,
                    config.IpAddress,
                    config.Port,
                    config.PortName,
                    config.BaudRate,
                    config.Parity,
                    config.DataBits,
                    config.StopBits);
                
                _logService.Debug("创建通信适配器成功");
                
                var success = await Task.Run(() => adapter.Connect());
                
                if (success)
                {
                    _logService.Info("测试连接成功");
                }
                else
                {
                    _logService.Warning("测试连接失败");
                }
                
                await Task.Run(() => adapter.Disconnect());
                _logService.Info("断开测试连接");
                
                return Result<bool>.SuccessResult(success);
            }
            catch (System.Exception ex)
            {
                _logService.Error("测试连接异常", ex);
                return Result<bool>.FailureResult(Error.ConnectionError($"测试连接失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 建立连接
        /// </summary>
        /// <param name="config">连接配置</param>
        /// <returns>连接结果</returns>
        public async Task<Result> ConnectAsync(ConnectionConfigDto config)
        {
            _logService.Info("开始建立连接", $"接口类型: {config.InterfaceType}, 协议类型: {config.ProtocolType}, IP地址: {config.IpAddress}, 端口: {config.Port}, 串口: {config.PortName}");
            
            try
            {
                // 先断开现有连接
                if (_currentAdapter != null && _currentAdapter.IsConnected)
                {
                    _logService.Info("断开现有连接");
                    await Task.Run(() => _currentAdapter.Disconnect());
                }

                // 创建新适配器并连接
                _currentAdapter = _adapterFactory.CreateAdapter(
                    config.InterfaceType,
                    config.ProtocolType,
                    config.IpAddress,
                    config.Port,
                    config.PortName,
                    config.BaudRate,
                    config.Parity,
                    config.DataBits,
                    config.StopBits);
                
                // 订阅通讯帧事件
                _currentAdapter.FrameReceived += CurrentAdapter_FrameReceived;
                
                _logService.Debug("创建通信适配器成功");
                
                var success = await Task.Run(() => _currentAdapter.Connect());

                if (!success)
                {
                    _logService.Warning("连接失败");
                    return Result.FailureResult(Error.ConnectionError("连接失败"));
                }

                _logService.Info("连接成功");
                return Result.SuccessResult();
            }
            catch (System.Exception ex)
            {
                _logService.Error("连接异常", ex);
                return Result.FailureResult(Error.ConnectionError($"连接失败: {ex.Message}"));
            }
        }
        

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns>断开结果</returns>
        public async Task<Result> DisconnectAsync()
        {
            _logService.Info("开始断开连接");
            
            try
            {
                if (_currentAdapter != null && _currentAdapter.IsConnected)
                {
                    await Task.Run(() => _currentAdapter.Disconnect());
                    _logService.Info("断开连接成功");
                }
                else
                {
                    _logService.Info("设备未连接，无需断开");
                }
                return Result.SuccessResult();
            }
            catch (System.Exception ex)
            {
                _logService.Error("断开连接异常", ex);
                return Result.FailureResult(Error.ConnectionError($"断开连接失败: {ex.Message}"));
            }
        }
        

        /// <summary>
        /// 获取连接状态
        /// </summary>
        /// <returns>连接状态</returns>
        public Result<bool> GetConnectionStatus()
        {
            _logService.Debug("获取连接状态");
            
            try
            {
                var isConnected = _currentAdapter != null && _currentAdapter.IsConnected;
                _logService.Debug($"连接状态: {isConnected}");
                return Result<bool>.SuccessResult(isConnected);
            }
            catch (System.Exception ex)
            {
                _logService.Error("获取连接状态异常", ex);
                return Result<bool>.FailureResult(Error.SystemError($"获取连接状态失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 处理通讯帧事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void CurrentAdapter_FrameReceived(object sender, MelsecPLCCommunicator.Infrastructure.Adapters.FrameEventArgs e)
        {
            // 记录通讯帧日志
            _logService.Debug($"发送帧: {e.SendFrame}");
            _logService.Debug($"接收帧: {e.ReceiveFrame}");
            
            // 传递事件给上层
            FrameReceived?.Invoke(this, e);
        }


    }
}
