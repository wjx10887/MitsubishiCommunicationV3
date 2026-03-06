# Mitsubishi PLC Communication V3 技术文档

## 1. 技术架构

### 1.1 分层架构
本项目采用经典的分层架构设计，将应用程序分为四个主要层次：

1. **领域层 (Domain)**：包含核心业务模型和领域逻辑
2. **应用层 (Application)**：包含业务服务和用例
3. **基础设施层 (Infrastructure)**：包含技术实现细节
4. **表示层 (UI)**：包含用户界面和交互逻辑

这种分层架构提供了以下好处：
- **关注点分离**：每个层次只负责特定的功能
- **可维护性**：修改一个层次不会影响其他层次
- **可测试性**：各层次可以独立测试
- **灵活性**：可以轻松替换实现细节

### 1.2 依赖关系
```
UI → Application → Domain ← Infrastructure
```

- **UI层**：依赖于应用层，负责用户交互
- **应用层**：依赖于领域层，实现业务逻辑
- **基础设施层**：依赖于领域层，提供技术实现
- **领域层**：不依赖于其他层，包含核心业务模型

## 2. 核心组件

### 2.1 领域层组件

#### 2.1.1 数据传输对象 (DTOs)
- **ConnectionConfigDto**：包含PLC连接配置信息
  - ConnectionName：连接名称
  - IpAddress：PLC IP地址
  - Port：通讯端口
  - Timeout：超时时间
  - ProtocolType：协议类型

#### 2.1.2 枚举类型
- **DataType**：数据类型枚举
  - 位数据类型：M, X, Y, L, B, TS, CS, TC, CC
  - 字数据类型：D, W, R, ZR, T, C
  - 双字数据类型：D32, Float, F64

#### 2.1.3 共享类
- **Result**：通用结果类型，包含成功/失败状态和数据
- **Error**：错误信息类
- **Guard**：参数验证工具类

### 2.2 应用层组件

#### 2.2.1 服务接口
- **IPlcConnectionService**：PLC连接服务
  - ConnectAsync：连接到PLC
  - DisconnectAsync：断开连接
  - GetConnectionStatus：获取连接状态
  - FrameReceived：通讯帧接收事件

- **IPlcReadWriteService**：PLC读写服务
  - BatchReadAsync：批量读取数据
  - BatchWriteAsync：批量写入数据

- **ILogService**：日志服务
  - Info：记录信息日志
  - Debug：记录调试日志
  - Warning：记录警告日志
  - Error：记录错误日志

- **ISettingsService**：设置服务
  - SaveConnectionConfig：保存连接配置
  - LoadConnectionConfig：加载连接配置

#### 2.2.2 服务实现
- **PlcConnectionService**：实现IPlcConnectionService接口
- **PlcReadWriteService**：实现IPlcReadWriteService接口
- **LogService**：实现ILogService接口
- **SettingsService**：实现ISettingsService接口

### 2.3 基础设施层组件

#### 2.3.1 通讯适配器
- **ICommunicationAdapter**：通讯适配器接口
  - Connect：连接到设备
  - Disconnect：断开连接
  - SendReceive：发送并接收数据
  - IsConnected：检查连接状态

- **CommunicationAdapterFactory**：通讯适配器工厂
  - CreateAdapter：根据配置创建通讯适配器

- **NetworkCommunicationAdapter**：网络通讯适配器
  - 实现基于以太网的通讯

- **SerialCommunicationAdapter**：串口通讯适配器
  - 实现基于串口的通讯

#### 2.3.2 帧解析服务
- **FrameParserService**：通讯帧解析服务
  - ParseMcFrame：解析MC协议通讯帧
  - ParseSendFrameData：解析发送帧数据
  - ParseReceiveFrameData：解析接收帧数据
  - ParseBatchCommandData：解析批量命令数据

### 2.4 表示层组件

#### 2.4.1 窗体
- **MainForm**：主窗体，包含批量操作和监控功能
- **ConnectionConfigForm**：连接配置窗体
- **ConnectionMonitorForm**：连接监控窗体
- **DeviceMonitorForm**：设备监控窗体
- **ErrorCodeLookupForm**：错误码查询窗体
- **FrameAnalyzerForm**：帧分析器窗体

#### 2.4.2 程序入口
- **Program**：应用程序入口点
  - Main：主方法，启动应用程序
  - ConfigureServices：配置依赖注入

## 3. 通讯协议

### 3.1 MC协议
本项目使用三菱MC协议与PLC进行通讯。MC协议是三菱PLC的专用通讯协议，支持多种通讯方式，包括以太网和串口。

### 3.2 帧格式

#### 3.2.1 发送帧格式
```
| 帧头 | 网络号 | 站号 | 预留 | 命令码 | 数据长度 | 数据 |
|------|--------|------|------|--------|----------|------|
| 2字节 | 1字节  | 1字节 | 2字节 | 2字节  | 2字节    | 可变 |
```

#### 3.2.2 接收帧格式
```
| 帧头 | 网络号 | 站号 | 预留 | 命令码 | 数据长度 | 响应码 | 数据 |
|------|--------|------|------|--------|----------|--------|------|
| 2字节 | 1字节  | 1字节 | 2字节 | 2字节  | 2字节    | 2字节  | 可变 |
```

### 3.3 命令码

| 命令码 | 命令名称 | 功能 |
|--------|----------|------|
| 04 01 | 批量读取 | 读取多个软元件的值 |
| 04 02 | 批量写入 | 写入多个软元件的值 |
| 04 03 | 随机读取 | 随机读取软元件 |
| 04 04 | 随机写入 | 随机写入软元件 |
| 04 05 | 远程运行 | 远程启动PLC |
| 04 06 | 远程停止 | 远程停止PLC |
| 04 07 | 远程复位 | 远程复位PLC |

### 3.4 响应码

| 响应码 | 描述 |
|--------|------|
| 00 00 | 正常 |
| 01 00 | 命令错误 |
| 02 00 | 格式错误 |
| 03 00 | 数据范围错误 |
| 04 00 | 数据长度错误 |
| 05 00 | 访问错误 |
| 06 00 | 其他错误 |

## 4. 依赖注入

### 4.1 服务注册
本项目使用Microsoft.Extensions.DependencyInjection进行依赖注入。在Program.cs文件中，ConfigureServices方法负责注册所有服务：

```csharp
private static ServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();

    // 注册基础设施层服务
    services.AddSingleton<ICommunicationAdapterFactory, CommunicationAdapterFactory>();

    // 注册应用层服务
    services.AddSingleton<IPlcConnectionService, PlcConnectionService>();
    services.AddSingleton<IPlcReadWriteService, PlcReadWriteService>();
    services.AddSingleton<ILogService, LogService>();
    services.AddSingleton<ISettingsService, SettingsService>();

    // 注册服务提供者本身
    services.AddSingleton<IServiceProvider>(sp => sp);

    // 注册UI层服务
    services.AddTransient<MainForm>();
    services.AddTransient<ConnectionConfigForm>();
    services.AddTransient<ConnectionMonitorForm>();
    services.AddTransient<DeviceMonitorForm>();
    services.AddTransient<ErrorCodeLookupForm>();
    services.AddTransient<FrameAnalyzerForm>();

    return services.BuildServiceProvider();
}
```

### 4.2 服务生命周期
- **Singleton**：整个应用程序生命周期内只有一个实例
- **Transient**：每次请求时创建新实例

## 5. 核心功能实现

### 5.1 PLC连接管理

#### 5.1.1 连接流程
1. 用户在ConnectionConfigForm中输入连接参数
2. 点击确定按钮，调用PlcConnectionService.ConnectAsync方法
3. PlcConnectionService使用CommunicationAdapterFactory创建通讯适配器
4. 通讯适配器尝试连接到PLC
5. 连接成功后，触发FrameReceived事件

#### 5.1.2 断开流程
1. 用户点击断开按钮
2. 调用PlcConnectionService.DisconnectAsync方法
3. 通讯适配器断开与PLC的连接
4. 更新连接状态

### 5.2 批量读写操作

#### 5.2.1 批量读取流程
1. 用户在MainForm中添加读取操作行
2. 点击批量读取按钮
3. 调用PlcReadWriteService.BatchReadAsync方法
4. 构建MC协议读取帧
5. 发送帧到PLC并接收响应
6. 解析响应数据
7. 更新界面显示

#### 5.2.2 批量写入流程
1. 用户在MainForm中添加写入操作行并输入值
2. 点击批量写入按钮
3. 调用PlcReadWriteService.BatchWriteAsync方法
4. 构建MC协议写入帧
5. 发送帧到PLC并接收响应
6. 解析响应数据
7. 更新界面显示

### 5.3 实时监控

#### 5.3.1 监控流程
1. 用户勾选监控模式复选框
2. 设置监控间隔
3. 启动定时器，按照设定间隔执行读取操作
4. 定时器触发时，调用ReadAllData方法
5. ReadAllData方法执行批量读取操作
6. 更新界面显示

### 5.4 帧分析

#### 5.4.1 帧解析流程
1. 用户在FrameAnalyzerForm中输入通讯帧
2. 点击解析按钮
3. 调用FrameParserService.ParseMcFrame方法
4. 解析帧的各个部分
5. 显示解析结果

## 6. 错误处理

### 6.1 错误处理策略
本项目采用以下错误处理策略：

1. **异常捕获**：在关键操作中捕获异常
2. **错误返回**：使用Result类型返回操作结果和错误信息
3. **日志记录**：记录错误信息到日志
4. **用户反馈**：通过MessageBox向用户显示错误信息

### 6.2 错误类型

| 错误类型 | 描述 | 处理方式 |
|----------|------|----------|
| 连接错误 | 无法连接到PLC | 显示错误信息，检查网络连接 |
| 读写错误 | 无法读写PLC数据 | 显示错误信息，检查软元件地址和类型 |
| 通讯错误 | 通讯过程中发生错误 | 显示错误信息，检查通讯参数 |
| 应用程序错误 | 应用程序内部错误 | 显示错误信息，记录详细日志 |

## 7. 日志系统

### 7.1 日志级别
- **INFO**：一般信息，如连接成功、操作完成等
- **DEBUG**：调试信息，如通讯帧内容
- **WARNING**：警告信息，如参数错误
- **ERROR**：错误信息，如连接失败、读写错误等

### 7.2 日志输出
- **界面日志**：在MainForm的日志面板中显示
- **文件日志**：可通过保存按钮保存到文件

## 8. 配置管理

### 8.1 应用配置
应用配置存储在App.config文件中，包含以下设置：

| 配置项 | 描述 | 默认值 |
|--------|------|--------|
| AppVersion | 应用程序版本 | 3.0.0 |
| DefaultIpAddress | 默认IP地址 | 192.168.1.100 |
| DefaultPort | 默认端口 | 6000 |
| DefaultTimeout | 默认超时时间 | 3000 |
| LogFilePath | 日志文件路径 | Logs |
| DefaultMonitorInterval | 默认监控间隔 | 1000 |

### 8.2 连接配置
连接配置以JSON格式保存，包含以下信息：
- ConnectionName：连接名称
- IpAddress：PLC IP地址
- Port：通讯端口
- Timeout：超时时间
- ProtocolType：协议类型

## 9. 性能优化

### 9.1 通讯优化
- **批量操作**：减少通讯次数，提高效率
- **超时设置**：合理设置超时时间，避免长时间等待
- **错误重试**：在网络不稳定时增加重试机制

### 9.2 界面优化
- **异步操作**：使用异步方法，避免界面卡顿
- **数据绑定**：使用数据绑定，减少手动更新
- **UI线程**：确保UI操作在UI线程中执行

### 9.3 内存优化
- **资源释放**：及时释放不再使用的资源
- **对象池**：重用对象，减少GC压力
- **日志管理**：定期清理日志，避免内存占用过高

## 10. 扩展与定制

### 10.1 添加新的通讯适配器
1. 实现ICommunicationAdapter接口
2. 在CommunicationAdapterFactory中添加创建逻辑
3. 在ConnectionConfigForm中添加相应的配置选项

### 10.2 添加新的软元件类型
1. 在DataType枚举中添加新的类型
2. 在MainForm的StringToDataType方法中添加类型转换
3. 在相关UI控件中添加新类型选项

### 10.3 添加新的命令类型
1. 在FrameParserService的GetCommandName方法中添加新命令
2. 在相关服务中实现命令逻辑
3. 在UI中添加相应的操作选项

## 11. 测试策略

### 11.1 单元测试
- **领域层**：测试核心业务逻辑
- **应用层**：测试服务方法
- **基础设施层**：测试通讯和解析功能

### 11.2 集成测试
- **通讯测试**：测试与PLC的通讯
- **功能测试**：测试完整的功能流程
- **性能测试**：测试系统性能

### 11.3 测试工具
- **NUnit**：单元测试框架
- **Moq**：模拟对象框架
- **Fiddler**：网络调试工具

## 12. 部署与发布

### 12.1 构建配置
- **Debug**：调试版本，包含调试信息
- **Release**：发布版本，优化性能

### 12.2 发布步骤
1. 选择Release配置
2. 构建解决方案
3. 复制bin/Release目录下的文件到发布目录
4. 确保包含所有必要的依赖项

### 12.3 依赖项
- **HslCommunication**：PLC通讯库
- **Newtonsoft.Json**：JSON序列化库
- **Microsoft.Extensions.DependencyInjection**：依赖注入库
- **.NET Framework 4.7.2**：运行时环境

## 13. 代码规范

### 13.1 命名规范
- **类名**：PascalCase
- **方法名**：PascalCase
- **变量名**：camelCase
- **常量名**：UPPER_SNAKE_CASE
- **命名空间**：PascalCase

### 13.2 代码风格
- **缩进**：4个空格
- **换行**：每行不超过120个字符
- **注释**：使用XML文档注释
- **异常处理**：使用try-catch块

### 13.3 最佳实践
- **单一职责**：每个类只负责一个功能
- **依赖注入**：使用依赖注入，避免硬编码依赖
- **错误处理**：使用Result类型，避免异常作为控制流
- **日志记录**：记录关键操作和错误信息
- **单元测试**：为核心功能编写单元测试

## 14. 版本控制

### 14.1 Git工作流
- **主分支**：master，稳定版本
- **开发分支**：develop，开发版本
- **特性分支**：feature/*，新特性开发
- **修复分支**：fix/*，bug修复

### 14.2 版本号规范
- **主版本号**：重大变更
- **次版本号**：新特性
- **修订号**：bug修复

## 15. 技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| C# | 7.0+ | 开发语言 |
| .NET Framework | 4.7.2 | 运行时环境 |
| Windows Forms | 4.7.2 | 用户界面 |
| HslCommunication | 10.6.0 | PLC通讯库 |
| Newtonsoft.Json | 13.0.1 | JSON序列化 |
| Microsoft.Extensions.DependencyInjection | 6.0.0 | 依赖注入 |

## 16. 未来计划

### 16.1 功能增强
- 添加串口通讯支持
- 增加更多PLC型号支持
- 添加数据可视化功能
- 实现数据历史记录和趋势分析

### 16.2 技术改进
- 迁移到.NET Core
- 使用现代UI框架（如WPF）
- 实现模块化设计
- 添加插件系统

### 16.3 性能优化
- 优化通讯协议
- 提高大数据量处理能力
- 减少内存占用
- 提高响应速度

## 17. 结论

Mitsubishi PLC Communication V3 是一个功能完整、架构清晰的三菱PLC通讯应用程序。它采用分层架构设计，提供了丰富的功能，包括PLC连接管理、批量读写操作、实时监控、帧分析等。

项目使用现代C#开发技术，包括依赖注入、异步编程、错误处理等最佳实践。它不仅满足了基本的PLC通讯需求，还提供了友好的用户界面和强大的工具功能。

通过本技术文档，开发者可以了解项目的架构设计、核心组件和实现细节，为后续的开发和扩展提供参考。

---

© 2026 Mitsubishi PLC Communication Team