using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using MelsecPLCCommunicator.Domain.Models;

namespace MelsecPLCCommunicator.Infrastructure.Services
{
    /// <summary>
    /// 数据库服务
    /// 用于执行数据库操作
    /// </summary>
    public class DatabaseService : IDisposable
    {
        /// <summary>
        /// 连接池
        /// </summary>
        private readonly ConnectionPool _connectionPool;

        /// <summary>
        /// 是否已 disposed
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        public DatabaseService(string connectionString)
        {
            _connectionPool = new ConnectionPool(connectionString);
            InitializeDatabase();
        }

        /// <summary>
        /// 初始化数据库
        /// 首次连接时创建必要的表结构
        /// </summary>
        private void InitializeDatabase()
        {
            var connection = _connectionPool.GetConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    // 创建变量配置表
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS plc_variables (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        address TEXT NOT NULL UNIQUE,
                        name TEXT NOT NULL,
                        data_type TEXT NOT NULL,
                        sampling_period INTEGER NOT NULL DEFAULT 1000,
                        unit TEXT,
                        min_value REAL,
                        max_value REAL,
                        scale_factor REAL DEFAULT 1.0,
                        offset REAL DEFAULT 0.0,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    )";
                    command.ExecuteNonQuery();

                    // 创建读取记录表
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS read_history (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        variable_id INTEGER NOT NULL,
                        timestamp DATETIME NOT NULL,
                        raw_value TEXT NOT NULL,
                        engineering_value REAL,
                        quality_flag BOOLEAN DEFAULT TRUE,
                        FOREIGN KEY (variable_id) REFERENCES plc_variables(id)
                    )";
                    command.ExecuteNonQuery();

                    // 创建报警日志表
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS alarm_logs (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        timestamp DATETIME NOT NULL,
                        error_code TEXT NOT NULL,
                        error_message TEXT NOT NULL,
                        severity TEXT NOT NULL,
                        is_resolved BOOLEAN DEFAULT FALSE,
                        resolved_at DATETIME
                    )";
                    command.ExecuteNonQuery();

                    // 创建历史归档表
                    command.CommandText = @"CREATE TABLE IF NOT EXISTS read_history_archive (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        variable_id INTEGER NOT NULL,
                        timestamp DATETIME NOT NULL,
                        raw_value TEXT NOT NULL,
                        engineering_value REAL,
                        quality_flag BOOLEAN DEFAULT TRUE,
                        archived_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (variable_id) REFERENCES plc_variables(id)
                    )";
                    command.ExecuteNonQuery();

                    // 创建索引
                    command.CommandText = "CREATE INDEX IF NOT EXISTS idx_read_history_variable_id ON read_history(variable_id)";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE INDEX IF NOT EXISTS idx_read_history_timestamp ON read_history(timestamp)";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE INDEX IF NOT EXISTS idx_alarm_logs_timestamp ON alarm_logs(timestamp)";
                    command.ExecuteNonQuery();

                    command.CommandText = "CREATE INDEX IF NOT EXISTS idx_read_history_archive_timestamp ON read_history_archive(timestamp)";
                    command.ExecuteNonQuery();

                    // 为 plc_variables 表的 address 字段添加索引，提高查询性能
                    command.CommandText = "CREATE INDEX IF NOT EXISTS idx_plc_variables_address ON plc_variables(address)";
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
        }

        /// <summary>
        /// 批量插入读取记录
        /// </summary>
        /// <param name="records">读取记录列表</param>
        /// <returns>插入成功的记录数</returns>
        public int InsertReadRecords(List<ReadRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                return 0;
            }

            var connection = _connectionPool.GetConnection();
            try
            {
                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = @"INSERT INTO read_history (variable_id, timestamp, raw_value, engineering_value, quality_flag) 
                                               VALUES (@VariableId, @Timestamp, @RawValue, @EngineeringValue, @QualityFlag)";

                        var variableIdParam = command.CreateParameter();
                        variableIdParam.ParameterName = "@VariableId";

                        var timestampParam = command.CreateParameter();
                        timestampParam.ParameterName = "@Timestamp";

                        var rawValueParam = command.CreateParameter();
                        rawValueParam.ParameterName = "@RawValue";

                        var engineeringValueParam = command.CreateParameter();
                        engineeringValueParam.ParameterName = "@EngineeringValue";

                        var qualityFlagParam = command.CreateParameter();
                        qualityFlagParam.ParameterName = "@QualityFlag";

                        command.Parameters.Add(variableIdParam);
                        command.Parameters.Add(timestampParam);
                        command.Parameters.Add(rawValueParam);
                        command.Parameters.Add(engineeringValueParam);
                        command.Parameters.Add(qualityFlagParam);

                        int count = 0;
                        foreach (var record in records)
                        {
                            variableIdParam.Value = record.VariableId;
                            timestampParam.Value = record.Timestamp;
                            rawValueParam.Value = record.RawValue;
                            engineeringValueParam.Value = record.EngineeringValue.HasValue ? (object)record.EngineeringValue.Value : DBNull.Value;
                            qualityFlagParam.Value = record.QualityFlag;

                            count += command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return count;
                    }
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
        }

        /// <summary>
        /// 插入报警记录
        /// </summary>
        /// <param name="alarmLog">报警记录</param>
        /// <returns>插入是否成功</returns>
        public bool InsertAlarmLog(AlarmLog alarmLog)
        {
            var connection = _connectionPool.GetConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"INSERT INTO alarm_logs (timestamp, error_code, error_message, severity, is_resolved, resolved_at) 
                                           VALUES (@Timestamp, @ErrorCode, @ErrorMessage, @Severity, @IsResolved, @ResolvedAt)";

                    var timestampParam = command.CreateParameter();
                    timestampParam.ParameterName = "@Timestamp";
                    timestampParam.Value = alarmLog.Timestamp;
                    command.Parameters.Add(timestampParam);

                    var errorCodeParam = command.CreateParameter();
                    errorCodeParam.ParameterName = "@ErrorCode";
                    errorCodeParam.Value = alarmLog.ErrorCode;
                    command.Parameters.Add(errorCodeParam);

                    var errorMessageParam = command.CreateParameter();
                    errorMessageParam.ParameterName = "@ErrorMessage";
                    errorMessageParam.Value = alarmLog.ErrorMessage;
                    command.Parameters.Add(errorMessageParam);

                    var severityParam = command.CreateParameter();
                    severityParam.ParameterName = "@Severity";
                    severityParam.Value = alarmLog.Severity;
                    command.Parameters.Add(severityParam);

                    var isResolvedParam = command.CreateParameter();
                    isResolvedParam.ParameterName = "@IsResolved";
                    isResolvedParam.Value = alarmLog.IsResolved;
                    command.Parameters.Add(isResolvedParam);

                    var resolvedAtParam = command.CreateParameter();
                    resolvedAtParam.ParameterName = "@ResolvedAt";
                    resolvedAtParam.Value = alarmLog.ResolvedAt.HasValue ? (object)alarmLog.ResolvedAt.Value : DBNull.Value;
                    command.Parameters.Add(resolvedAtParam);

                    var result = command.ExecuteNonQuery();
                    return result > 0;
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
        }

        /// <summary>
        /// 获取所有变量
        /// </summary>
        /// <returns>变量列表</returns>
        public List<PlcVariable> GetAllVariables()
        {
            var connection = _connectionPool.GetConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM plc_variables ORDER BY id";

                    var variables = new List<PlcVariable>();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            variables.Add(new PlcVariable
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Address = reader.GetString(reader.GetOrdinal("address")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                DataType = reader.GetString(reader.GetOrdinal("data_type")),
                                SamplingPeriod = reader.GetInt32(reader.GetOrdinal("sampling_period")),
                                Unit = reader.IsDBNull(reader.GetOrdinal("unit")) ? null : reader.GetString(reader.GetOrdinal("unit")),
                                MinValue = reader.IsDBNull(reader.GetOrdinal("min_value")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("min_value")),
                                MaxValue = reader.IsDBNull(reader.GetOrdinal("max_value")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("max_value")),
                                ScaleFactor = reader.IsDBNull(reader.GetOrdinal("scale_factor")) ? 1.0 : reader.GetDouble(reader.GetOrdinal("scale_factor")),
                                Offset = reader.IsDBNull(reader.GetOrdinal("offset")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("offset")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            });
                        }
                    }

                    return variables;
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
        }

        /// <summary>
        /// 根据地址获取变量
        /// </summary>
        /// <param name="address">变量地址</param>
        /// <returns>变量</returns>
        public PlcVariable GetVariableByAddress(string address)
        {
            var connection = _connectionPool.GetConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM plc_variables WHERE LOWER(address) = LOWER(@Address)";

                    var addressParam = command.CreateParameter();
                    addressParam.ParameterName = "@Address";
                    addressParam.Value = address;
                    command.Parameters.Add(addressParam);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new PlcVariable
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Address = reader.GetString(reader.GetOrdinal("address")),
                                Name = reader.GetString(reader.GetOrdinal("name")),
                                DataType = reader.GetString(reader.GetOrdinal("data_type")),
                                SamplingPeriod = reader.GetInt32(reader.GetOrdinal("sampling_period")),
                                Unit = reader.IsDBNull(reader.GetOrdinal("unit")) ? null : reader.GetString(reader.GetOrdinal("unit")),
                                MinValue = reader.IsDBNull(reader.GetOrdinal("min_value")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("min_value")),
                                MaxValue = reader.IsDBNull(reader.GetOrdinal("max_value")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("max_value")),
                                ScaleFactor = reader.IsDBNull(reader.GetOrdinal("scale_factor")) ? 1.0 : reader.GetDouble(reader.GetOrdinal("scale_factor")),
                                Offset = reader.IsDBNull(reader.GetOrdinal("offset")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("offset")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                            };
                        }
                    }

                    return null;
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
        }

        /// <summary>
        /// 插入变量
        /// </summary>
        /// <param name="variable">变量</param>
        /// <returns>插入是否成功</returns>
        public bool InsertVariable(PlcVariable variable)
        {
            var connection = _connectionPool.GetConnection();
            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"INSERT INTO plc_variables (address, name, data_type, sampling_period, unit, min_value, max_value, scale_factor, offset, created_at, updated_at) 
                                           VALUES (@Address, @Name, @DataType, @SamplingPeriod, @Unit, @MinValue, @MaxValue, @ScaleFactor, @Offset, @CreatedAt, @UpdatedAt);
                                           SELECT last_insert_rowid();";

                    var addressParam = command.CreateParameter();
                    addressParam.ParameterName = "@Address";
                    addressParam.Value = variable.Address;
                    command.Parameters.Add(addressParam);

                    var nameParam = command.CreateParameter();
                    nameParam.ParameterName = "@Name";
                    nameParam.Value = variable.Name;
                    command.Parameters.Add(nameParam);

                    var dataTypeParam = command.CreateParameter();
                    dataTypeParam.ParameterName = "@DataType";
                    dataTypeParam.Value = variable.DataType;
                    command.Parameters.Add(dataTypeParam);

                    var samplingPeriodParam = command.CreateParameter();
                    samplingPeriodParam.ParameterName = "@SamplingPeriod";
                    samplingPeriodParam.Value = variable.SamplingPeriod;
                    command.Parameters.Add(samplingPeriodParam);

                    var unitParam = command.CreateParameter();
                    unitParam.ParameterName = "@Unit";
                    unitParam.Value = (object)(string.IsNullOrEmpty(variable.Unit) ? null : variable.Unit) ?? DBNull.Value;
                    command.Parameters.Add(unitParam);

                    var minValueParam = command.CreateParameter();
                    minValueParam.ParameterName = "@MinValue";
                    minValueParam.Value = variable.MinValue.HasValue ? (object)variable.MinValue.Value : DBNull.Value;
                    command.Parameters.Add(minValueParam);

                    var maxValueParam = command.CreateParameter();
                    maxValueParam.ParameterName = "@MaxValue";
                    maxValueParam.Value = variable.MaxValue.HasValue ? (object)variable.MaxValue.Value : DBNull.Value;
                    command.Parameters.Add(maxValueParam);

                    var scaleFactorParam = command.CreateParameter();
                    scaleFactorParam.ParameterName = "@ScaleFactor";
                    scaleFactorParam.Value = variable.ScaleFactor;
                    command.Parameters.Add(scaleFactorParam);

                    var offsetParam = command.CreateParameter();
                    offsetParam.ParameterName = "@Offset";
                    offsetParam.Value = variable.Offset;
                    command.Parameters.Add(offsetParam);

                    var createdAtParam = command.CreateParameter();
                    createdAtParam.ParameterName = "@CreatedAt";
                    createdAtParam.Value = DateTime.Now;
                    command.Parameters.Add(createdAtParam);

                    var updatedAtParam = command.CreateParameter();
                    updatedAtParam.ParameterName = "@UpdatedAt";
                    updatedAtParam.Value = DateTime.Now;
                    command.Parameters.Add(updatedAtParam);

                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        variable.Id = Convert.ToInt32(Convert.ToInt64(result));
                        return true;
                    }
                    return false;
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
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
            var connection = _connectionPool.GetConnection();
            try
            {
                var sql = @"SELECT r.id as record_id, r.variable_id, r.timestamp, r.raw_value, r.engineering_value, r.quality_flag, 
                           v.id as var_id, v.address, v.name, v.data_type, v.sampling_period, v.unit, 
                           v.min_value, v.max_value, v.scale_factor, v.offset, v.created_at, v.updated_at 
                           FROM read_history r 
                           LEFT JOIN plc_variables v ON r.variable_id = v.id 
                           WHERE 1=1";

                using (var command = connection.CreateCommand())
                {
                    if (variableId.HasValue)
                    {
                        sql += " AND r.variable_id = @VariableId";
                        var variableIdParam = command.CreateParameter();
                        variableIdParam.ParameterName = "@VariableId";
                        variableIdParam.Value = variableId.Value;
                        command.Parameters.Add(variableIdParam);
                    }

                    if (startTime.HasValue)
                    {
                        sql += " AND r.timestamp >= @StartTime";
                        var startTimeParam = command.CreateParameter();
                        startTimeParam.ParameterName = "@StartTime";
                        startTimeParam.Value = startTime.Value;
                        command.Parameters.Add(startTimeParam);
                    }

                    if (endTime.HasValue)
                    {
                        sql += " AND r.timestamp <= @EndTime";
                        var endTimeParam = command.CreateParameter();
                        endTimeParam.ParameterName = "@EndTime";
                        endTimeParam.Value = endTime.Value;
                        command.Parameters.Add(endTimeParam);
                    }

                    sql += " ORDER BY r.timestamp DESC LIMIT @Offset, @Limit";
                    var offsetParam = command.CreateParameter();
                    offsetParam.ParameterName = "@Offset";
                    offsetParam.Value = (pageIndex - 1) * pageSize;
                    command.Parameters.Add(offsetParam);

                    var limitParam = command.CreateParameter();
                    limitParam.ParameterName = "@Limit";
                    limitParam.Value = pageSize;
                    command.Parameters.Add(limitParam);

                    command.CommandText = sql;

                    var records = new List<ReadRecord>();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var record = new ReadRecord
                            {
                                Id = reader.GetInt64(reader.GetOrdinal("record_id")),
                                VariableId = reader.GetInt32(reader.GetOrdinal("variable_id")),
                                Timestamp = reader.GetDateTime(reader.GetOrdinal("timestamp")),
                                RawValue = reader.GetString(reader.GetOrdinal("raw_value")),
                                EngineeringValue = reader.IsDBNull(reader.GetOrdinal("engineering_value")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("engineering_value")),
                                QualityFlag = reader.GetBoolean(reader.GetOrdinal("quality_flag"))
                            };

                            // 读取变量信息
                            try
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("var_id")))
                                {
                                    record.Variable = new PlcVariable
                                    {
                                        Id = reader.GetInt32(reader.GetOrdinal("var_id")),
                                        Address = reader.GetString(reader.GetOrdinal("address")),
                                        Name = reader.GetString(reader.GetOrdinal("name")),
                                        DataType = reader.GetString(reader.GetOrdinal("data_type")),
                                        SamplingPeriod = reader.GetInt32(reader.GetOrdinal("sampling_period")),
                                        Unit = reader.IsDBNull(reader.GetOrdinal("unit")) ? null : reader.GetString(reader.GetOrdinal("unit")),
                                        MinValue = reader.IsDBNull(reader.GetOrdinal("min_value")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("min_value")),
                                        MaxValue = reader.IsDBNull(reader.GetOrdinal("max_value")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("max_value")),
                                        ScaleFactor = reader.IsDBNull(reader.GetOrdinal("scale_factor")) ? 1.0 : reader.GetDouble(reader.GetOrdinal("scale_factor")),
                                        Offset = reader.IsDBNull(reader.GetOrdinal("offset")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("offset")),
                                        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
                                    };
                                }
                            }
                            catch (Exception ex)
                            {
                                // 变量信息读取失败，跳过
                                Console.WriteLine($"读取变量信息失败: {ex.Message}");
                            }

                            records.Add(record);
                        }
                    }

                    return records;
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
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
        /// 聚合查询
        /// </summary>
        /// <param name="variableId">变量ID</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="interval">聚合间隔（hour, day, month）</param>
        /// <returns>聚合结果</returns>
        public List<AggregatedData> AggregateQuery(int variableId, DateTime startTime, DateTime endTime, string interval)
        {
            var connection = _connectionPool.GetConnection();
            try
            {
                string timeFormat;
                switch (interval)
                {
                    case "hour":
                        timeFormat = "%Y-%m-%d %H:00:00";
                        break;
                    case "day":
                        timeFormat = "%Y-%m-%d 00:00:00";
                        break;
                    case "month":
                        timeFormat = "%Y-%m-01 00:00:00";
                        break;
                    default:
                        timeFormat = "%Y-%m-%d %H:00:00";
                        break;
                }

                var sql = $@"SELECT 
                               strftime('{timeFormat}', timestamp) as TimePeriod,
                               AVG(engineering_value) as AvgValue,
                               MAX(engineering_value) as MaxValue,
                               MIN(engineering_value) as MinValue,
                               COUNT(*) as Count
                           FROM read_history 
                           WHERE variable_id = @VariableId 
                           AND timestamp >= @StartTime 
                           AND timestamp <= @EndTime 
                           GROUP BY TimePeriod 
                           ORDER BY TimePeriod";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    var variableIdParam = command.CreateParameter();
                    variableIdParam.ParameterName = "@VariableId";
                    variableIdParam.Value = variableId;
                    command.Parameters.Add(variableIdParam);

                    var startTimeParam = command.CreateParameter();
                    startTimeParam.ParameterName = "@StartTime";
                    startTimeParam.Value = startTime;
                    command.Parameters.Add(startTimeParam);

                    var endTimeParam = command.CreateParameter();
                    endTimeParam.ParameterName = "@EndTime";
                    endTimeParam.Value = endTime;
                    command.Parameters.Add(endTimeParam);

                    var results = new List<AggregatedData>();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new AggregatedData
                            {
                                TimePeriod = reader.GetString(reader.GetOrdinal("TimePeriod")),
                                AvgValue = reader.IsDBNull(reader.GetOrdinal("AvgValue")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("AvgValue")),
                                MaxValue = reader.IsDBNull(reader.GetOrdinal("MaxValue")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("MaxValue")),
                                MinValue = reader.IsDBNull(reader.GetOrdinal("MinValue")) ? (double?)null : reader.GetDouble(reader.GetOrdinal("MinValue")),
                                Count = reader.GetInt32(reader.GetOrdinal("Count"))
                            });
                        }
                    }

                    return results;
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
        }

        /// <summary>
        /// 执行数据归档
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        /// <returns>归档是否成功</returns>
        public bool ArchiveData(int daysToKeep)
        {
            var connection = _connectionPool.GetConnection();
            try
            {
                using (var transaction = connection.BeginTransaction())
                {
                    // 第一步：将旧数据插入到归档表
                    using (var insertCommand = connection.CreateCommand())
                    {
                        insertCommand.Transaction = transaction;
                        insertCommand.CommandText = @"INSERT INTO read_history_archive (variable_id, timestamp, raw_value, engineering_value, quality_flag, archived_at)
                                               SELECT variable_id, timestamp, raw_value, engineering_value, quality_flag, datetime('now')
                                               FROM read_history
                                               WHERE timestamp < datetime('now', '-' || @DaysToKeep || ' days')";

                        var daysToKeepParam = insertCommand.CreateParameter();
                        daysToKeepParam.ParameterName = "@DaysToKeep";
                        daysToKeepParam.Value = daysToKeep;
                        insertCommand.Parameters.Add(daysToKeepParam);

                        insertCommand.ExecuteNonQuery();
                    }

                    // 第二步：从原表中删除旧数据
                    using (var deleteCommand = connection.CreateCommand())
                    {
                        deleteCommand.Transaction = transaction;
                        deleteCommand.CommandText = @"DELETE FROM read_history
                                               WHERE timestamp < datetime('now', '-' || @DaysToKeep || ' days')";

                        var daysToKeepParam = deleteCommand.CreateParameter();
                        daysToKeepParam.ParameterName = "@DaysToKeep";
                        daysToKeepParam.Value = daysToKeep;
                        deleteCommand.Parameters.Add(daysToKeepParam);

                        deleteCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return true;
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
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
            var connection = _connectionPool.GetConnection();
            try
            {
                var sql = @"SELECT id, timestamp, error_code, error_message, severity, is_resolved, resolved_at 
                           FROM alarm_logs 
                           WHERE 1=1";

                using (var command = connection.CreateCommand())
                {
                    if (startTime.HasValue)
                    {
                        sql += " AND timestamp >= @StartTime";
                        var startTimeParam = command.CreateParameter();
                        startTimeParam.ParameterName = "@StartTime";
                        startTimeParam.Value = startTime.Value;
                        command.Parameters.Add(startTimeParam);
                    }

                    if (endTime.HasValue)
                    {
                        sql += " AND timestamp <= @EndTime";
                        var endTimeParam = command.CreateParameter();
                        endTimeParam.ParameterName = "@EndTime";
                        endTimeParam.Value = endTime.Value;
                        command.Parameters.Add(endTimeParam);
                    }

                    sql += " ORDER BY timestamp DESC LIMIT @Offset, @Limit";
                    var offsetParam = command.CreateParameter();
                    offsetParam.ParameterName = "@Offset";
                    offsetParam.Value = (pageIndex - 1) * pageSize;
                    command.Parameters.Add(offsetParam);

                    var limitParam = command.CreateParameter();
                    limitParam.ParameterName = "@Limit";
                    limitParam.Value = pageSize;
                    command.Parameters.Add(limitParam);

                    command.CommandText = sql;

                    var logs = new List<AlarmLog>();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new AlarmLog
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                Timestamp = reader.GetDateTime(reader.GetOrdinal("timestamp")),
                                ErrorCode = reader.GetInt32(reader.GetOrdinal("error_code")),
                                ErrorMessage = reader.GetString(reader.GetOrdinal("error_message")),
                                Severity = reader.GetInt32(reader.GetOrdinal("severity")),
                                IsResolved = reader.GetBoolean(reader.GetOrdinal("is_resolved")),
                                ResolvedAt = reader.IsDBNull(reader.GetOrdinal("resolved_at")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("resolved_at"))
                            });
                        }
                    }

                    return logs;
                }
            }
            finally
            {
                _connectionPool.ReturnConnection(connection);
            }
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
                    _connectionPool.Dispose();
                }

                _disposed = true;
            }
        }
    }
}