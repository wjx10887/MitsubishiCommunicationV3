using System;
using HslCommunication;
using HslCommunication.Profinet.Melsec;
using HslCommunication.ModBus;

namespace MelsecPLCCommunicator.Infrastructure.Adapters
{
    /// <summary>
    /// 网络通信适配器实现
    /// </summary>
    public class NetworkCommunicationAdapter : ICommunicationAdapter
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly string _protocolType;
        private readonly string _localIpAddress;
        private readonly int _localPort;
        private MelsecMcNet _plc;
        private MelsecMcAsciiNet _plcAscii;
        private MelsecMcUdp _plcUdp;
        private MelsecMcAsciiUdp _plcUdpAscii;
        private MelsecA1ENet _a1ePlc;
        private MelsecA1EAsciiNet _a1ePlcAscii;
        private ModbusTcpNet _modbusTcp;
        private bool _isConnected;
        private HslCommunication.LogNet.ILogNet _logNet;

        /// <summary>
        /// 通讯帧事件
        /// </summary>
        public event EventHandler<FrameEventArgs> FrameReceived;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口</param>
        /// <param name="protocolType">协议类型</param>
        /// <param name="localIpAddress">本地IP地址</param>
        /// <param name="localPort">本地端口</param>
        public NetworkCommunicationAdapter(string ipAddress, int port, string protocolType, string localIpAddress = "192.168.1.100", int localPort = 3000)
        {
            _ipAddress = ipAddress;
            _port = port;
            _protocolType = protocolType;
            _localIpAddress = localIpAddress;
            _localPort = localPort;
            _isConnected = false;
            InitializeLog();
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public NetworkCommunicationAdapter() : this("192.168.1.100", 6000, "MC协议格式1")
        {
        }

        /// <summary>
        /// 初始化日志
        /// </summary>
        private void InitializeLog()
        {
            _logNet = new HslCommunication.LogNet.LogNetSingle(@"logs\network.log");
            _logNet.BeforeSaveToFile += LogNet_BeforeSaveToFile;
        }

        /// <summary>
        /// 日志保存前事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void LogNet_BeforeSaveToFile(object sender, EventArgs e)
        {
            try
            {
                if (e == null)
                    return;
                
                // 使用反射获取事件参数中的消息内容
                var messageProperty = e.GetType().GetProperty("HslMessage");
                if (messageProperty != null)
                {
                    var messageObj = messageProperty.GetValue(e);
                    if (messageObj != null)
                    {
                        string message = messageObj.ToString();
                        if (!string.IsNullOrEmpty(message) && message.Contains("Send:"))
                        {
                            // 提取发送帧
                            int sendStart = message.IndexOf("Send:") + 5;
                            if (sendStart > 5)
                            {
                                int receiveStart = message.IndexOf("Receive:", sendStart);
                                string sendFrame = string.Empty;
                                string receiveFrame = string.Empty;
                                
                                if (receiveStart > sendStart)
                                {
                                    sendFrame = message.Substring(sendStart, receiveStart - sendStart).Trim();
                                    receiveFrame = message.Substring(receiveStart + 8).Trim();
                                }
                                else
                                {
                                    sendFrame = message.Substring(sendStart).Trim();
                                }
                                
                                // 触发事件
                                FrameReceived?.Invoke(this, new FrameEventArgs
                                {
                                    SendFrame = sendFrame,
                                    ReceiveFrame = receiveFrame
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录异常但不抛出，避免影响主程序
                _logNet?.WriteException("日志处理异常", ex);
            }
        }

        /// <summary>
        /// 连接到设备
        /// </summary>
        /// <returns>是否连接成功</returns>
        public bool Connect()
        {
            try
            {
                // 根据协议类型选择合适的通信类
                switch (_protocolType)
                {
                    case "MC Protocol (3E) - TCP (二进制)":
                        // 使用3E帧协议，TCP二进制
                        _plc = new MelsecMcNet(_ipAddress, _port);
                        _plc.LogNet = _logNet;
                        // 设置本地IP和端口
                        try
                        {
                            // 验证本地IP地址格式
                            if (System.Net.IPAddress.TryParse(_localIpAddress, out var localIp))
                            {
                                // 验证端口范围
                                if (_localPort >= 1 && _localPort <= 65535)
                                {
                                    // 尝试设置本地绑定
                                    try
                                    {
                                        var localBindingField = _plc.GetType().GetField("localBinding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        if (localBindingField != null)
                                        {
                                            localBindingField.SetValue(_plc, new System.Net.IPEndPoint(localIp, _localPort));
                                        }
                                    }
                                    catch (Exception socketEx)
                                    {
                                        _logNet?.WriteException("设置TCP本地绑定失败", socketEx);
                                    }
                                }
                                else
                                {
                                    var errorMsg = $"端口 {_localPort} 不在有效范围内 (1-65535)";
                                    _logNet?.WriteException("TCP本地端口无效，使用默认端口", new Exception(errorMsg));
                                }
                            }
                            else
                            {
                                var errorMsg = $"IP地址 {_localIpAddress} 格式无效";
                                _logNet?.WriteException("TCP本地IP地址格式无效，不设置本地绑定", new Exception(errorMsg));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logNet?.WriteException("设置TCP本地绑定失败", ex);
                        }
                        var result = _plc.ConnectServer();
                        _isConnected = result.IsSuccess;
                        return _isConnected;
                    case "MC Protocol (3E) - TCP (ASCII)":
                        // 使用3E帧协议，TCP ASCII
                        _plcAscii = new MelsecMcAsciiNet(_ipAddress, _port);
                        _plcAscii.LogNet = _logNet;
                        var asciiResult = _plcAscii.ConnectServer();
                        _isConnected = asciiResult.IsSuccess;
                        return _isConnected;
                    case "MC Protocol (3E) - UDP (二进制)":
                        // 使用3E帧协议，UDP二进制
                        // 每次连接时重新创建UDP实例，避免端口冲突
                        _plcUdp = null;
                        _plcUdp = new MelsecMcUdp(_ipAddress, _port);
                        _plcUdp.LogNet = _logNet;
                        // 设置本地IP和端口
                        try
                        {
                            // 验证本地IP地址格式
                            if (System.Net.IPAddress.TryParse(_localIpAddress, out var localIp))
                            {
                                // 验证端口范围
                                if (_localPort >= 1 && _localPort <= 65535)
                                {
                                    _plcUdp.LocalBinding = new System.Net.IPEndPoint(localIp, _localPort);
                                    // 尝试设置套接字选项，允许端口重用
                                        /*
                                        try
                                        {
                                            var socketField = _plcUdp.GetType().GetField("socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                            if (socketField != null)
                                            {
                                                var socket = socketField.GetValue(_plcUdp) as System.Net.Sockets.Socket;
                                                if (socket != null)
                                                {
                                                    socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                                                }
                                            }
                                        }
                                        catch (Exception socketEx)
                                        {
                                            _logNet?.WriteException("设置套接字选项失败", socketEx);
                                        }
                                        */
                                }
                                else
                                {
                                    var errorMsg = $"端口 {_localPort} 不在有效范围内 (1-65535)";
                                    _logNet?.WriteException("UDP本地端口无效，使用默认端口", new Exception(errorMsg));
                                }
                            }
                            else
                            {
                                var errorMsg = $"IP地址 {_localIpAddress} 格式无效";
                                _logNet?.WriteException("UDP本地IP地址格式无效，不设置本地绑定", new Exception(errorMsg));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logNet?.WriteException("设置UDP本地绑定失败", ex);
                            // _logService?.Error("设置UDP本地绑定失败", ex);
                        }
                        // UDP不需要显式连接，直接设置为已连接
                        // 尝试连接远程的服务器，如果连接成功，就切换短连接模式到长连接模式，
                        // 后面的每次请求都共享一个通道，使得通讯速度更快速
                        OperateResult udpResult = _plcUdp.ConnectServer();
                        _isConnected = udpResult.IsSuccess;
                        return _isConnected;
                    case "MC Protocol (3E) - UDP (ASCII)":
                        // 使用3E帧协议，UDP ASCII
                        // 每次连接时重新创建UDP实例，避免端口冲突
                        _plcUdpAscii = null;
                        _plcUdpAscii = new MelsecMcAsciiUdp(_ipAddress, _port);
                        _plcUdpAscii.LogNet = _logNet;
                        // 设置本地IP和端口
                        try
                        {
                            // 验证本地IP地址格式
                            if (System.Net.IPAddress.TryParse(_localIpAddress, out var localIp))
                            {
                                // 验证端口范围
                                if (_localPort >= 1 && _localPort <= 65535)
                                {
                                    _plcUdpAscii.LocalBinding = new System.Net.IPEndPoint(localIp, _localPort);
                                    // 尝试设置套接字选项，允许端口重用
                                    /*
                                    try
                                    {
                                        var socketField = _plcUdpAscii.GetType().GetField("socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                        if (socketField != null)
                                        {
                                            var socket = socketField.GetValue(_plcUdpAscii) as System.Net.Sockets.Socket;
                                            if (socket != null)
                                            {
                                                socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                                            }
                                        }
                                    }
                                    catch (Exception socketEx)
                                    {
                                        _logNet?.WriteException("设置套接字选项失败", socketEx);
                                    }
                                    */
                                }
                                else
                                {
                                    var errorMsg = $"端口 {_localPort} 不在有效范围内 (1-65535)";
                                    _logNet?.WriteException("UDP本地端口无效，使用默认端口", new Exception(errorMsg));
                                }
                            }
                            else
                            {
                                var errorMsg = $"IP地址 {_localIpAddress} 格式无效";
                                _logNet?.WriteException("UDP本地IP地址格式无效，不设置本地绑定", new Exception(errorMsg));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logNet?.WriteException("设置UDP本地绑定失败", ex);
                            // _logService?.Error("设置UDP本地绑定失败", ex);
                        }
                        // UDP不需要显式连接，直接设置为已连接
                        // 尝试连接远程的服务器，如果连接成功，就切换短连接模式到长连接模式，
                        // 后面的每次请求都共享一个通道，使得通讯速度更快速
                        OperateResult udpAsciiResult = _plcUdpAscii.ConnectServer();
                        _isConnected = udpAsciiResult.IsSuccess;
                        return _isConnected;
                    case "MC Protocol (1E) - TCP (二进制)":
                        // 使用1E帧协议，二进制
                        _a1ePlc = new MelsecA1ENet(_ipAddress, _port);
                        _a1ePlc.LogNet = _logNet;
                        var a1eResult = _a1ePlc.ConnectServer();
                        _isConnected = a1eResult.IsSuccess;
                        return _isConnected;
                    case "MC Protocol (1E) - TCP (ASCII)":
                        // 使用1E帧协议，ASCII
                        _a1ePlcAscii = new MelsecA1EAsciiNet(_ipAddress, _port);
                        _a1ePlcAscii.LogNet = _logNet;
                        var a1eAsciiResult = _a1ePlcAscii.ConnectServer();
                        _isConnected = a1eAsciiResult.IsSuccess;
                        return _isConnected;
                    case "Modbus TCP":
                        // 使用Modbus TCP协议
                        _modbusTcp = new ModbusTcpNet(_ipAddress, _port);
                        _modbusTcp.LogNet = _logNet;
                        var modbusResult = _modbusTcp.ConnectServer();
                        _isConnected = modbusResult.IsSuccess;
                        return _isConnected;
                    default:
                        // 默认使用3E帧协议，TCP二进制
                        _plc = new MelsecMcNet(_ipAddress, _port);
                        _plc.LogNet = _logNet;
                        var defaultResult = _plc.ConnectServer();
                        _isConnected = defaultResult.IsSuccess;
                        return _isConnected;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            _plc?.ConnectClose();
            _plcAscii?.ConnectClose();
            // UDP不需要断开连接
            _a1ePlc?.ConnectClose();
            _a1ePlcAscii?.ConnectClose();
            _modbusTcp?.ConnectClose();
                    // 释放UDP连接
            if (_plcUdp != null)
            {
                // UDP 实例需要手动调用 ConnectClose() 方法显式关闭连接
                _plcUdp.ConnectClose();
            }
            if (_plcUdpAscii != null)
            {
                // UDP 实例需要手动调用 ConnectClose() 方法显式关闭连接
                _plcUdpAscii.ConnectClose();
            }
            _isConnected = false;
        }

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="length">长度</param>
        /// <returns>读取结果</returns>
        public object Read(string address, string dataType, ushort length)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("设备未连接");
            }

            if (_plc != null)
            {
                return ReadData(_plc, dataType, address, length);
            }
            else if (_plcAscii != null)
            {
                return ReadData(_plcAscii, dataType, address, length);
            }
            else if (_plcUdp != null)
            {
                for (int retry = 0; retry < 3; retry++)
                {
                    try
                    {
                        return ReadData(_plcUdp, dataType, address, length);
                    }
                    catch (Exception ex)
                    {
                        // 如果是套接字地址错误，重新创建UDP实例
                        if (ex.Message.Contains("套接字地址") || ex.Message.Contains("socket address") || ex.Message.Contains("只允许使用一次") || ex.Message.Contains("通常每个套接字地址"))
                        {
                            _logNet?.WriteException($"UDP连接错误，重新创建实例 (尝试 {retry + 1}/3)", ex);
                            // 重新创建UDP实例
                            _plcUdp = null;
                            System.Threading.Thread.Sleep(100); // 短暂延迟，让操作系统有时间释放端口
                            _plcUdp = new MelsecMcUdp(_ipAddress, _port);
                            _plcUdp.LogNet = _logNet;
                            try
                            {
                                // 验证本地IP地址格式
                                if (System.Net.IPAddress.TryParse(_localIpAddress, out var localIp))
                                {
                                    // 验证端口范围
                                    if (_localPort >= 1 && _localPort <= 65535)
                                    {
                                        _plcUdp.LocalBinding = new System.Net.IPEndPoint(localIp, _localPort);
                                        // 尝试设置套接字选项，允许端口重用
                                        /*
                                        try
                                        {
                                            var socketField = _plcUdp.GetType().GetField("socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                            if (socketField != null)
                                            {
                                                var socket = socketField.GetValue(_plcUdp) as System.Net.Sockets.Socket;
                                                if (socket != null)
                                                {
                                                    socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                                                }
                                            }
                                        }
                                        catch (Exception socketEx)
                                        {
                                            _logNet?.WriteException("设置套接字选项失败", socketEx);
                                            // _logService?.Error("设置套接字选项失败", socketEx);
                                        }
                                        */
                                    }
                                    else
                                    {
                                        var errorMsg = $"端口 {_localPort} 不在有效范围内 (1-65535)";
                                        _logNet?.WriteException("UDP本地端口无效，使用默认端口", new Exception(errorMsg));
                                        // _logService?.Error("UDP本地端口无效，使用默认端口", null, errorMsg);
                                    }
                                }
                                else
                                {
                                    var errorMsg = $"IP地址 {_localIpAddress} 格式无效";
                                    _logNet?.WriteException("UDP本地IP地址格式无效，不设置本地绑定", new Exception(errorMsg));
                                    // _logService?.Error("UDP本地IP地址格式无效，不设置本地绑定", null, errorMsg);
                                }
                            }
                            catch (Exception bindEx)
                            {
                                _logNet?.WriteException("设置UDP本地绑定失败", bindEx);
                            }
                            // 如果是最后一次尝试，直接抛出异常
                            if (retry == 2)
                            {
                                throw;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                throw new Exception("UDP读取失败，已达到最大重试次数");
            }
            else if (_plcUdpAscii != null)
            {
                for (int retry = 0; retry < 3; retry++)
                {
                    try
                    {
                        return ReadData(_plcUdpAscii, dataType, address, length);
                    }
                    catch (Exception ex)
                    {
                        // 如果是套接字地址错误，重新创建UDP实例
                        if (ex.Message.Contains("套接字地址") || ex.Message.Contains("socket address") || ex.Message.Contains("只允许使用一次") || ex.Message.Contains("通常每个套接字地址"))
                        {
                            _logNet?.WriteException($"UDP连接错误，重新创建实例 (尝试 {retry + 1}/3)", ex);
                            // 重新创建UDP实例
                            _plcUdpAscii = null;
                            System.Threading.Thread.Sleep(100); // 短暂延迟，让操作系统有时间释放端口
                            _plcUdpAscii = new MelsecMcAsciiUdp(_ipAddress, _port);
                            _plcUdpAscii.LogNet = _logNet;
                            try
                            {
                                // 验证本地IP地址格式
                                if (System.Net.IPAddress.TryParse(_localIpAddress, out var localIp))
                                {
                                    // 验证端口范围
                                    if (_localPort >= 1 && _localPort <= 65535)
                                    {
                                        _plcUdpAscii.LocalBinding = new System.Net.IPEndPoint(localIp, _localPort);
                                        // 尝试设置套接字选项，允许端口重用
                                        /*
                                        try
                                        {
                                            var socketField = _plcUdpAscii.GetType().GetField("socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                            if (socketField != null)
                                            {
                                                var socket = socketField.GetValue(_plcUdpAscii) as System.Net.Sockets.Socket;
                                                if (socket != null)
                                                {
                                                    socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                                                }
                                            }
                                        }
                                        catch (Exception socketEx)
                                        {
                                            _logNet?.WriteException("设置套接字选项失败", socketEx);
                                            // _logService?.Error("设置套接字选项失败", socketEx);
                                        }
                                        */
                                    }
                                    else
                                    {
                                        var errorMsg = $"端口 {_localPort} 不在有效范围内 (1-65535)";
                                        _logNet?.WriteException("UDP本地端口无效，使用默认端口", new Exception(errorMsg));
                                        // _logService?.Error("UDP本地端口无效，使用默认端口", null, errorMsg);
                                    }
                                }
                                else
                                {
                                    var errorMsg = $"IP地址 {_localIpAddress} 格式无效";
                                    _logNet?.WriteException("UDP本地IP地址格式无效，不设置本地绑定", new Exception(errorMsg));
                                    // _logService?.Error("UDP本地IP地址格式无效，不设置本地绑定", null, errorMsg);
                                }
                            }
                            catch (Exception bindEx)
                            {
                                _logNet?.WriteException("设置UDP本地绑定失败", bindEx);
                            }
                            // 如果是最后一次尝试，直接抛出异常
                            if (retry == 2)
                            {
                                throw;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                throw new Exception("UDP读取失败，已达到最大重试次数");
            }
            else if (_a1ePlc != null)
            {
                return ReadData(_a1ePlc, dataType, address, length);
            }
            else if (_a1ePlcAscii != null)
            {
                return ReadData(_a1ePlcAscii, dataType, address, length);
            }
            else if (_modbusTcp != null)
            {
                return ReadData(_modbusTcp, dataType, address, length);
            }
            else
            {
                throw new InvalidOperationException("设备未连接");
            }
        }

        /// <summary>
        /// 通用读取数据方法 - MelsecMcNet
        /// </summary>
        private object ReadData(MelsecMcNet plc, string dataType, string address, ushort length)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "B":
                case "S":
                case "F":
                case "T":
                case "C":
                    var boolResult = plc.ReadBool($"{dataType}{address}", length);
                    if (boolResult.IsSuccess)
                    {
                        return boolResult.Content;
                    }
                    throw new Exception(boolResult.Message);
                case "D":
                case "W":
                case "R":
                    var shortResult = plc.ReadInt16($"{dataType}{address}", length);
                    if (shortResult.IsSuccess)
                    {
                        return shortResult.Content;
                    }
                    throw new Exception(shortResult.Message);
                case "TN": // 定时器当前值
                case "CN": // 计数器当前值
                    var shortResultTC = plc.ReadInt16($"{dataType}{address}", length);
                    if (shortResultTC.IsSuccess)
                    {
                        return shortResultTC.Content;
                    }
                    throw new Exception(shortResultTC.Message);
                case "D32": // 32位整型
                    var intResultD32 = plc.ReadInt32($"D{address}", length);
                    if (intResultD32.IsSuccess)
                    {
                        return intResultD32.Content;
                    }
                    throw new Exception(intResultD32.Message);
                case "Float": // 浮点数
                    var floatResult = plc.ReadFloat($"D{address}", length);
                    if (floatResult.IsSuccess)
                    {
                        return floatResult.Content;
                    }
                    throw new Exception(floatResult.Message);
                case "DFloat": // 双精度浮点
                    var doubleResult = plc.ReadDouble($"D{address}", length);
                    if (doubleResult.IsSuccess)
                    {
                        return doubleResult.Content;
                    }
                    throw new Exception(doubleResult.Message);
                default:
                    throw new NotSupportedException($"不支持的数据类型: {dataType}");
            }
        }

        /// <summary>
        /// 通用读取数据方法 - MelsecMcAsciiNet
        /// </summary>
        private object ReadData(MelsecMcAsciiNet plc, string dataType, string address, ushort length)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "B":
                case "S":
                case "F":
                case "T":
                case "C":
                    var boolResult = plc.ReadBool($"{dataType}{address}", length);
                    if (boolResult.IsSuccess)
                    {
                        return boolResult.Content;
                    }
                    throw new Exception(boolResult.Message);
                case "D":
                case "W":
                case "R":
                case "ZR": // 字数据寄存器
                    var shortResult = plc.ReadInt16($"{dataType}{address}", length);
                    if (shortResult.IsSuccess)
                    {
                        return shortResult.Content;
                    }
                    throw new Exception(shortResult.Message);
                case "TN": // 定时器当前值
                case "CN": // 计数器当前值
                    var shortResultTC = plc.ReadInt16($"{dataType}{address}", length);
                    if (shortResultTC.IsSuccess)
                    {
                        return shortResultTC.Content;
                    }
                    throw new Exception(shortResultTC.Message);

                default:
                    throw new NotSupportedException($"不支持的数据类型: {dataType}");
            }
        }

        /// <summary>
        /// 通用读取数据方法 - MelsecMcUdp
        /// </summary>
        private object ReadData(MelsecMcUdp plc, string dataType, string address, ushort length)
        {
            try
            {
                _logNet?.WriteInfo($"UDP读取数据: 类型={dataType}, 地址={address}, 长度={length}");
                switch (dataType)
                {
                    case "M":
                    case "X":
                    case "Y":
                    case "L":
                    case "B":
                    case "S":
                    case "F":
                    case "T":
                    case "C":
                        var boolResult = plc.ReadBool($"{dataType}{address}", length);
                        if (boolResult.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP读取布尔值成功: {boolResult.Content}");
                            return boolResult.Content;
                        }
                        _logNet?.WriteError($"UDP读取布尔值失败: {boolResult.Message}");
                        throw new Exception(boolResult.Message);
                    case "D":
                    case "W":
                    case "R":
                        var shortResult = plc.ReadInt16($"{dataType}{address}", length);
                        if (shortResult.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP读取短整型成功: {shortResult.Content}");
                            return shortResult.Content;
                        }
                        _logNet?.WriteError($"UDP读取短整型失败: {shortResult.Message}");
                        throw new Exception(shortResult.Message);
                    case "TN": // 定时器当前值
                    case "CN": // 计数器当前值
                        var shortResultTC = plc.ReadInt16($"{dataType}{address}", length);
                        if (shortResultTC.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP读取定时器/计数器成功: {shortResultTC.Content}");
                            return shortResultTC.Content;
                        }
                        _logNet?.WriteError($"UDP读取定时器/计数器失败: {shortResultTC.Message}");
                        throw new Exception(shortResultTC.Message);
                    case "D32": // 32位整型
                        var intResultD32 = plc.ReadInt32($"D{address}", length);
                        if (intResultD32.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP读取32位整型成功: {intResultD32.Content}");
                            return intResultD32.Content;
                        }
                        _logNet?.WriteError($"UDP读取32位整型失败: {intResultD32.Message}");
                        throw new Exception(intResultD32.Message);
                    case "Float": // 浮点数
                        var floatResult = plc.ReadFloat($"D{address}", length);
                        if (floatResult.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP读取浮点数成功: {floatResult.Content}");
                            return floatResult.Content;
                        }
                        _logNet?.WriteError($"UDP读取浮点数失败: {floatResult.Message}");
                        // _logService?.Error($"UDP读取浮点数失败: {floatResult.Message}");
                        throw new Exception(floatResult.Message);
                    case "DFloat": // 双精度浮点
                        var doubleResult = plc.ReadDouble($"D{address}", length);
                        if (doubleResult.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP读取双精度浮点成功: {doubleResult.Content}");
                            // _logService?.Info($"UDP读取双精度浮点成功: {doubleResult.Content}");
                            return doubleResult.Content;
                        }
                        _logNet?.WriteError($"UDP读取双精度浮点失败: {doubleResult.Message}");
                        // _logService?.Error($"UDP读取双精度浮点失败: {doubleResult.Message}");
                        throw new Exception(doubleResult.Message);
                    default:
                        var errorMsg = $"不支持的数据类型: {dataType}";
                        // _logService?.Error(errorMsg);
                        throw new NotSupportedException(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logNet?.WriteException($"UDP读取数据异常: 类型={dataType}, 地址={address}, 长度={length}", ex);
                // _logService?.Error($"UDP读取数据异常: 类型={dataType}, 地址={address}, 长度={length}", ex);
                throw;
            }
        }

        /// <summary>
        /// 通用读取数据方法 - MelsecMcAsciiUdp
        /// </summary>
        private object ReadData(MelsecMcAsciiUdp plc, string dataType, string address, ushort length)
        {
            try
            {
                _logNet?.WriteInfo($"UDP ASCII读取数据: 类型={dataType}, 地址={address}, 长度={length}");
                // _logService?.Info($"UDP ASCII读取数据: 类型={dataType}, 地址={address}, 长度={length}");
                switch (dataType)
                {
                    case "M":
                    case "X":
                    case "Y":
                    case "L":
                    case "B":
                    case "S":
                    case "F":
                    case "T":
                    case "C":
                        var boolResult = plc.ReadBool($"{dataType}{address}", length);
                        if (boolResult.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP ASCII读取布尔值成功: {boolResult.Content}");
                            // _logService?.Info($"UDP ASCII读取布尔值成功: {boolResult.Content}");
                            return boolResult.Content;
                        }
                        _logNet?.WriteError($"UDP ASCII读取布尔值失败: {boolResult.Message}");
                        // _logService?.Error($"UDP ASCII读取布尔值失败: {boolResult.Message}");
                        throw new Exception(boolResult.Message);
                    case "D":
                    case "W":
                    case "R":
                        var shortResult = plc.ReadInt16($"{dataType}{address}", length);
                        if (shortResult.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP ASCII读取短整型成功: {shortResult.Content}");
                            // _logService?.Info($"UDP ASCII读取短整型成功: {shortResult.Content}");
                            return shortResult.Content;
                        }
                        _logNet?.WriteError($"UDP ASCII读取短整型失败: {shortResult.Message}");
                        // _logService?.Error($"UDP ASCII读取短整型失败: {shortResult.Message}");
                        throw new Exception(shortResult.Message);
                    case "TN": // 定时器当前值
                    case "CN": // 计数器当前值
                        var shortResultTC = plc.ReadInt16($"{dataType}{address}", length);
                        if (shortResultTC.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP ASCII读取定时器/计数器成功: {shortResultTC.Content}");
                            // _logService?.Info($"UDP ASCII读取定时器/计数器成功: {shortResultTC.Content}");
                            return shortResultTC.Content;
                        }
                        _logNet?.WriteError($"UDP ASCII读取定时器/计数器失败: {shortResultTC.Message}");
                        // _logService?.Error($"UDP ASCII读取定时器/计数器失败: {shortResultTC.Message}");
                        throw new Exception(shortResultTC.Message);
                    case "D32": // 32位整型
                        var intResultD32 = plc.ReadInt32($"D{address}", length);
                        if (intResultD32.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP ASCII读取32位整型成功: {intResultD32.Content}");
                            // _logService?.Info($"UDP ASCII读取32位整型成功: {intResultD32.Content}");
                            return intResultD32.Content;
                        }
                        _logNet?.WriteError($"UDP ASCII读取32位整型失败: {intResultD32.Message}");
                        // _logService?.Error($"UDP ASCII读取32位整型失败: {intResultD32.Message}");
                        throw new Exception(intResultD32.Message);
                    case "Float": // 浮点数
                        var floatResult = plc.ReadFloat($"D{address}", length);
                        if (floatResult.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP ASCII读取浮点数成功: {floatResult.Content}");
                            // _logService?.Info($"UDP ASCII读取浮点数成功: {floatResult.Content}");
                            return floatResult.Content;
                        }
                        _logNet?.WriteError($"UDP ASCII读取浮点数失败: {floatResult.Message}");
                        // _logService?.Error($"UDP ASCII读取浮点数失败: {floatResult.Message}");
                        throw new Exception(floatResult.Message);
                    case "DFloat": // 双精度浮点
                        var doubleResult = plc.ReadDouble($"D{address}", length);
                        if (doubleResult.IsSuccess)
                        {
                            _logNet?.WriteInfo($"UDP ASCII读取双精度浮点成功: {doubleResult.Content}");
                            // _logService?.Info($"UDP ASCII读取双精度浮点成功: {doubleResult.Content}");
                            return doubleResult.Content;
                        }
                        _logNet?.WriteError($"UDP ASCII读取双精度浮点失败: {doubleResult.Message}");
                        // _logService?.Error($"UDP ASCII读取双精度浮点失败: {doubleResult.Message}");
                        throw new Exception(doubleResult.Message);
                    default:
                        var errorMsg = $"不支持的数据类型: {dataType}";
                        // _logService?.Error(errorMsg);
                        throw new NotSupportedException(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logNet?.WriteException($"UDP ASCII读取数据异常: 类型={dataType}, 地址={address}, 长度={length}", ex);
                // _logService?.Error($"UDP ASCII读取数据异常: 类型={dataType}, 地址={address}, 长度={length}", ex);
                throw;
            }
        }

        /// <summary>
        /// 通用读取数据方法 - MelsecA1ENet
        /// </summary>
        private object ReadData(MelsecA1ENet plc, string dataType, string address, ushort length)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "B":
                case "S":
                case "F":
                case "T":
                case "C":
                    var boolResult = plc.ReadBool($"{dataType}{address}", length);
                    if (boolResult.IsSuccess)
                    {
                        return boolResult.Content;
                    }
                    throw new Exception(boolResult.Message);
                case "D":
                case "W":
                case "R":
                    var shortResult = plc.ReadInt16($"{dataType}{address}", length);
                    if (shortResult.IsSuccess)
                    {
                        return shortResult.Content;
                    }
                    throw new Exception(shortResult.Message);
                case "TN": // 定时器当前值
                case "CN": // 计数器当前值
                    var shortResultTC = plc.ReadInt16($"{dataType}{address}", length);
                    if (shortResultTC.IsSuccess)
                    {
                        return shortResultTC.Content;
                    }
                    throw new Exception(shortResultTC.Message);
                case "D32": // 32位整型
                    var intResultD32 = plc.ReadInt32($"D{address}", length);
                    if (intResultD32.IsSuccess)
                    {
                        return intResultD32.Content;
                    }
                    throw new Exception(intResultD32.Message);
                case "Float": // 浮点数
                    var floatResult = plc.ReadFloat($"D{address}", length);
                    if (floatResult.IsSuccess)
                    {
                        return floatResult.Content;
                    }
                    throw new Exception(floatResult.Message);
                case "DFloat": // 双精度浮点
                    var doubleResult = plc.ReadDouble($"D{address}", length);
                    if (doubleResult.IsSuccess)
                    {
                        return doubleResult.Content;
                    }
                    throw new Exception(doubleResult.Message);
                default:
                    throw new NotSupportedException($"不支持的数据类型: {dataType}");
            }
        }

        /// <summary>
        /// 通用读取数据方法 - MelsecA1EAsciiNet
        /// </summary>
        private object ReadData(MelsecA1EAsciiNet plc, string dataType, string address, ushort length)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "B":
                case "S":
                case "F":
                case "T":
                case "C":
                    var boolResult = plc.ReadBool($"{dataType}{address}", length);
                    if (boolResult.IsSuccess)
                    {
                        return boolResult.Content;
                    }
                    throw new Exception(boolResult.Message);
                case "D":
                case "W":
                case "R":
                    var shortResult = plc.ReadInt16($"{dataType}{address}", length);
                    if (shortResult.IsSuccess)
                    {
                        return shortResult.Content;
                    }
                    throw new Exception(shortResult.Message);
                case "TN": // 定时器当前值
                case "CN": // 计数器当前值
                    var shortResultTC = plc.ReadInt16($"{dataType}{address}", length);
                    if (shortResultTC.IsSuccess)
                    {
                        return shortResultTC.Content;
                    }
                    throw new Exception(shortResultTC.Message);
                case "D32": // 32位整型
                    var intResultD32 = plc.ReadInt32($"D{address}", length);
                    if (intResultD32.IsSuccess)
                    {
                        return intResultD32.Content;
                    }
                    throw new Exception(intResultD32.Message);
                case "Float": // 浮点数
                    var floatResult = plc.ReadFloat($"D{address}", length);
                    if (floatResult.IsSuccess)
                    {
                        return floatResult.Content;
                    }
                    throw new Exception(floatResult.Message);
                case "DFloat": // 双精度浮点
                    var doubleResult = plc.ReadDouble($"D{address}", length);
                    if (doubleResult.IsSuccess)
                    {
                        return doubleResult.Content;
                    }
                    throw new Exception(doubleResult.Message);
                default:
                    throw new NotSupportedException($"不支持的数据类型: {dataType}");
            }
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="value">值</param>
        /// <returns>是否写入成功</returns>
        public bool Write(string address, string dataType, object value)
        {
            if (!IsConnected)
            {
                return false;
            }

            try
            {
                if (_plc != null)
                {
                    return WriteData(_plc, dataType, address, value);
                }
                else if (_plcAscii != null)
                {
                    return WriteData(_plcAscii, dataType, address, value);
                }
                else if (_plcUdp != null)
                {
                    for (int retry = 0; retry < 3; retry++)
                    {
                        try
                        {
                            return WriteData(_plcUdp, dataType, address, value);
                        }
                        catch (Exception ex)
                        {
                            // 如果是套接字地址错误，重新创建UDP实例
                            if (ex.Message.Contains("套接字地址") || ex.Message.Contains("socket address") || ex.Message.Contains("只允许使用一次") || ex.Message.Contains("通常每个套接字地址"))
                            {
                                _logNet?.WriteException($"UDP连接错误，重新创建实例 (尝试 {retry + 1}/3)", ex);
                                // _logService?.Error($"UDP连接错误，重新创建实例 (尝试 {retry + 1}/3)", ex);
                                // 重新创建UDP实例
                            _plcUdp = null;
                            System.Threading.Thread.Sleep(100); // 短暂延迟，让操作系统有时间释放端口
                            _plcUdp = new MelsecMcUdp(_ipAddress, _port);
                            _plcUdp.LogNet = _logNet;
                            try
                            {
                                // 验证本地IP地址格式
                                if (System.Net.IPAddress.TryParse(_localIpAddress, out var localIp))
                                {
                                    // 验证端口范围
                                    if (_localPort >= 1 && _localPort <= 65535)
                                    {
                                        _plcUdp.LocalBinding = new System.Net.IPEndPoint(localIp, _localPort);
                                        // 尝试设置套接字选项，允许端口重用
                                        /*
                                        try
                                        {
                                            var socketField = _plcUdp.GetType().GetField("socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                            if (socketField != null)
                                            {
                                                var socket = socketField.GetValue(_plcUdp) as System.Net.Sockets.Socket;
                                                if (socket != null)
                                                {
                                                    socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                                                }
                                            }
                                        }
                                        catch (Exception socketEx)
                                        {
                                            _logNet?.WriteException("设置套接字选项失败", socketEx);
                                            // _logService?.Error("设置套接字选项失败", socketEx);
                                        }
                                        */
                                    }
                                    else
                                    {
                                        var errorMsg = $"端口 {_localPort} 不在有效范围内 (1-65535)";
                                        _logNet?.WriteException("UDP本地端口无效，使用默认端口", new Exception(errorMsg));
                                        // _logService?.Error("UDP本地端口无效，使用默认端口", null, errorMsg);
                                    }
                                }
                                else
                                {
                                    var errorMsg = $"IP地址 {_localIpAddress} 格式无效";
                                    _logNet?.WriteException("UDP本地IP地址格式无效，不设置本地绑定", new Exception(errorMsg));
                                    // _logService?.Error("UDP本地IP地址格式无效，不设置本地绑定", null, errorMsg);
                                }
                            }
                            catch (Exception bindEx)
                            {
                                _logNet?.WriteException("设置UDP本地绑定失败", bindEx);
                            }
                                // 如果是最后一次尝试，直接返回失败
                                if (retry == 2)
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    return false;
                }
                else if (_plcUdpAscii != null)
                {
                    for (int retry = 0; retry < 3; retry++)
                    {
                        try
                        {
                            return WriteData(_plcUdpAscii, dataType, address, value);
                        }
                        catch (Exception ex)
                        {
                            // 如果是套接字地址错误，重新创建UDP实例
                            if (ex.Message.Contains("套接字地址") || ex.Message.Contains("socket address") || ex.Message.Contains("只允许使用一次") || ex.Message.Contains("通常每个套接字地址"))
                            {
                                _logNet?.WriteException($"UDP连接错误，重新创建实例 (尝试 {retry + 1}/3)", ex);
                                // _logService?.Error($"UDP连接错误，重新创建实例 (尝试 {retry + 1}/3)", ex);
                                // 重新创建UDP实例
                            _plcUdpAscii = null;
                            System.Threading.Thread.Sleep(100); // 短暂延迟，让操作系统有时间释放端口
                            _plcUdpAscii = new MelsecMcAsciiUdp(_ipAddress, _port);
                            _plcUdpAscii.LogNet = _logNet;
                            try
                            {
                                // 验证本地IP地址格式
                                if (System.Net.IPAddress.TryParse(_localIpAddress, out var localIp))
                                {
                                    // 验证端口范围
                                    if (_localPort >= 1 && _localPort <= 65535)
                                    {
                                        _plcUdpAscii.LocalBinding = new System.Net.IPEndPoint(localIp, _localPort);
                                        // 尝试设置套接字选项，允许端口重用
                                        /*
                                        try
                                        {
                                            var socketField = _plcUdpAscii.GetType().GetField("socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                            if (socketField != null)
                                            {
                                                var socket = socketField.GetValue(_plcUdpAscii) as System.Net.Sockets.Socket;
                                                if (socket != null)
                                                {
                                                    socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
                                                }
                                            }
                                        }
                                        catch (Exception socketEx)
                                        {
                                            _logNet?.WriteException("设置套接字选项失败", socketEx);
                                            // _logService?.Error("设置套接字选项失败", socketEx);
                                        }
                                        */
                                    }
                                    else
                                    {
                                        var errorMsg = $"端口 {_localPort} 不在有效范围内 (1-65535)";
                                        _logNet?.WriteException("UDP本地端口无效，使用默认端口", new Exception(errorMsg));
                                        // _logService?.Error("UDP本地端口无效，使用默认端口", null, errorMsg);
                                    }
                                }
                                else
                                {
                                    var errorMsg = $"IP地址 {_localIpAddress} 格式无效";
                                    _logNet?.WriteException("UDP本地IP地址格式无效，不设置本地绑定", new Exception(errorMsg));
                                    // _logService?.Error("UDP本地IP地址格式无效，不设置本地绑定", null, errorMsg);
                                }
                            }
                            catch (Exception bindEx)
                            {
                                _logNet?.WriteException("设置UDP本地绑定失败", bindEx);
                            }
                                // 如果是最后一次尝试，直接返回失败
                                if (retry == 2)
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    return false;
                }
                else if (_a1ePlc != null)
                {
                    return WriteData(_a1ePlc, dataType, address, value);
                }
                else if (_a1ePlcAscii != null)
                {
                    return WriteData(_a1ePlcAscii, dataType, address, value);
                }
                else if (_modbusTcp != null)
                {
                    return WriteData(_modbusTcp, dataType, address, value);
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 通用写入数据方法 - MelsecMcNet
        /// </summary>
        private bool WriteData(MelsecMcNet plc, string dataType, string address, object value)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "B":
                case "S":
                case "F":
                case "T":
                case "C":
                    if (value is bool boolValue)
                    {
                        var result = plc.Write($"{dataType}{address}", boolValue);
                        return result.IsSuccess;
                    }
                    break;
                case "D":
                case "W":
                case "R":
                    if (value is short shortValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue1);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue1);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue1);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue1);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue1 && short.TryParse(stringValue1, out short parsedShort1))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort1);
                        return result.IsSuccess;
                    }
                    break;
                case "TN":
                case "CN":
                    if (value is short shortValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && short.TryParse(stringValue2, out short parsedShort2))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort2);
                        return result.IsSuccess;
                    }
                    break;
                case "D32": // 32位整型
                    if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && int.TryParse(stringValue3, out int parsedInt3))
                    {
                        var result = plc.Write($"D{address}", parsedInt3);
                        return result.IsSuccess;
                    }
                    break;
                case "Float": // 浮点数
                    if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (float)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && float.TryParse(stringValue4, out float parsedFloat4))
                    {
                        var result = plc.Write($"D{address}", parsedFloat4);
                        return result.IsSuccess;
                    }
                    break;
                case "DFloat": // 双精度浮点
                    if (value is double doubleValue5)
                    {
                        var result = plc.Write($"D{address}", doubleValue5);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue5)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue5);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue5)
                    {
                        var result = plc.Write($"D{address}", (double)intValue5);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue5)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue5);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue5 && double.TryParse(stringValue5, out double parsedDouble5))
                    {
                        var result = plc.Write($"D{address}", parsedDouble5);
                        return result.IsSuccess;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// 通用写入数据方法 - MelsecMcAsciiNet
        /// </summary>
        private bool WriteData(MelsecMcAsciiNet plc, string dataType, string address, object value)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "B":
                case "S":
                case "F":
                case "T":
                case "C":
                    if (value is bool boolValue)
                    {
                        var result = plc.Write($"{dataType}{address}", boolValue);
                        return result.IsSuccess;
                    }
                    break;
                case "D":
                case "W":
                case "R":
                    if (value is short shortValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue1);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue1);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue1);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue1);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue1 && short.TryParse(stringValue1, out short parsedShort1))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort1);
                        return result.IsSuccess;
                    }
                    break;
                case "TN":
                case "CN":
                    if (value is short shortValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && short.TryParse(stringValue2, out short parsedShort2))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort2);
                        return result.IsSuccess;
                    }
                    break;
                case "D32": // 32位整型
                    if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && int.TryParse(stringValue3, out int parsedInt3))
                    {
                        var result = plc.Write($"D{address}", parsedInt3);
                        return result.IsSuccess;
                    }
                    break;
                case "Float": // 浮点数
                    if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (float)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && float.TryParse(stringValue4, out float parsedFloat4))
                    {
                        var result = plc.Write($"D{address}", parsedFloat4);
                        return result.IsSuccess;
                    }
                    break;
                case "DFloat": // 双精度浮点
                    if (value is double doubleValue5)
                    {
                        var result = plc.Write($"D{address}", doubleValue5);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue5)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue5);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue5)
                    {
                        var result = plc.Write($"D{address}", (double)intValue5);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue5)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue5);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue5 && double.TryParse(stringValue5, out double parsedDouble5))
                    {
                        var result = plc.Write($"D{address}", parsedDouble5);
                        return result.IsSuccess;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// 通用写入数据方法 - MelsecMcUdp
        /// </summary>
        private bool WriteData(MelsecMcUdp plc, string dataType, string address, object value)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "B":
                case "S":
                case "F":
                case "T":
                case "C":
                    if (value is bool boolValue)
                    {
                        var result = plc.Write($"{dataType}{address}", boolValue);
                        return result.IsSuccess;
                    }
                    break;
                case "D":
                case "W":
                case "R":
                    if (value is short shortValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue1);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue1);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue1);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue1);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue1 && short.TryParse(stringValue1, out short parsedShort1))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort1);
                        return result.IsSuccess;
                    }
                    break;
                case "TN":
                case "CN":
                    if (value is short shortValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && short.TryParse(stringValue2, out short parsedShort2))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort2);
                        return result.IsSuccess;
                    }
                    break;
                case "D32": // 32位整型
                    if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && int.TryParse(stringValue3, out int parsedInt3))
                    {
                        var result = plc.Write($"D{address}", parsedInt3);
                        return result.IsSuccess;
                    }
                    break;
                case "Float": // 浮点数
                    if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (float)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && float.TryParse(stringValue4, out float parsedFloat4))
                    {
                        var result = plc.Write($"D{address}", parsedFloat4);
                        return result.IsSuccess;
                    }
                    break;
                case "DFloat": // 双精度浮点
                    if (value is double doubleValue5)
                    {
                        var result = plc.Write($"D{address}", doubleValue5);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue5)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue5);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue5)
                    {
                        var result = plc.Write($"D{address}", (double)intValue5);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue5)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue5);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue5 && double.TryParse(stringValue5, out double parsedDouble5))
                    {
                        var result = plc.Write($"D{address}", parsedDouble5);
                        return result.IsSuccess;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// 通用写入数据方法 - MelsecMcAsciiUdp
        /// </summary>
        private bool WriteData(MelsecMcAsciiUdp plc, string dataType, string address, object value)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "B":
                case "S":
                case "F":
                case "T":
                case "C":
                    if (value is bool boolValue)
                    {
                        var result = plc.Write($"{dataType}{address}", boolValue);
                        return result.IsSuccess;
                    }
                    break;
                case "D":
                case "W":
                case "R":
                    if (value is short shortValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue1);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue1);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue1);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue1);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue1 && short.TryParse(stringValue1, out short parsedShort1))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort1);
                        return result.IsSuccess;
                    }
                    break;
                case "TN":
                case "CN":
                    if (value is short shortValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && short.TryParse(stringValue2, out short parsedShort2))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort2);
                        return result.IsSuccess;
                    }
                    break;
                case "D32": // 32位整型
                    if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && int.TryParse(stringValue3, out int parsedInt3))
                    {
                        var result = plc.Write($"D{address}", parsedInt3);
                        return result.IsSuccess;
                    }
                    break;
                case "Float": // 浮点数
                    if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (float)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && float.TryParse(stringValue4, out float parsedFloat4))
                    {
                        var result = plc.Write($"D{address}", parsedFloat4);
                        return result.IsSuccess;
                    }
                    break;
                case "DFloat": // 双精度浮点
                    if (value is double doubleValue5)
                    {
                        var result = plc.Write($"D{address}", doubleValue5);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue5)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue5);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue5)
                    {
                        var result = plc.Write($"D{address}", (double)intValue5);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue5)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue5);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue5 && double.TryParse(stringValue5, out double parsedDouble5))
                    {
                        var result = plc.Write($"D{address}", parsedDouble5);
                        return result.IsSuccess;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// 通用写入数据方法 - MelsecA1ENet
        /// </summary>
        private bool WriteData(MelsecA1ENet plc, string dataType, string address, object value)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "B":
                case "S":
                case "F":
                case "T":
                case "C":
                    if (value is bool boolValue)
                    {
                        var result = plc.Write($"{dataType}{address}", boolValue);
                        return result.IsSuccess;
                    }
                    break;
                case "D":
                case "W":
                case "R":
                    if (value is short shortValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue1);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue1);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue1);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue1);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue1 && short.TryParse(stringValue1, out short parsedShort1))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort1);
                        return result.IsSuccess;
                    }
                    break;
                case "TN":
                case "CN":
                    if (value is short shortValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && short.TryParse(stringValue2, out short parsedShort2))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort2);
                        return result.IsSuccess;
                    }
                    break;
                case "D32": // 32位整型
                    if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && int.TryParse(stringValue3, out int parsedInt3))
                    {
                        var result = plc.Write($"D{address}", parsedInt3);
                        return result.IsSuccess;
                    }
                    break;
                case "Float": // 浮点数
                    if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (float)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && float.TryParse(stringValue4, out float parsedFloat4))
                    {
                        var result = plc.Write($"D{address}", parsedFloat4);
                        return result.IsSuccess;
                    }
                    break;
                case "DFloat": // 双精度浮点
                    if (value is double doubleValue5)
                    {
                        var result = plc.Write($"D{address}", doubleValue5);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue5)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue5);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue5)
                    {
                        var result = plc.Write($"D{address}", (double)intValue5);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue5)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue5);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue5 && double.TryParse(stringValue5, out double parsedDouble5))
                    {
                        var result = plc.Write($"D{address}", parsedDouble5);
                        return result.IsSuccess;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// 通用写入数据方法 - MelsecA1EAsciiNet
        /// </summary>
        private bool WriteData(MelsecA1EAsciiNet plc, string dataType, string address, object value)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "B":
                case "S":
                case "F":
                case "T":
                case "C":
                    if (value is bool boolValue)
                    {
                        var result = plc.Write($"{dataType}{address}", boolValue);
                        return result.IsSuccess;
                    }
                    break;
                case "D":
                case "W":
                case "R":
                    if (value is short shortValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue1);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue1);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue1);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue1)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue1);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue1 && short.TryParse(stringValue1, out short parsedShort1))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort1);
                        return result.IsSuccess;
                    }
                    break;
                case "TN":
                case "CN":
                    if (value is short shortValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"{dataType}{address}", (short)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && short.TryParse(stringValue2, out short parsedShort2))
                    {
                        var result = plc.Write($"{dataType}{address}", parsedShort2);
                        return result.IsSuccess;
                    }
                    break;
                case "D32": // 32位整型
                    if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && int.TryParse(stringValue3, out int parsedInt3))
                    {
                        var result = plc.Write($"D{address}", parsedInt3);
                        return result.IsSuccess;
                    }
                    break;
                case "Float": // 浮点数
                    if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (float)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && float.TryParse(stringValue4, out float parsedFloat4))
                    {
                        var result = plc.Write($"D{address}", parsedFloat4);
                        return result.IsSuccess;
                    }
                    break;
                case "DFloat": // 双精度浮点
                    if (value is double doubleValue5)
                    {
                        var result = plc.Write($"D{address}", doubleValue5);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue5)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue5);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue5)
                    {
                        var result = plc.Write($"D{address}", (double)intValue5);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue5)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue5);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue5 && double.TryParse(stringValue5, out double parsedDouble5))
                    {
                        var result = plc.Write($"D{address}", parsedDouble5);
                        return result.IsSuccess;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// 通用读取数据方法 - ModbusTcpNet
        /// </summary>
        private object ReadData(HslCommunication.ModBus.ModbusTcpNet plc, string dataType, string address, ushort length)
        {
            switch (dataType)
            {
                case "M": // 中间继电器 -> Modbus线圈
                case "X": // 输入继电器 -> Modbus离散输入
                case "Y": // 输出继电器 -> Modbus线圈
                case "L": // 锁存继电器 -> Modbus线圈
                case "B": // 连接继电器 -> Modbus线圈
                case "S": // 状态继电器 -> Modbus线圈
                case "F": // 报警器 -> Modbus线圈
                case "T": // 定时器触点 -> Modbus线圈
                case "C": // 计数器触点 -> Modbus线圈
                    // 对于布尔类型，使用ReadCoil
                    var boolResult = plc.ReadCoil(address, length);
                    if (boolResult.IsSuccess)
                    {
                        return boolResult.Content;
                    }
                    throw new Exception(boolResult.Message);
                case "D": // 数据寄存器 -> Modbus保持寄存器
                case "W": // 链接寄存器 -> Modbus保持寄存器
                case "R": // 文件寄存器 -> Modbus保持寄存器
                case "TN": // 定时器当前值 -> Modbus保持寄存器
                case "CN": // 计数器当前值 -> Modbus保持寄存器
                    // 对于16位整数，使用ReadInt16
                    var shortResult = plc.ReadInt16(address, length);
                    if (shortResult.IsSuccess)
                    {
                        return shortResult.Content;
                    }
                    throw new Exception(shortResult.Message);
                case "D32": // 32位整型 -> Modbus保持寄存器
                    // 对于32位整数，使用ReadInt32
                    var intResultD32 = plc.ReadInt32(address, length);
                    if (intResultD32.IsSuccess)
                    {
                        return intResultD32.Content;
                    }
                    throw new Exception(intResultD32.Message);
                case "Float": // 浮点数 -> Modbus保持寄存器
                    // 对于浮点数，使用ReadFloat
                    var floatResult = plc.ReadFloat(address, length);
                    if (floatResult.IsSuccess)
                    {
                        return floatResult.Content;
                    }
                    throw new Exception(floatResult.Message);
                case "DFloat": // 双精度浮点 -> Modbus保持寄存器
                    // 对于双精度浮点数，使用ReadDouble
                    var doubleResult = plc.ReadDouble(address, length);
                    if (doubleResult.IsSuccess)
                    {
                        return doubleResult.Content;
                    }
                    throw new Exception(doubleResult.Message);
                default:
                    throw new NotSupportedException($"不支持的数据类型: {dataType}");
            }
        }

        /// <summary>
        /// 通用写入数据方法 - ModbusTcpNet
        /// </summary>
        private bool WriteData(HslCommunication.ModBus.ModbusTcpNet plc, string dataType, string address, object value)
        {
            switch (dataType)
            {
                case "M": // 中间继电器 -> Modbus线圈
                case "Y": // 输出继电器 -> Modbus线圈
                case "L": // 锁存继电器 -> Modbus线圈
                case "B": // 连接继电器 -> Modbus线圈
                case "S": // 状态继电器 -> Modbus线圈
                case "F": // 报警器 -> Modbus线圈
                case "T": // 定时器触点 -> Modbus线圈
                case "C": // 计数器触点 -> Modbus线圈
                    if (value is bool boolValue)
                    {
                        var result = plc.Write(address, boolValue);
                        return result.IsSuccess;
                    }
                    break;
                case "D": // 数据寄存器 -> Modbus保持寄存器
                case "W": // 链接寄存器 -> Modbus保持寄存器
                case "R": // 文件寄存器 -> Modbus保持寄存器
                case "TN": // 定时器当前值 -> Modbus保持寄存器
                case "CN": // 计数器当前值 -> Modbus保持寄存器
                    if (value is short shortValue1)
                    {
                        var result = plc.Write(address, new short[] { shortValue1 });
                        return result.IsSuccess;
                    }
                    else if (value is int intValue1)
                    {
                        var result = plc.Write(address, new short[] { (short)intValue1 });
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue1)
                    {
                        var result = plc.Write(address, new short[] { (short)floatValue1 });
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue1)
                    {
                        var result = plc.Write(address, new short[] { (short)doubleValue1 });
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue1 && short.TryParse(stringValue1, out short parsedShort1))
                    {
                        var result = plc.Write(address, new short[] { parsedShort1 });
                        return result.IsSuccess;
                    }
                    break;
                case "D32": // 32位整型 -> Modbus保持寄存器
                    if (value is int intValue3)
                    {
                        var result = plc.Write(address, intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write(address, (int)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue3)
                    {
                        var result = plc.Write(address, (int)floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write(address, (int)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && int.TryParse(stringValue3, out int parsedInt3))
                    {
                        var result = plc.Write(address, parsedInt3);
                        return result.IsSuccess;
                    }
                    break;
                case "Float": // 浮点数 -> Modbus保持寄存器
                    if (value is float floatValue4)
                    {
                        var result = plc.Write(address, floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write(address, (float)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write(address, (float)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue4)
                    {
                        var result = plc.Write(address, (float)doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && float.TryParse(stringValue4, out float parsedFloat4))
                    {
                        var result = plc.Write(address, parsedFloat4);
                        return result.IsSuccess;
                    }
                    break;
                case "DFloat": // 双精度浮点 -> Modbus保持寄存器
                    if (value is double doubleValue5)
                    {
                        var result = plc.Write(address, doubleValue5);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue5)
                    {
                        var result = plc.Write(address, (double)floatValue5);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue5)
                    {
                        var result = plc.Write(address, (double)intValue5);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue5)
                    {
                        var result = plc.Write(address, (double)shortValue5);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue5 && double.TryParse(stringValue5, out double parsedDouble5))
                    {
                        var result = plc.Write(address, parsedDouble5);
                        return result.IsSuccess;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        /// <returns>是否已连接</returns>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// 获取最后发送的通讯帧
        /// </summary>
        /// <returns>通讯帧</returns>
        public byte[] LastSentFrame
        {
            get
            {
                // HslCommunication库版本不支持获取通讯帧
                return null;
            }
        }

        /// <summary>
        /// 获取最后接收的通讯帧
        /// </summary>
        /// <returns>通讯帧</returns>
        public byte[] LastReceivedFrame
        {
            get
            {
                // HslCommunication库版本不支持获取通讯帧
                return null;
            }
        }
    }
}