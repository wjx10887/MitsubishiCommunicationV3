using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HslCommunication.BasicFramework;
using MelsecPLCCommunicator.Domain.Models;
using MelsecPLCCommunicator.Infrastructure.Services;

namespace MelsecPLCCommunicator.Application.Services
{
    /// <summary>
    /// 历史查询服务
    /// 用于查询和导出PLC变量的读取历史
    /// </summary>
    public class HistoryQueryService : IDisposable
    {
        /// <summary>
        /// 数据库服务
        /// </summary>
        private readonly DatabaseService _databaseService;

        /// <summary>
        /// CSV导出器
        /// </summary>
        private readonly CsvExporter _csvExporter;

        /// <summary>
        /// 是否已 disposed
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="databaseService">数据库服务</param>
        public HistoryQueryService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _csvExporter = new CsvExporter();
        }

        /// <summary>
        /// 查询读取记录
        /// </summary>
        /// <param name="variableId">变量ID</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="pageIndex">页码</param>
        /// <returns>读取记录列表</returns>
        public List<ReadRecord> QueryReadRecords(int? variableId = null, DateTime? startTime = null, DateTime? endTime = null, int pageSize = 100, int pageIndex = 1)
        {
            return _databaseService.QueryReadRecords(variableId, startTime, endTime, pageSize, pageIndex);
        }

        /// <summary>
        /// 聚合查询
        /// </summary>
        /// <param name="variableId">变量ID</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="interval">聚合间隔（hour, day, month）</param>
        /// <returns>聚合结果</returns>
        public List<DatabaseService.AggregatedData> AggregateQuery(int variableId, DateTime startTime, DateTime endTime, string interval)
        {
            return _databaseService.AggregateQuery(variableId, startTime, endTime, interval);
        }

        /// <summary>
        /// 导出读取记录到CSV文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="variableId">变量ID</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>导出是否成功</returns>
        public bool ExportReadRecordsToCsv(string filePath, int? variableId = null, DateTime? startTime = null, DateTime? endTime = null, Action<int, int> progressCallback = null)
        {
            try
            {
                // 分批查询数据
                const int pageSize = 1000;
                int pageIndex = 1;
                int totalCount = 0;
                int exportedCount = 0;

                // 第一次查询获取总记录数
                var firstPage = _databaseService.QueryReadRecords(variableId, startTime, endTime, pageSize, pageIndex);
                if (firstPage.Count == 0)
                {
                    // 没有数据，创建空文件
                    _csvExporter.ExportReadRecords(new List<ReadRecord>(), filePath);
                    return true;
                }

                // 估算总记录数（实际项目中应该查询总记录数）
                totalCount = firstPage.Count * 10; // 临时估算

                // 导出第一批数据
                _csvExporter.ExportReadRecords(firstPage, filePath);
                exportedCount += firstPage.Count;
                progressCallback?.Invoke(exportedCount, totalCount);

                // 继续导出剩余数据
                pageIndex++;
                while (true)
                {
                    var nextPage = _databaseService.QueryReadRecords(variableId, startTime, endTime, pageSize, pageIndex);
                    if (nextPage.Count == 0)
                    {
                        break;
                    }

                    _csvExporter.ExportReadRecords(nextPage, filePath);
                    exportedCount += nextPage.Count;
                    progressCallback?.Invoke(exportedCount, totalCount);
                    pageIndex++;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出CSV文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 导出聚合数据到CSV文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="variableId">变量ID</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="interval">聚合间隔</param>
        /// <returns>导出是否成功</returns>
        public bool ExportAggregatedDataToCsv(string filePath, int variableId, DateTime startTime, DateTime endTime, string interval)
        {
            try
            {
                var aggregatedData = _databaseService.AggregateQuery(variableId, startTime, endTime, interval);
                // 转换为CsvExporter.AggregatedData类型
                var csvAggregatedData = new List<CsvExporter.AggregatedData>();
                foreach (var data in aggregatedData)
                {
                    csvAggregatedData.Add(new CsvExporter.AggregatedData
                    {
                        TimePeriod = data.TimePeriod,
                        AvgValue = data.AvgValue,
                        MaxValue = data.MaxValue,
                        MinValue = data.MinValue,
                        Count = data.Count
                    });
                }
                _csvExporter.ExportAggregatedData(csvAggregatedData, filePath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导出聚合数据失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 执行数据归档
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        /// <returns>归档是否成功</returns>
        public bool ArchiveData(int daysToKeep)
        {
            return _databaseService.ArchiveData(daysToKeep);
        }

        /// <summary>
        /// 查询报警日志
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="pageIndex">页码</param>
        /// <returns>报警日志列表</returns>
        public List<AlarmLog> QueryAlarmLogs(DateTime? startTime = null, DateTime? endTime = null, int pageSize = 100, int pageIndex = 1)
        {
            return _databaseService.QueryAlarmLogs(startTime, endTime, pageSize, pageIndex);
        }

        /// <summary>
        /// 根据地址获取或创建变量
        /// </summary>
        /// <param name="address">变量地址</param>
        /// <returns>变量</returns>
        public PlcVariable GetOrCreateVariable(string address)
        {
            // 尝试从数据库获取变量
            var variable = _databaseService.GetVariableByAddress(address);
            if (variable != null)
            {
                return variable;
            }
            // 变量不存在，记录日志或抛出自定义异常
            throw new InvalidOperationException($"变量 {address} 不存在");
        }

        /// <summary>
        /// 根据地址前缀自动判断数据类型
        /// </summary>
        /// <param name="address">变量地址</param>
        /// <returns>数据类型</returns>
        private string GetDataTypeByAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return "INT";
            }

            // 验证地址格式
            if (!IsValidAddress(address))
            {
                return "INT";
            }

            // 根据地址前缀判断数据类型
            string addressPrefix = address.Substring(0, 1).ToUpper();
            switch (addressPrefix)
            {
                case "M": // 中间寄存器
                case "X": // 输入寄存器
                case "Y": // 输出寄存器
                case "B": // 连接继电器
                case "S": // 状态寄存器
                case "F": // 报警器
                    return "BOOL";
                case "D": // 数据寄存器
                case "W": // 链接寄存器
                case "R": // 文件寄存器
                case "TN": // 定时器当前值
                case "CN": // 计数器当前值
                    return "INT";
                case "Float":
                    return "FLOAT";

                default:
                    return "INT";
            }
        }

        /// <summary>
        /// 验证地址格式是否正确
        /// </summary>
        /// <param name="address">变量地址</param>
        /// <returns>地址格式是否正确</returns>
        private bool IsValidAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            // 地址格式：字母 + 数字
            string pattern = @"^[A-Za-z]+\d+$";
            return System.Text.RegularExpressions.Regex.IsMatch(address, pattern);
        }

        /// <summary>
        /// 添加示例数据
        /// </summary>
        /// <param name="variable">变量</param>
        private void AddSampleData(PlcVariable variable)
        {
            // 创建示例数据
            var records = new List<ReadRecord>();
            var now = DateTime.Now;
            
            for (int i = 0; i < 10; i++)
            {
                records.Add(new ReadRecord
                {
                    VariableId = variable.Id,
                    Timestamp = now.AddMinutes(-i),
                    RawValue = (100 + i).ToString(),
                    EngineeringValue = 100 + i,
                    QualityFlag = true,
                    Variable = variable
                });
            }
            
            // 插入示例数据
            _databaseService.InsertReadRecords(records);
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
                    // 释放资源
                }

                _disposed = true;
            }
        }
    }
}