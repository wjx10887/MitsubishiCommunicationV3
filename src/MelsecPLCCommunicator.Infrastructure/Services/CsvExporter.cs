using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MelsecPLCCommunicator.Domain.Models;

namespace MelsecPLCCommunicator.Infrastructure.Services
{
    /// <summary>
    /// CSV导出器
    /// 用于将查询结果导出到CSV文件
    /// </summary>
    public class CsvExporter
    {
        /// <summary>
        /// 导出读取记录到CSV文件
        /// </summary>
        /// <param name="records">读取记录列表</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>导出是否成功</returns>
        public bool ExportReadRecords(List<ReadRecord> records, string filePath, Action<int, int> progressCallback = null)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(true))) // 带BOM头
                {
                    // 写入表头
                    writer.WriteLine("时间戳,变量地址,变量名称,数据类型,原始值,工程值,工程单位,质量标志");

                    // 写入数据
                    for (int i = 0; i < records.Count; i++)
                    {
                        var record = records[i];
                        var line = $"{record.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{record.Variable?.Address ?? ""},{record.Variable?.Name ?? ""},{record.Variable?.DataType ?? ""},{record.RawValue},{record.EngineeringValue?.ToString("F4") ?? ""},{record.Variable?.Unit ?? ""},{(record.QualityFlag ? "良好" : "异常")}";
                        writer.WriteLine(line);

                        // 调用进度回调
                        progressCallback?.Invoke(i + 1, records.Count);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // 记录错误
                Console.WriteLine($"导出CSV失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 聚合数据模型
        /// </summary>
        public class AggregatedData
        {
            public string TimePeriod { get; set; }
            public double? AvgValue { get; set; }
            public double? MaxValue { get; set; }
            public double? MinValue { get; set; }
            public int Count { get; set; }
        }

        /// <summary>
        /// 导出聚合数据到CSV文件
        /// </summary>
        /// <param name="aggregatedData">聚合数据</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>导出是否成功</returns>
        public bool ExportAggregatedData(List<AggregatedData> aggregatedData, string filePath, Action<int, int> progressCallback = null)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(true))) // 带BOM头
                {
                    // 写入表头
                    writer.WriteLine("时间段,平均值,最大值,最小值,记录数");

                    // 写入数据
                    for (int i = 0; i < aggregatedData.Count; i++)
                    {
                        var data = aggregatedData[i];
                        var line = $"{data.TimePeriod},{data.AvgValue?.ToString("F4") ?? ""},{data.MaxValue?.ToString("F4") ?? ""},{data.MinValue?.ToString("F4") ?? ""},{data.Count}";
                        writer.WriteLine(line);

                        // 调用进度回调
                        progressCallback?.Invoke(i + 1, aggregatedData.Count);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // 记录错误
                Console.WriteLine($"导出CSV失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 导出报警记录到CSV文件
        /// </summary>
        /// <param name="alarmLogs">报警记录列表</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>导出是否成功</returns>
        public bool ExportAlarmLogs(List<AlarmLog> alarmLogs, string filePath, Action<int, int> progressCallback = null)
        {
            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(true))) // 带BOM头
                {
                    // 写入表头
                    writer.WriteLine("时间戳,错误代码,错误信息,严重程度,是否已解决,解决时间");

                    // 写入数据
                    for (int i = 0; i < alarmLogs.Count; i++)
                    {
                        var log = alarmLogs[i];
                        string severity;
                        switch (log.Severity)
                        {
                            case 1:
                                severity = "警告";
                                break;
                            case 2:
                                severity = "错误";
                                break;
                            case 3:
                                severity = "严重";
                                break;
                            default:
                                severity = "未知";
                                break;
                        }
                        var line = $"{log.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{log.ErrorCode},{EscapeCsvValue(log.ErrorMessage)},{severity},{(log.IsResolved ? "是" : "否")},{log.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}";
                        writer.WriteLine(line);

                        // 调用进度回调
                        progressCallback?.Invoke(i + 1, alarmLogs.Count);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                // 记录错误
                Console.WriteLine($"导出CSV失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 转义CSV值
        /// </summary>
        /// <param name="value">原始值</param>
        /// <returns>转义后的值</returns>
        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            // 如果包含逗号、引号或换行符，需要用引号包围
            if (value.Contains(",") || value.Contains("\n") || value.Contains("\""))
            {
                // 替换引号为双引号
                value = value.Replace("\"", "\"\"");
                // 用引号包围
                return $"\"{value}\"";
            }

            return value;
        }
    }
}
