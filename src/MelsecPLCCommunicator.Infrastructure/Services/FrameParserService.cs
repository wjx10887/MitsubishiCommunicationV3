using System;
using System.Collections.Generic;
using System.Linq;

namespace MelsecPLCCommunicator.Infrastructure.Services
{
    /// <summary>
    /// 通讯帧解析服务
    /// </summary>
    public class FrameParserService
    {
        /// <summary>
        /// 解析MC协议通讯帧
        /// </summary>
        /// <param name="frame">通讯帧字符串</param>
        /// <param name="isSendFrame">是否为发送帧</param>
        /// <returns>解析结果</returns>
        public FrameParseResult ParseMcFrame(string frame, bool isSendFrame = true)
        {
            if (string.IsNullOrEmpty(frame))
            {
                return new FrameParseResult { Error = "帧数据为空" };
            }

            try
            {
                // 移除空格并转换为字节数组
                string cleanedFrame = frame.Replace(" ", "");
                byte[] bytes = new byte[cleanedFrame.Length / 2];
                for (int i = 0; i < cleanedFrame.Length; i += 2)
                {
                    bytes[i / 2] = Convert.ToByte(cleanedFrame.Substring(i, 2), 16);
                }

                var result = new FrameParseResult
                {
                    RawFrame = frame,
                    ByteLength = bytes.Length
                };

                // 解析MC协议帧结构
                if (bytes.Length < 10)
                {
                    result.Error = "帧长度不足，MC协议帧至少需要10字节";
                    return result;
                }

                // 帧头 (2字节)
                result.Header = $"{bytes[0]:X2} {bytes[1]:X2}";

                // 网络号 (1字节)
                result.NetworkNumber = bytes[2];

                // 站号 (1字节)
                result.StationNumber = bytes[3];

                // 预留 (2字节)
                result.Reserved = $"{bytes[4]:X2} {bytes[5]:X2}";

                // 命令码 (2字节)
                result.CommandCode = $"{bytes[6]:X2} {bytes[7]:X2}";
                result.CommandName = GetCommandName(bytes[6], bytes[7]);

                // 数据长度 (2字节)
                result.DataLength = (ushort)((bytes[8] << 8) | bytes[9]);

                // 数据部分
                if (bytes.Length > 10)
                {
                    byte[] dataBytes = new byte[bytes.Length - 10];
                    Array.Copy(bytes, 10, dataBytes, 0, dataBytes.Length);
                    result.Data = BitConverter.ToString(dataBytes).Replace("-", " ");
                }

                // 解析数据部分
                if (isSendFrame)
                {
                    ParseSendFrameData(result, bytes);
                }
                else
                {
                    ParseReceiveFrameData(result, bytes);
                }

                return result;
            }
            catch (Exception ex)
            {
                return new FrameParseResult { Error = $"解析失败: {ex.Message}" };
            }
        }

        /// <summary>
        /// 解析发送帧数据
        /// </summary>
        /// <param name="result">解析结果</param>
        /// <param name="bytes">帧字节数组</param>
        private void ParseSendFrameData(FrameParseResult result, byte[] bytes)
        {
            if (bytes.Length <= 10)
                return;

            // 根据命令码解析数据
            switch (result.CommandCode)
            {
                case "04 01": // 批量读取
                case "04 02": // 批量写入
                    ParseBatchCommandData(result, bytes);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 解析接收帧数据
        /// </summary>
        /// <param name="result">解析结果</param>
        /// <param name="bytes">帧字节数组</param>
        private void ParseReceiveFrameData(FrameParseResult result, byte[] bytes)
        {
            if (bytes.Length <= 10)
                return;

            // 检查响应码
            if (bytes[10] == 0 && bytes[11] == 0)
            {
                result.ResponseCode = "00 00";
                result.ResponseMessage = "正常";
            }
            else
            {
                result.ResponseCode = $"{bytes[10]:X2} {bytes[11]:X2}";
                result.ResponseMessage = GetResponseMessage(bytes[10], bytes[11]);
            }

            // 解析数据部分
            if (bytes.Length > 12)
            {
                byte[] dataBytes = new byte[bytes.Length - 12];
                Array.Copy(bytes, 12, dataBytes, 0, dataBytes.Length);
                result.ResponseData = BitConverter.ToString(dataBytes).Replace("-", " ");
            }
        }

        /// <summary>
        /// 解析批量命令数据
        /// </summary>
        /// <param name="result">解析结果</param>
        /// <param name="bytes">帧字节数组</param>
        private void ParseBatchCommandData(FrameParseResult result, byte[] bytes)
        {
            if (bytes.Length <= 12)
                return;

            // 软元件数量
            ushort itemCount = (ushort)((bytes[10] << 8) | bytes[11]);
            result.ItemCount = itemCount;

            int offset = 12;
            var items = new List<FrameItem>();

            for (int i = 0; i < itemCount && offset < bytes.Length; i++)
            {
                if (offset + 4 > bytes.Length)
                    break;

                // 软元件类型
                byte deviceCode = bytes[offset];
                string deviceName = GetDeviceName(deviceCode);

                // 软元件地址
                ushort address = (ushort)((bytes[offset + 1] << 8) | bytes[offset + 2]);

                // 软元件长度
                ushort length = (ushort)((bytes[offset + 3] << 8) | bytes[offset + 4]);

                items.Add(new FrameItem
                {
                    DeviceCode = deviceCode.ToString("X2"),
                    DeviceName = deviceName,
                    Address = address,
                    Length = length
                });

                offset += 5;
            }

            result.Items = items;
        }

        /// <summary>
        /// 获取命令名称
        /// </summary>
        /// <param name="highByte">高位字节</param>
        /// <param name="lowByte">低位字节</param>
        /// <returns>命令名称</returns>
        private string GetCommandName(byte highByte, byte lowByte)
        {
            string commandCode = $"{highByte:X2} {lowByte:X2}";
            switch (commandCode)
            {
                case "04 01": return "批量读取";
                case "04 02": return "批量写入";
                case "04 03": return "随机读取";
                case "04 04": return "随机写入";
                case "04 05": return "远程运行";
                case "04 06": return "远程停止";
                case "04 07": return "远程复位";
                default: return "未知命令";
            }
        }

        /// <summary>
        /// 获取响应消息
        /// </summary>
        /// <param name="highByte">高位字节</param>
        /// <param name="lowByte">低位字节</param>
        /// <returns>响应消息</returns>
        private string GetResponseMessage(byte highByte, byte lowByte)
        {
            string responseCode = $"{highByte:X2} {lowByte:X2}";
            switch (responseCode)
            {
                case "00 00": return "正常";
                case "01 00": return "命令错误";
                case "02 00": return "格式错误";
                case "03 00": return "数据范围错误";
                case "04 00": return "数据长度错误";
                case "05 00": return "访问错误";
                case "06 00": return "其他错误";
                default: return "未知错误";
            }
        }

        /// <summary>
        /// 获取软元件名称
        /// </summary>
        /// <param name="deviceCode">软元件代码</param>
        /// <returns>软元件名称</returns>
        private string GetDeviceName(byte deviceCode)
        {
            switch (deviceCode)
            {
                case 0x90: return "X";
                case 0x91: return "Y";
                case 0x92: return "M";
                case 0x93: return "L";
                case 0x94: return "F";
                case 0x95: return "V";
                case 0x96: return "B";
                case 0x97: return "W";
                case 0xA8: return "D";
                case 0xAF: return "R";
                default: return "未知";
            }
        }
    }

    /// <summary>
    /// 帧解析结果
    /// </summary>
    public class FrameParseResult
    {
        /// <summary>
        /// 原始帧
        /// </summary>
        public string RawFrame { get; set; }

        /// <summary>
        /// 字节长度
        /// </summary>
        public int ByteLength { get; set; }

        /// <summary>
        /// 帧头
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// 网络号
        /// </summary>
        public byte NetworkNumber { get; set; }

        /// <summary>
        /// 站号
        /// </summary>
        public byte StationNumber { get; set; }

        /// <summary>
        /// 预留
        /// </summary>
        public string Reserved { get; set; }

        /// <summary>
        /// 命令码
        /// </summary>
        public string CommandCode { get; set; }

        /// <summary>
        /// 命令名称
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// 数据长度
        /// </summary>
        public ushort DataLength { get; set; }

        /// <summary>
        /// 数据
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// 响应码
        /// </summary>
        public string ResponseCode { get; set; }

        /// <summary>
        /// 响应消息
        /// </summary>
        public string ResponseMessage { get; set; }

        /// <summary>
        /// 响应数据
        /// </summary>
        public string ResponseData { get; set; }

        /// <summary>
        /// 软元件数量
        /// </summary>
        public ushort ItemCount { get; set; }

        /// <summary>
        /// 软元件列表
        /// </summary>
        public List<FrameItem> Items { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 是否解析成功
        /// </summary>
        public bool Success => string.IsNullOrEmpty(Error);
    }

    /// <summary>
    /// 帧中的软元件项
    /// </summary>
    public class FrameItem
    {
        /// <summary>
        /// 软元件代码
        /// </summary>
        public string DeviceCode { get; set; }

        /// <summary>
        /// 软元件名称
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// 软元件地址
        /// </summary>
        public ushort Address { get; set; }

        /// <summary>
        /// 软元件长度
        /// </summary>
        public ushort Length { get; set; }
    }
}