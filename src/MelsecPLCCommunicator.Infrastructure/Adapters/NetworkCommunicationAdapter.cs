using System;
using HslCommunication;
using HslCommunication.Profinet.Melsec;

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
        private MelsecMcNet _plc;
        private MelsecA1ENet _a1ePlc;
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
        public NetworkCommunicationAdapter(string ipAddress, int port, string protocolType)
        {
            _ipAddress = ipAddress;
            _port = port;
            _protocolType = protocolType;
            _isConnected = false;
            InitializeLog();
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public NetworkCommunicationAdapter() : this("192.168.1.100", 502, "MC协议格式1")
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
        private void LogNet_BeforeSaveToFile(object sender, System.EventArgs e)
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
                    case "MC协议格式1":
                    case "MC协议格式4":
                        // 使用3E帧协议
                        _plc = new MelsecMcNet(_ipAddress, _port);
                        _plc.LogNet = _logNet;
                        var result = _plc.ConnectServer();
                        _isConnected = result.IsSuccess;
                        return _isConnected;
                    case "MC协议格式A1E":
                        // 使用1E帧协议
                        _a1ePlc = new MelsecA1ENet(_ipAddress, _port);
                        _a1ePlc.LogNet = _logNet;
                        var a1eResult = _a1ePlc.ConnectServer();
                        _isConnected = a1eResult.IsSuccess;
                        return _isConnected;
                    default:
                        // 默认使用3E帧协议
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
            if (_plc != null)
            {
                _plc.ConnectClose();
            }
            if (_a1ePlc != null)
            {
                _a1ePlc.ConnectClose();
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
                switch (dataType)
                {
                    case "M":
                    case "X":
                    case "Y":
                    case "L":
                        var boolResult = _plc.ReadBool($"{dataType}{address}", length);
                        if (boolResult.IsSuccess)
                        {
                            return boolResult.Content;
                        }
                        throw new Exception(boolResult.Message);
                    case "D":
                    case "W":
                    case "R":
                        var shortResult = _plc.ReadInt16($"{dataType}{address}", length);
                        if (shortResult.IsSuccess)
                        {
                            return shortResult.Content;
                        }
                        throw new Exception(shortResult.Message);
                    case "DD":
                        var intResult = _plc.ReadInt32($"D{address}", length);
                        if (intResult.IsSuccess)
                        {
                            return intResult.Content;
                        }
                        throw new Exception(intResult.Message);
                    default:
                        throw new NotSupportedException($"不支持的数据类型: {dataType}");
                }
            }
            else if (_a1ePlc != null)
            {
                switch (dataType)
                {
                    case "M":
                    case "X":
                    case "Y":
                    case "L":
                        var boolResult = _a1ePlc.ReadBool($"{dataType}{address}", length);
                        if (boolResult.IsSuccess)
                        {
                            return boolResult.Content;
                        }
                        throw new Exception(boolResult.Message);
                    case "D":
                    case "W":
                    case "R":
                        var shortResult = _a1ePlc.ReadInt16($"{dataType}{address}", length);
                        if (shortResult.IsSuccess)
                        {
                            return shortResult.Content;
                        }
                        throw new Exception(shortResult.Message);
                    case "DD":
                        var intResult = _a1ePlc.ReadInt32($"D{address}", length);
                        if (intResult.IsSuccess)
                        {
                            return intResult.Content;
                        }
                        throw new Exception(intResult.Message);
                    default:
                        throw new NotSupportedException($"不支持的数据类型: {dataType}");
                }
            }
            else
            {
                throw new InvalidOperationException("设备未连接");
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
                    switch (dataType)
                    {
                        case "M":
                        case "X":
                        case "Y":
                        case "L":
                            if (value is bool boolValue)
                            {
                                var result = _plc.Write($"{dataType}{address}", boolValue);
                                return result.IsSuccess;
                            }
                            break;
                        case "D":
                        case "W":
                        case "R":
                            if (value is short shortValue)
                            {
                                var result = _plc.Write($"{dataType}{address}", shortValue);
                                return result.IsSuccess;
                            }
                            break;
                        case "DD":
                            if (value is int intValue)
                            {
                                var result = _plc.Write($"D{address}", intValue);
                                return result.IsSuccess;
                            }
                            break;
                    }
                }
                else if (_a1ePlc != null)
                {
                    switch (dataType)
                    {
                        case "M":
                        case "X":
                        case "Y":
                        case "L":
                            if (value is bool boolValue)
                            {
                                var result = _a1ePlc.Write($"{dataType}{address}", boolValue);
                                return result.IsSuccess;
                            }
                            break;
                        case "D":
                        case "W":
                        case "R":
                            if (value is short shortValue)
                            {
                                var result = _a1ePlc.Write($"{dataType}{address}", shortValue);
                                return result.IsSuccess;
                            }
                            break;
                        case "DD":
                            if (value is int intValue)
                            {
                                var result = _a1ePlc.Write($"D{address}", intValue);
                                return result.IsSuccess;
                            }
                            break;
                    }
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
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