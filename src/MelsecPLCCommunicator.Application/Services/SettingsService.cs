using System.IO;
using Newtonsoft.Json;
using MelsecPLCCommunicator.Application.Interfaces;
using MelsecPLCCommunicator.Domain.DTOs;

namespace MelsecPLCCommunicator.Application.Services
{
    /// <summary>
    /// 设置服务实现
    /// </summary>
    public class SettingsService : ISettingsService
    {
        /// <summary>
        /// 保存连接配置
        /// </summary>
        /// <param name="config">连接配置</param>
        /// <param name="fileName">文件名</param>
        /// <returns>是否保存成功</returns>
        public bool SaveConnectionConfig(ConnectionConfigDto config, string fileName)
        {
            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 序列化并保存
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(fileName, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 加载连接配置
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>连接配置</returns>
        public ConnectionConfigDto LoadConnectionConfig(string fileName)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    return new ConnectionConfigDto();
                }

                // 读取并反序列化
                string json = File.ReadAllText(fileName);
                return JsonConvert.DeserializeObject<ConnectionConfigDto>(json);
            }
            catch
            {
                return new ConnectionConfigDto();
            }
        }
    }
}