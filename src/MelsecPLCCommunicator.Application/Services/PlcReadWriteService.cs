using System;
using System.Threading.Tasks;
using MelsecPLCCommunicator.Application.Interfaces;
using MelsecPLCCommunicator.Domain.Enums;
using MelsecPLCCommunicator.Domain.Shared;
using MelsecPLCCommunicator.Infrastructure.Adapters;

namespace MelsecPLCCommunicator.Application.Services
{
    /// <summary>
    /// PLC读写服务实现
    /// </summary>
    public class PlcReadWriteService : IPlcReadWriteService
    {
        private readonly IPlcConnectionService _connectionService;
        private readonly ILogService _logService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionService">连接服务</param>
        /// <param name="logService">日志服务</param>
        public PlcReadWriteService(IPlcConnectionService connectionService, ILogService logService)
        {
            _connectionService = connectionService;
            _logService = logService;
        }

        /// <summary>
        /// 当前通信适配器
        /// </summary>
        private ICommunicationAdapter Adapter => _connectionService.CurrentAdapter;

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="length">长度</param>
        /// <returns>读取结果</returns>
        public async Task<Result<object>> ReadAsync(string address, DataType dataType, ushort length)
        {
            _logService.Info("开始读取数据", $"地址: {address}, 数据类型: {dataType}, 长度: {length}");
            
            try
            {
                if (Adapter == null || !Adapter.IsConnected)
                {
                    _logService.Warning("设备未连接");
                    return Result<object>.FailureResult(Error.ConnectionError("设备未连接"));
                }

                var result = await Task.Run(() => Adapter.Read(address, dataType.ToString(), length));
                
                // 记录通讯帧
                var sentFrame = Adapter.LastSentFrame;
                var receivedFrame = Adapter.LastReceivedFrame;
                
                if (sentFrame != null)
                {
                    _logService.Debug($"发送帧: {BitConverter.ToString(sentFrame)}");
                }
                if (receivedFrame != null)
                {
                    _logService.Debug($"接收帧: {BitConverter.ToString(receivedFrame)}");
                }
                
                _logService.Info($"读取成功: {result}");
                return Result<object>.SuccessResult(result);
            }
            catch (System.Exception ex)
            {
                _logService.Error("读取数据异常", ex);
                return Result<object>.FailureResult(Error.ReadWriteError($"读取失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="value">值</param>
        /// <returns>写入结果</returns>
        public async Task<Result> WriteAsync(string address, DataType dataType, object value)
        {
            _logService.Info("开始写入数据", $"地址: {address}, 数据类型: {dataType}, 值: {value}");
            
            try
            {
                if (Adapter == null || !Adapter.IsConnected)
                {
                    _logService.Warning("设备未连接");
                    return Result.FailureResult(Error.ConnectionError("设备未连接"));
                }

                var success = await Task.Run(() => Adapter.Write(address, dataType.ToString(), value));
                
                // 记录通讯帧
                var sentFrame = Adapter.LastSentFrame;
                var receivedFrame = Adapter.LastReceivedFrame;
                
                if (sentFrame != null)
                {
                    _logService.Debug($"发送帧: {BitConverter.ToString(sentFrame)}");
                }
                if (receivedFrame != null)
                {
                    _logService.Debug($"接收帧: {BitConverter.ToString(receivedFrame)}");
                }
                
                if (!success)
                {
                    _logService.Warning("写入失败");
                    return Result.FailureResult(Error.ReadWriteError("写入失败"));
                }

                _logService.Info("写入成功");
                return Result.SuccessResult();
            }
            catch (System.Exception ex)
            {
                _logService.Error("写入数据异常", ex);
                return Result.FailureResult(Error.ReadWriteError($"写入失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 批量读取数据
        /// </summary>
        /// <param name="readRequests">读取请求列表</param>
        /// <returns>批量读取结果</returns>
        public async Task<Result<object[]>> BatchReadAsync(BatchReadRequest[] readRequests)
        {
            _logService.Info($"开始批量读取数据，共 {readRequests.Length} 个请求");
            
            try
            {
                if (Adapter == null || !Adapter.IsConnected)
                {
                    _logService.Warning("设备未连接");
                    return Result<object[]>.FailureResult(Error.ConnectionError("设备未连接"));
                }

                var results = new object[readRequests.Length];
                for (int i = 0; i < readRequests.Length; i++)
                {
                    var request = readRequests[i];
                    _logService.Info($"读取第 {i+1} 个请求", $"地址: {request.Address}, 数据类型: {request.DataType}, 长度: {request.Length}");
                    
                    results[i] = await Task.Run(() => Adapter.Read(request.Address, request.DataType.ToString(), request.Length));
                    
                    // 记录通讯帧
                    var sentFrame = Adapter.LastSentFrame;
                    var receivedFrame = Adapter.LastReceivedFrame;
                    
                    if (sentFrame != null)
                    {
                        _logService.Debug($"发送帧: {BitConverter.ToString(sentFrame)}");
                    }
                    if (receivedFrame != null)
                    {
                        _logService.Debug($"接收帧: {BitConverter.ToString(receivedFrame)}");
                    }
                    
                    _logService.Info($"第 {i+1} 个请求读取成功: {results[i]}");
                }

                _logService.Info("批量读取完成");
                return Result<object[]>.SuccessResult(results);
            }
            catch (System.Exception ex)
            {
                _logService.Error("批量读取数据异常", ex);
                return Result<object[]>.FailureResult(Error.ReadWriteError($"批量读取失败: {ex.Message}"));
            }
        }

        /// <summary>
        /// 批量写入数据
        /// </summary>
        /// <param name="writeRequests">写入请求列表</param>
        /// <returns>批量写入结果</returns>
        public async Task<Result> BatchWriteAsync(BatchWriteRequest[] writeRequests)
        {
            _logService.Info($"开始批量写入数据，共 {writeRequests.Length} 个请求");
            
            try
            {
                if (Adapter == null || !Adapter.IsConnected)
                {
                    _logService.Warning("设备未连接");
                    return Result.FailureResult(Error.ConnectionError("设备未连接"));
                }

                for (int i = 0; i < writeRequests.Length; i++)
                {
                    var request = writeRequests[i];
                    _logService.Info($"写入第 {i+1} 个请求", $"地址: {request.Address}, 数据类型: {request.DataType}, 值: {request.Value}");
                    
                    var success = await Task.Run(() => Adapter.Write(request.Address, request.DataType.ToString(), request.Value));
                    
                    // 记录通讯帧
                    var sentFrame = Adapter.LastSentFrame;
                    var receivedFrame = Adapter.LastReceivedFrame;
                    
                    if (sentFrame != null)
                    {
                        _logService.Debug($"发送帧: {BitConverter.ToString(sentFrame)}");
                    }
                    if (receivedFrame != null)
                    {
                        _logService.Debug($"接收帧: {BitConverter.ToString(receivedFrame)}");
                    }
                    
                    if (!success)
                    {
                        _logService.Warning($"写入地址 {request.Address} 失败");
                        return Result.FailureResult(Error.ReadWriteError($"写入地址 {request.Address} 失败"));
                    }
                    
                    _logService.Info($"第 {i+1} 个请求写入成功");
                }

                _logService.Info("批量写入完成");
                return Result.SuccessResult();
            }
            catch (System.Exception ex)
            {
                _logService.Error("批量写入数据异常", ex);
                return Result.FailureResult(Error.ReadWriteError($"批量写入失败: {ex.Message}"));
            }
        }
    }
}
