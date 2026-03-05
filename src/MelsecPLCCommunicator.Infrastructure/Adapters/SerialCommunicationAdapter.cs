using System;
using System.IO.Ports;
using HslCommunication;
using HslCommunication.Profinet.Melsec;

namespace MelsecPLCCommunicator.Infrastructure.Adapters
{
    /// <summary>
    /// 串口通信适配器实现
    /// </summary>
    public class SerialCommunicationAdapter : ICommunicationAdapter
    {
        private readonly string _portName;
        private readonly int _baudRate;
        private readonly string _parity;
        private readonly int _dataBits;
        private readonly int _stopBits;
        private readonly string _protocolType;
        private SerialPort _serialPort;
        private MelsecA3CNet _melsecA3C;
        private MelsecFxSerial _melsecFxSerial;
        private MelsecFxLinks _melsecFxLinks;
        private bool _isConnected;
        private HslCommunication.LogNet.ILogNet _logNet;

        /// <summary>
        /// 通讯帧事件
        /// </summary>
        public event EventHandler<FrameEventArgs> FrameReceived;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="portName">端口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="parity">校验位</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="protocolType">协议类型</param>
        public SerialCommunicationAdapter(string portName, int baudRate, string parity, int dataBits, int stopBits, string protocolType)
        {
            _portName = portName;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _protocolType = protocolType;
            _isConnected = false;
            InitializeLog();
        }

        /// <summary>
        /// 初始化日志
        /// </summary>
        private void InitializeLog()
        {
            _logNet = new HslCommunication.LogNet.LogNetSingle(@"logs\serial.log");
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
                switch (_protocolType)
                {
                    case "MC协议格式3C":
                        // 使用3C帧协议
                        _melsecA3C = new MelsecA3CNet();
                        _melsecA3C.LogNet = _logNet;
                        _melsecA3C.SerialPortInni(_portName, _baudRate, _dataBits, GetStopBits(_stopBits), GetParity(_parity));
                        var a3cResult = _melsecA3C.Open();
                        _isConnected = a3cResult.IsSuccess;
                        return _isConnected;
                    case "FX编程口":
                        // 使用FX编程口协议
                        _melsecFxSerial = new MelsecFxSerial();
                        _melsecFxSerial.LogNet = _logNet;
                        _melsecFxSerial.SerialPortInni(_portName, _baudRate, _dataBits, GetStopBits(_stopBits), GetParity(_parity));
                        var fxResult = _melsecFxSerial.Open();
                        _isConnected = fxResult.IsSuccess;
                        return _isConnected;
                    case "计算机链接协议":
                        // 使用计算机链接协议
                        _melsecFxLinks = new MelsecFxLinks();
                        _melsecFxLinks.LogNet = _logNet;
                        _melsecFxLinks.SerialPortInni(_portName, _baudRate, _dataBits, GetStopBits(_stopBits), GetParity(_parity));
                        var linksResult = _melsecFxLinks.Open();
                        _isConnected = linksResult.IsSuccess;
                        return _isConnected;
                    default:
                        // 使用普通串口通信
                        _serialPort = new SerialPort(_portName, _baudRate, GetParity(_parity), _dataBits, GetStopBits(_stopBits));
                        _serialPort.Open();
                        _isConnected = true;
                        return true;
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
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
            }
            if (_melsecA3C != null)
            {
                _melsecA3C.Close();
            }
            if (_melsecFxSerial != null)
            {
                _melsecFxSerial.Close();
            }
            if (_melsecFxLinks != null)
            {
                _melsecFxLinks.Close();
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

            if (_melsecA3C != null)
            {
                return ReadData(_melsecA3C, dataType, address, length);
            }
            else if (_melsecFxSerial != null)
            {
                return ReadData(_melsecFxSerial, dataType, address, length);
            }
            else if (_melsecFxLinks != null)
            {
                return ReadData(_melsecFxLinks, dataType, address, length);
            }
            else
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
                        return new bool[length];
                    case "D":
                    case "W":
                    case "R":
                        return new short[length];
                    case "DD":
                    case "D32":
                        return new int[length];
                    case "F":
                        return new float[length];
                    case "DF":
                    case "F64":
                        return new double[length];
                    default:
                        throw new NotSupportedException($"不支持的数据类型: {dataType}");
                }
            }
        }

        /// <summary>
        /// 通用读取数据方法 - MelsecA3CNet
        /// </summary>
        private object ReadData(MelsecA3CNet plc, string dataType, string address, ushort length)
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
        /// 通用读取数据方法 - MelsecFxSerial
        /// </summary>
        private object ReadData(MelsecFxSerial plc, string dataType, string address, ushort length)
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
        /// 通用读取数据方法 - MelsecFxLinks
        /// </summary>
        private object ReadData(MelsecFxLinks plc, string dataType, string address, ushort length)
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
                if (_melsecA3C != null)
                {
                    return WriteData(_melsecA3C, dataType, address, value);
                }
                else if (_melsecFxSerial != null)
                {
                    return WriteData(_melsecFxSerial, dataType, address, value);
                }
                else if (_melsecFxLinks != null)
                {
                    return WriteData(_melsecFxLinks, dataType, address, value);
                }
                else
                {
                    // 暂时返回写入成功
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 通用写入数据方法 - MelsecA3CNet
        /// </summary>
        private bool WriteData(MelsecA3CNet plc, string dataType, string address, object value)
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
        /// 通用写入数据方法 - MelsecFxSerial
        /// </summary>
        private bool WriteData(MelsecFxSerial plc, string dataType, string address, object value)
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
        /// 通用写入数据方法 - MelsecFxLinks
        /// </summary>
        private bool WriteData(MelsecFxLinks plc, string dataType, string address, object value)
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

        /// <summary>
        /// 获取校验位
        /// </summary>
        /// <param name="parityStr">校验位字符串</param>
        /// <returns>校验位</returns>
        private Parity GetParity(string parityStr)
        {
            switch (parityStr.ToLower())
            {
                case "odd":
                    return Parity.Odd;
                case "even":
                    return Parity.Even;
                default:
                    return Parity.None;
            }
        }

        /// <summary>
        /// 获取停止位
        /// </summary>
        /// <param name="stopBits">停止位</param>
        /// <returns>停止位</returns>
        private StopBits GetStopBits(int stopBits)
        {
            switch (stopBits)
            {
                case 2:
                    return StopBits.Two;
                default:
                    return StopBits.One;
            }
        }
    }
} 