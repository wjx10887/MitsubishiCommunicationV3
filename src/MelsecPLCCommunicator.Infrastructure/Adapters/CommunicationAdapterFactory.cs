using HslCommunication.Profinet.Melsec;
using System;

namespace MelsecPLCCommunicator.Infrastructure.Adapters
{
    /// <summary>
    /// 通信适配器工厂实现
    /// </summary>
    public class CommunicationAdapterFactory : ICommunicationAdapterFactory
    {
        /// <summary>
        /// 创建网络通信适配器
        /// </summary>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口</param>
        /// <param name="protocolType">协议类型</param>
        /// <returns>通信适配器</returns>
        public ICommunicationAdapter CreateNetworkAdapter(string ipAddress, int port, string protocolType)
        {
            return new NetworkCommunicationAdapter(ipAddress, port, protocolType);
        }

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
        public ICommunicationAdapter CreateSerialAdapter(string portName, int baudRate, string parity, int dataBits, int stopBits, string protocolType)
        {
            return new SerialCommunicationAdapter(portName, baudRate, parity, dataBits, stopBits, protocolType);
        }

        /// <summary>
        /// 根据协议类型和接口类型创建通信适配器
        /// </summary>
        /// <param name="interfaceType">接口类型</param>
        /// <param name="protocolType">协议类型</param>
        /// <param name="ipAddress">IP地址</param>
        /// <param name="port">端口</param>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率</param>
        /// <param name="parity">校验位</param>
        /// <param name="dataBits">数据位</param>
        /// <param name="stopBits">停止位</param>
        /// <returns>通信适配器</returns>
        public ICommunicationAdapter CreateAdapter(string interfaceType, string protocolType, string ipAddress = null, int port = 0, string portName = null, int baudRate = 9600, string parity = "None", int dataBits = 8, int stopBits = 1)
        {
            if (interfaceType == "以太网"||interfaceType == "以太网 (内置/模块)")
            {
                return CreateNetworkAdapter(ipAddress, port, protocolType);
            }
            else if (interfaceType == "串口"||interfaceType== "串口 (内置/模块)"||interfaceType== "串口 (模块)")
            {
                return CreateSerialAdapter(portName, baudRate, parity, dataBits, stopBits, protocolType);
            }
            else
            {
                throw new ArgumentException($"不支持的接口类型: {interfaceType}");
            }
        }
    }
}