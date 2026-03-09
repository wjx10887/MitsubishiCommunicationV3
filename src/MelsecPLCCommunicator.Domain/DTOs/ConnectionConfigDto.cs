namespace MelsecPLCCommunicator.Domain.DTOs
{
    /// <summary>
    /// 连接配置DTO
    /// </summary>
    public class ConnectionConfigDto
    {
        /// <summary>
        /// 连接名称
        /// </summary>
        public string ConnectionName { get; set; }

        /// <summary>
        /// PLC系列
        /// </summary>
        public string PlcSeries { get; set; }

        /// <summary>
        /// 物理接口类型
        /// </summary>
        public string InterfaceType { get; set; }

        /// <summary>
        /// 协议类型
        /// </summary>
        public string ProtocolType { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 串口名称
        /// </summary>
        public string PortName { get; set; }

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate { get; set; }

        /// <summary>
        /// 校验位
        /// </summary>
        public string Parity { get; set; }

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits { get; set; }

        /// <summary>
        /// 停止位
        /// </summary>
        public int StopBits { get; set; }

        /// <summary>
        /// PLC型号
        /// </summary>
        public string PlcModel { get; set; }

        /// <summary>
        /// 网络号
        /// </summary>
        public int NetworkNumber { get; set; }

        /// <summary>
        /// 站号
        /// </summary>
        public int StationNumber { get; set; }

        /// <summary>
        /// 连接超时
        /// </summary>
        public int ConnectionTimeout { get; set; }

        /// <summary>
        /// 接收超时
        /// </summary>
        public int ReceiveTimeout { get; set; }

        /// <summary>
        /// 自动重连
        /// </summary>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// 重连间隔
        /// </summary>
        public int ReconnectInterval { get; set; }

        /// <summary>
        /// 本地IP地址
        /// </summary>
        public string LocalIpAddress { get; set; }

        /// <summary>
        /// 本地端口
        /// </summary>
        public int LocalPort { get; set; }
    }
}
