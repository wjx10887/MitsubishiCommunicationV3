# 核心类UML关系图

## 类关系描述

```
┌───────────────────┐       ┌───────────────────┐
│   PlcDataLogger  │◄──────┤ IPlcDataLogger   │
└───────────────────┘       └───────────────────┘
        │
        │ 1..*
        ▼
┌───────────────────┐
│  DataRecordQueue  │
└───────────────────┘
        │
        │ 1
        ▼
┌───────────────────┐       ┌───────────────────┐
│ DatabaseService   │◄──────┤ IDatabaseService  │
└───────────────────┘       └───────────────────┘
        │
        │ 1
        ▼
┌───────────────────┐
│ ConnectionPool    │
└───────────────────┘

┌───────────────────┐       ┌───────────────────┐
│ HistoryQueryService │◄─────┤ IHistoryQueryService │
└───────────────────┘       └───────────────────┘
        │
        │ 1
        ▼
┌───────────────────┐
│ CsvExporter       │
└───────────────────┘

┌───────────────────┐       ┌───────────────────┐
│ PlcVariable       │◄──────┤ IPlcVariable     │
└───────────────────┘       └───────────────────┘

┌───────────────────┐       ┌───────────────────┐
│ ReadRecord        │◄──────┤ IReadRecord      │
└───────────────────┘       └───────────────────┘

┌───────────────────┐       ┌───────────────────┐
│ AlarmLog          │◄──────┤ IAlarmLog        │
└───────────────────┘       └───────────────────┘
```

## 依赖关系

1. **PlcDataLogger** 依赖于：
   - `DataRecordQueue`：用于缓冲待写入的数据
   - `DatabaseService`：用于执行数据库操作
   - `IPlcReadWriteService`：用于订阅PLC数据读取事件

2. **HistoryQueryService** 依赖于：
   - `DatabaseService`：用于执行数据库查询
   - `CsvExporter`：用于导出数据到CSV文件

3. **DatabaseService** 依赖于：
   - `ConnectionPool`：用于管理数据库连接
   - `MySql.Data` 或 `Dapper`：用于执行SQL语句

4. **PlcVariable**：
   - 表示PLC变量的配置信息
   - 包含地址、名称、数据类型、采样周期等属性

5. **ReadRecord**：
   - 表示一次PLC变量的读取记录
   - 包含时间戳、变量ID、原始值、工程值、质量标志等属性

6. **AlarmLog**：
   - 表示通讯异常记录
   - 包含时间戳、错误代码、错误信息、严重程度等属性

## 事件订阅模式

- `PlcDataLogger` 通过事件订阅模式与现有PLC通讯代码解耦
- 当PLC变量被读取时，触发事件，`PlcDataLogger` 接收事件并记录数据
- 这种方式确保了数据记录功能与PLC通讯逻辑的分离

## 数据流

1. PLC通讯模块读取变量值
2. 触发数据读取事件
3. `PlcDataLogger` 接收事件，将数据添加到 `DataRecordQueue`
4. 后台线程批量处理队列中的数据
5. 通过 `DatabaseService` 将数据写入MySQL数据库
6. `HistoryQueryService` 提供查询和导出功能
