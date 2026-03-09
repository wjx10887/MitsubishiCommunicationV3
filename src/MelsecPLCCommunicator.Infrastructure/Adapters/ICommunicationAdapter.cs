using System;

namespace MelsecPLCCommunicator.Infrastructure.Adapters
{
    /// <summary>
    /// 通讯帧事件参数
    /// </summary>
    public class FrameEventArgs : EventArgs
    {
        /// <summary>
        /// 发送帧
        /// </summary>
        public string SendFrame { get; set; }

        /// <summary>
        /// 接收帧
        /// </summary>
        public string ReceiveFrame { get; set; }
    }

    /// <summary>
    /// 通信适配器接口
    /// </summary>
    public interface ICommunicationAdapter
    {
        /// <summary>
        /// 连接到设备
        /// </summary>
        /// <returns>是否连接成功</returns>
        bool Connect();

        /// <summary>
        /// 断开连接
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 读取数据
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="length">长度</param>
        /// <returns>读取结果</returns>
        object Read(string address, string dataType, ushort length);

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="value">值</param>
        /// <returns>是否写入成功</returns>
        bool Write(string address, string dataType, object value);

        /// <summary>
        /// 检查连接状态
        /// </summary>
        /// <returns>是否已连接</returns>
        bool IsConnected { get; }

        /// <summary>
        /// 获取最后发送的通讯帧
        /// </summary>
        /// <returns>通讯帧</returns>
        byte[] LastSentFrame { get; }

        /// <summary>
        /// 获取最后接收的通讯帧
        /// </summary>
        /// <returns>通讯帧</returns>
        byte[] LastReceivedFrame { get; }

        /// <summary>
        /// 通讯帧事件
        /// </summary>
        event EventHandler<FrameEventArgs> FrameReceived;
    }

    /// <summary>
    /// 通信适配器工厂接口
    /// </summary>
    public interface ICommunicationAdapterFactory
    {
        /// <summary>
        /// 创建网络通信适配器
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口</param>
        /// <param name="protocolType">协议类型</param>
        /// <param name="localIpAddress">本地IP地址</param>
        /// <param name="localPort">本地端口</param>
        /// <param name="logService">日志服务</param>
        /// <returns>通信适配器</returns>
        ICommunicationAdapter CreateNetworkAdapter(string ipAddress, int port, string protocolType, string localIpAddress = "192.168.1.100", int localPort = 3000, object logService = null);

        /// <summary>
        /// 创建串口通信适配器
        /// </summary>
        /// <param name="portName">端口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="parity">校验位</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="protocolType">协议类型</param>
        /// <returns>通信适配器</returns>
        ICommunicationAdapter CreateSerialAdapter(string portName, int baudRate, string parity, int dataBits, int stopBits, string protocolType);

        /// <summary>
        /// 根据协议类型和接口类型创建通信适配器
        /// </summary>
        /// <param name="interfaceType">接口类型</param>
        /// <param name="protocolType">协议类型</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口</param>
        /// <param name="localIpAddress">本地IP地址</param>
        /// <param name="localPort">本地端口</param>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="parity">校验位</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <param name="logService">日志服务</param>
        /// <returns>通信适配器</returns>
        ICommunicationAdapter CreateAdapter(string interfaceType, string protocolType, string ipAddress = null, int port = 0, string localIpAddress = "192.168.1.100", int localPort = 3000, string portName = null, int baudRate = 9600, string parity = "None", int dataBits = 8, int stopBits = 1, object logService = null);
    }
}
