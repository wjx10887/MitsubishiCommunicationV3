using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MelsecPLCCommunicator.Application.Interfaces;
using MelsecPLCCommunicator.Domain.DTOs;
using MelsecPLCCommunicator.Domain.Models;
using MelsecPLCCommunicator.Infrastructure.Services;

namespace MelsecPLCCommunicator.Application.Services
{
    /// <summary>
    /// PLC数据记录器
    /// 用于记录PLC变量的读取历史
    /// </summary>
    public class PlcDataLogger : IDisposable
    {
        /// <summary>
        /// PLC读写服务
        /// </summary>
        private readonly IPlcReadWriteService _plcReadWriteService;

        /// <summary>
        /// 数据库服务
        /// </summary>
        private readonly DatabaseService _databaseService;

        /// <summary>
        /// 数据记录队列
        /// </summary>
        private readonly DataRecordQueue _dataRecordQueue;

        /// <summary>
        /// 后台处理线程
        /// </summary>
        private Task _processingTask;

        /// <summary>
        /// 取消令牌源
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 变量配置缓存
        /// </summary>
        private Dictionary<string, PlcVariable> _variableCache;

        /// <summary>
        /// 变量配置缓存更新时间
        /// </summary>
        private DateTime _lastVariableCacheUpdate;

        /// <summary>
        /// 变量配置缓存更新间隔
        /// </summary>
        private readonly TimeSpan _variableCacheUpdateInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// 批量处理大小
        /// </summary>
        private readonly int _batchSize = 1000;

        /// <summary>
        /// 批量处理间隔
        /// </summary>
        private readonly TimeSpan _batchInterval = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// 是否已 disposed
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="plcReadWriteService">PLC读写服务</param>
        /// <param name="databaseService">数据库服务</param>
        /// <param name="dataRecordQueue">数据记录队列</param>
        public PlcDataLogger(IPlcReadWriteService plcReadWriteService, DatabaseService databaseService, DataRecordQueue dataRecordQueue)
        {
            try
            {
                Console.WriteLine("PlcDataLogger: 构造函数开始");
                
                _plcReadWriteService = plcReadWriteService;
                _databaseService = databaseService;
                _dataRecordQueue = dataRecordQueue;
                _cancellationTokenSource = new CancellationTokenSource();
                _variableCache = new Dictionary<string, PlcVariable>();
                _lastVariableCacheUpdate = DateTime.MinValue;

                // 订阅PLC数据读取事件
                _plcReadWriteService.BatchReadCompleted += PlcReadWriteService_BatchReadCompleted;
                Console.WriteLine("PlcDataLogger: 订阅了BatchReadCompleted事件");

                // 启动后台处理线程
                _processingTask = Task.Run(() => ProcessDataRecordsAsync(_cancellationTokenSource.Token));
                Console.WriteLine("PlcDataLogger: 后台处理线程启动");
                
                Console.WriteLine("PlcDataLogger: 构造函数完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PlcDataLogger: 构造函数异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 处理PLC批量读取完成事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void PlcReadWriteService_BatchReadCompleted(object sender, BatchReadCompletedEventArgs e)
        {
            Console.WriteLine("PlcDataLogger: 接收到BatchReadCompleted事件");
            
            if (!e.Success || e.Results == null)
            {
                Console.WriteLine("PlcDataLogger: 事件数据无效，跳过处理");
                return;
            }

            Console.WriteLine($"PlcDataLogger: 处理 {e.Requests.Length} 个读取请求");
            
            // 处理读取的数据
            for (int i = 0; i < e.Requests.Length && i < e.Results.Length; i++)
            {
                var request = e.Requests[i];
                var result = e.Results[i];
                Console.WriteLine($"PlcDataLogger: 处理地址 {request.Address}，值: {result}");

                // 查找变量配置
                PlcVariable variable = null;
                if (!_variableCache.TryGetValue(request.Address, out variable))
                {
                    // 自动创建变量配置
                    var newVariable = new PlcVariable
                    {
                        Address = request.Address,
                        Name = request.Address,
                        DataType = request.DataType.ToString(),
                        SamplingPeriod = 1000,
                        ScaleFactor = 1.0,
                        Offset = 0.0
                    };

                    // 插入到数据库
                    Console.WriteLine($"PlcDataLogger: 尝试插入变量 {request.Address}");
                    try
                    {
                        if (_databaseService.InsertVariable(newVariable))
                        {
                            Console.WriteLine($"PlcDataLogger: 变量 {request.Address} 插入成功，ID: {newVariable.Id}");
                            variable = newVariable;
                            // 重新更新缓存
                            UpdateVariableCache();
                            Console.WriteLine("PlcDataLogger: 变量缓存更新完成");
                        }
                        else
                        {
                            Console.WriteLine($"PlcDataLogger: 变量 {request.Address} 插入失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"PlcDataLogger: 插入变量失败: {ex.Message}");
                        Console.WriteLine($"PlcDataLogger: 内部异常: {ex.InnerException?.Message}");
                        Console.WriteLine($"PlcDataLogger: 堆栈跟踪: {ex.StackTrace}");
                    }

                }
                if (variable != null)
                {
                    // 计算工程值
                double? engineeringValue = null;
                string rawValueStr = result?.ToString();
                
                // 处理数组类型的值
                if (result != null)
                {
                    if (result is Array array)
                    {
                        // 将数组转换为更有意义的字符串表示
                        var arrayValues = new List<string>();
                        for (int j = 0; j < array.Length; j++)
                        {
                            arrayValues.Add(array.GetValue(j)?.ToString() ?? "null");
                        }
                        rawValueStr = "[" + string.Join(", ", arrayValues) + "]";
                    }
                    
                    // 尝试解析为数值计算工程值
                    if (double.TryParse(rawValueStr.Replace("[", "").Replace("]", "").Split(',')[0].Trim(), out var rawValue))
                    {
                        engineeringValue = variable.CalculateEngineeringValue(rawValue);
                    }
                }

                // 创建读取记录
                var record = new ReadRecord
                {
                    VariableId = variable.Id,
                    Timestamp = DateTime.Now,
                    RawValue = rawValueStr,
                    EngineeringValue = engineeringValue,
                    QualityFlag = true
                };

                    Console.WriteLine($"PlcDataLogger: 创建读取记录 - 变量ID: {variable.Id}, 地址: {variable.Address}, 值: {result}, 时间: {record.Timestamp}");
                    
                    // 加入队列
                    _dataRecordQueue.Enqueue(record);
                    Console.WriteLine($"PlcDataLogger: 记录已加入队列，当前队列大小: {_dataRecordQueue.Count}");
                }
            }
        }

        /// <summary>
        /// 后台处理数据记录
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        private async Task ProcessDataRecordsAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("PlcDataLogger: 后台处理线程启动");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 批量出队
                    var records = await _dataRecordQueue.DequeueBatchAsync(_batchSize, cancellationToken);
                    
                    if (records.Count > 0)
                    {
                        Console.WriteLine($"PlcDataLogger: 从队列中获取 {records.Count} 条记录");
                        // 批量插入数据库
                        int insertedCount = _databaseService.InsertReadRecords(records);
                        Console.WriteLine($"PlcDataLogger: 成功插入 {insertedCount} 条记录到数据库");
                    }
                    else
                    {
                        // 没有数据，等待一段时间
                        await Task.Delay(_batchInterval, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 取消操作，退出循环
                    Console.WriteLine("PlcDataLogger: 后台处理线程被取消");
                    break;
                }
                catch (Exception ex)
                {
                    // 记录错误
                    Console.WriteLine($"PlcDataLogger: 处理数据记录失败: {ex.Message}");
                    // 等待一段时间后重试
                    await Task.Delay(_batchInterval, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 更新变量缓存
        /// </summary>
        private void UpdateVariableCache()
        {
            try
            {
                var variables = _databaseService.GetAllVariables();
                var newCache = new Dictionary<string, PlcVariable>();

                foreach (var variable in variables)
                {
                    newCache[variable.Address] = variable;
                }

                _variableCache = newCache;
                _lastVariableCacheUpdate = DateTime.Now;
            }
            catch (Exception ex)
            {
                // 记录错误
                Console.WriteLine($"更新变量缓存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 手动添加读取记录
        /// </summary>
        /// <param name="address">变量地址</param>
        /// <param name="value">变量值</param>
        /// <param name="qualityFlag">质量标志</param>
        public void AddReadRecord(string address, object value, bool qualityFlag = true)
        {
            // 检查变量缓存是否需要更新
            if (DateTime.Now - _lastVariableCacheUpdate > _variableCacheUpdateInterval)
            {
                UpdateVariableCache();
            }

            // 查找变量配置
            if (_variableCache.TryGetValue(address, out var variable))
            {
                // 计算工程值
                double? engineeringValue = null;
                if (double.TryParse(value.ToString(), out var rawValue))
                {
                    engineeringValue = variable.CalculateEngineeringValue(rawValue);
                }

                // 创建读取记录
                var record = new ReadRecord
                {
                    VariableId = variable.Id,
                    Timestamp = DateTime.Now,
                    RawValue = value.ToString(),
                    EngineeringValue = engineeringValue,
                    QualityFlag = qualityFlag
                };

                // 加入队列
                _dataRecordQueue.Enqueue(record);
            }
        }

        /// <summary>
        /// 添加报警记录
        /// </summary>
        /// <param name="errorCode">错误代码</param>
        /// <param name="errorMessage">错误信息</param>
        /// <param name="severity">严重程度</param>
        public void AddAlarmLog(int errorCode, string errorMessage, int severity = 1)
        {
            var alarmLog = new AlarmLog
            {
                Timestamp = DateTime.Now,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                Severity = severity,
                IsResolved = false
            };

            _databaseService.InsertAlarmLog(alarmLog);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 取消事件订阅
                    _plcReadWriteService.BatchReadCompleted -= PlcReadWriteService_BatchReadCompleted;

                    // 取消后台任务
                    _cancellationTokenSource.Cancel();
                    _processingTask.Wait(1000);
                    _cancellationTokenSource.Dispose();

                    // 释放队列
                    _dataRecordQueue.Dispose();

                    // 释放数据库服务
                    _databaseService.Dispose();
                }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 数据读取事件参数
    /// </summary>
    public class DataReadEventArgs : EventArgs
    {
        /// <summary>
        /// 读取的数据
        /// </summary>
        public List<PlcData> Data { get; set; }
    }

    /// <summary>
    /// PLC数据
    /// </summary>
    public class PlcData
    {
        /// <summary>
        /// 变量地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 变量值
        /// </summary>
        public object Value { get; set; }
    }
}
