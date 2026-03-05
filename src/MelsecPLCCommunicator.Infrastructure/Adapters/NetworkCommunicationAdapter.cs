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
        private MelsecMcAsciiNet _plcAscii;
        private MelsecMcUdp _plcUdp;
        private MelsecMcAsciiUdp _plcUdpAscii;
        private MelsecA1ENet _a1ePlc;
        private MelsecA1EAsciiNet _a1ePlcAscii;
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
                    case "MC Protocol (3E) - TCP (二进制)":
                        // 使用3E帧协议，TCP二进制
                        _plc = new MelsecMcNet(_ipAddress, _port);
                        _plc.LogNet = _logNet;
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
                        _plcUdp = new MelsecMcUdp(_ipAddress, _port);
                        _plcUdp.LogNet = _logNet;
                        // UDP不需要连接，直接设置为已连接
                        _isConnected = true;
                        return _isConnected;
                    case "MC Protocol (3E) - UDP (ASCII)":
                        // 使用3E帧协议，UDP ASCII
                        _plcUdpAscii = new MelsecMcAsciiUdp(_ipAddress, _port);
                        _plcUdpAscii.LogNet = _logNet;
                        // UDP不需要连接，直接设置为已连接
                        _isConnected = true;
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
            if (_plc != null)
            {
                _plc.ConnectClose();
            }
            if (_plcAscii != null)
            {
                _plcAscii.ConnectClose();
            }
            // UDP不需要断开连接
            if (_a1ePlc != null)
            {
                _a1ePlc.ConnectClose();
            }
            if (_a1ePlcAscii != null)
            {
                _a1ePlcAscii.ConnectClose();
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
                return ReadData(_plcUdp, dataType, address, length);
            }
            else if (_plcUdpAscii != null)
            {
                return ReadData(_plcUdpAscii, dataType, address, length);
            }
            else if (_a1ePlc != null)
            {
                return ReadData(_a1ePlc, dataType, address, length);
            }
            else if (_a1ePlcAscii != null)
            {
                return ReadData(_a1ePlcAscii, dataType, address, length);
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
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    var intResult = plc.ReadInt32($"D{address}", length);
                    if (intResult.IsSuccess)
                    {
                        return intResult.Content;
                    }
                    throw new Exception(intResult.Message);
                case "F":
                    var floatResult = plc.ReadFloat($"D{address}", length);
                    if (floatResult.IsSuccess)
                    {
                        return floatResult.Content;
                    }
                    throw new Exception(floatResult.Message);
                case "DF":
                case "F64":
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
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    var intResult = plc.ReadInt32($"D{address}", length);
                    if (intResult.IsSuccess)
                    {
                        return intResult.Content;
                    }
                    throw new Exception(intResult.Message);
                case "F":
                    var floatResult = plc.ReadFloat($"D{address}", length);
                    if (floatResult.IsSuccess)
                    {
                        return floatResult.Content;
                    }
                    throw new Exception(floatResult.Message);
                case "DF":
                case "F64":
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
        /// 通用读取数据方法 - MelsecMcUdp
        /// </summary>
        private object ReadData(MelsecMcUdp plc, string dataType, string address, ushort length)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    var intResult = plc.ReadInt32($"D{address}", length);
                    if (intResult.IsSuccess)
                    {
                        return intResult.Content;
                    }
                    throw new Exception(intResult.Message);
                case "F":
                    var floatResult = plc.ReadFloat($"D{address}", length);
                    if (floatResult.IsSuccess)
                    {
                        return floatResult.Content;
                    }
                    throw new Exception(floatResult.Message);
                case "DF":
                case "F64":
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
        /// 通用读取数据方法 - MelsecMcAsciiUdp
        /// </summary>
        private object ReadData(MelsecMcAsciiUdp plc, string dataType, string address, ushort length)
        {
            switch (dataType)
            {
                case "M":
                case "X":
                case "Y":
                case "L":
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    var intResult = plc.ReadInt32($"D{address}", length);
                    if (intResult.IsSuccess)
                    {
                        return intResult.Content;
                    }
                    throw new Exception(intResult.Message);
                case "F":
                    var floatResult = plc.ReadFloat($"D{address}", length);
                    if (floatResult.IsSuccess)
                    {
                        return floatResult.Content;
                    }
                    throw new Exception(floatResult.Message);
                case "DF":
                case "F64":
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
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    var intResult = plc.ReadInt32($"D{address}", length);
                    if (intResult.IsSuccess)
                    {
                        return intResult.Content;
                    }
                    throw new Exception(intResult.Message);
                case "F":
                    var floatResult = plc.ReadFloat($"D{address}", length);
                    if (floatResult.IsSuccess)
                    {
                        return floatResult.Content;
                    }
                    throw new Exception(floatResult.Message);
                case "DF":
                case "F64":
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
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    var intResult = plc.ReadInt32($"D{address}", length);
                    if (intResult.IsSuccess)
                    {
                        return intResult.Content;
                    }
                    throw new Exception(intResult.Message);
                case "F":
                    var floatResult = plc.ReadFloat($"D{address}", length);
                    if (floatResult.IsSuccess)
                    {
                        return floatResult.Content;
                    }
                    throw new Exception(floatResult.Message);
                case "DF":
                case "F64":
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
                    return WriteData(_plcUdp, dataType, address, value);
                }
                else if (_plcUdpAscii != null)
                {
                    return WriteData(_plcUdpAscii, dataType, address, value);
                }
                else if (_a1ePlc != null)
                {
                    return WriteData(_a1ePlc, dataType, address, value);
                }
                else if (_a1ePlcAscii != null)
                {
                    return WriteData(_a1ePlcAscii, dataType, address, value);
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
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    if (value is int intValue2)
                    {
                        var result = plc.Write($"D{address}", intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue2)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && int.TryParse(stringValue2, out int parsedInt2))
                    {
                        var result = plc.Write($"D{address}", parsedInt2);
                        return result.IsSuccess;
                    }
                    break;
                case "F":
                    if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", (float)intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && float.TryParse(stringValue3, out float parsedFloat3))
                    {
                        var result = plc.Write($"D{address}", parsedFloat3);
                        return result.IsSuccess;
                    }
                    break;
                case "DF":
                case "F64":
                    if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (double)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && double.TryParse(stringValue4, out double parsedDouble4))
                    {
                        var result = plc.Write($"D{address}", parsedDouble4);
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
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    if (value is int intValue2)
                    {
                        var result = plc.Write($"D{address}", intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue2)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && int.TryParse(stringValue2, out int parsedInt2))
                    {
                        var result = plc.Write($"D{address}", parsedInt2);
                        return result.IsSuccess;
                    }
                    break;
                case "F":
                    if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", (float)intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && float.TryParse(stringValue3, out float parsedFloat3))
                    {
                        var result = plc.Write($"D{address}", parsedFloat3);
                        return result.IsSuccess;
                    }
                    break;
                case "DF":
                case "F64":
                    if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (double)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && double.TryParse(stringValue4, out double parsedDouble4))
                    {
                        var result = plc.Write($"D{address}", parsedDouble4);
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
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    if (value is int intValue2)
                    {
                        var result = plc.Write($"D{address}", intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue2)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && int.TryParse(stringValue2, out int parsedInt2))
                    {
                        var result = plc.Write($"D{address}", parsedInt2);
                        return result.IsSuccess;
                    }
                    break;
                case "F":
                    if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", (float)intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && float.TryParse(stringValue3, out float parsedFloat3))
                    {
                        var result = plc.Write($"D{address}", parsedFloat3);
                        return result.IsSuccess;
                    }
                    break;
                case "DF":
                case "F64":
                    if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (double)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && double.TryParse(stringValue4, out double parsedDouble4))
                    {
                        var result = plc.Write($"D{address}", parsedDouble4);
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
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    if (value is int intValue2)
                    {
                        var result = plc.Write($"D{address}", intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue2)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && int.TryParse(stringValue2, out int parsedInt2))
                    {
                        var result = plc.Write($"D{address}", parsedInt2);
                        return result.IsSuccess;
                    }
                    break;
                case "F":
                    if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", (float)intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && float.TryParse(stringValue3, out float parsedFloat3))
                    {
                        var result = plc.Write($"D{address}", parsedFloat3);
                        return result.IsSuccess;
                    }
                    break;
                case "DF":
                case "F64":
                    if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (double)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && double.TryParse(stringValue4, out double parsedDouble4))
                    {
                        var result = plc.Write($"D{address}", parsedDouble4);
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
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    if (value is int intValue2)
                    {
                        var result = plc.Write($"D{address}", intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue2)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && int.TryParse(stringValue2, out int parsedInt2))
                    {
                        var result = plc.Write($"D{address}", parsedInt2);
                        return result.IsSuccess;
                    }
                    break;
                case "F":
                    if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", (float)intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && float.TryParse(stringValue3, out float parsedFloat3))
                    {
                        var result = plc.Write($"D{address}", parsedFloat3);
                        return result.IsSuccess;
                    }
                    break;
                case "DF":
                case "F64":
                    if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (double)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && double.TryParse(stringValue4, out double parsedDouble4))
                    {
                        var result = plc.Write($"D{address}", parsedDouble4);
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
                case "TS":
                case "CS":
                case "TC":
                case "CC":
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
                case "DD":
                case "D32":
                    if (value is int intValue2)
                    {
                        var result = plc.Write($"D{address}", intValue2);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue2)
                    {
                        var result = plc.Write($"D{address}", (int)shortValue2);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue2)
                    {
                        var result = plc.Write($"D{address}", (int)floatValue2);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue2)
                    {
                        var result = plc.Write($"D{address}", (int)doubleValue2);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue2 && int.TryParse(stringValue2, out int parsedInt2))
                    {
                        var result = plc.Write($"D{address}", parsedInt2);
                        return result.IsSuccess;
                    }
                    break;
                case "F":
                    if (value is float floatValue3)
                    {
                        var result = plc.Write($"D{address}", floatValue3);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue3)
                    {
                        var result = plc.Write($"D{address}", (float)intValue3);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue3)
                    {
                        var result = plc.Write($"D{address}", (float)shortValue3);
                        return result.IsSuccess;
                    }
                    else if (value is double doubleValue3)
                    {
                        var result = plc.Write($"D{address}", (float)doubleValue3);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue3 && float.TryParse(stringValue3, out float parsedFloat3))
                    {
                        var result = plc.Write($"D{address}", parsedFloat3);
                        return result.IsSuccess;
                    }
                    break;
                case "DF":
                case "F64":
                    if (value is double doubleValue4)
                    {
                        var result = plc.Write($"D{address}", doubleValue4);
                        return result.IsSuccess;
                    }
                    else if (value is float floatValue4)
                    {
                        var result = plc.Write($"D{address}", (double)floatValue4);
                        return result.IsSuccess;
                    }
                    else if (value is int intValue4)
                    {
                        var result = plc.Write($"D{address}", (double)intValue4);
                        return result.IsSuccess;
                    }
                    else if (value is short shortValue4)
                    {
                        var result = plc.Write($"D{address}", (double)shortValue4);
                        return result.IsSuccess;
                    }
                    else if (value is string stringValue4 && double.TryParse(stringValue4, out double parsedDouble4))
                    {
                        var result = plc.Write($"D{address}", parsedDouble4);
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