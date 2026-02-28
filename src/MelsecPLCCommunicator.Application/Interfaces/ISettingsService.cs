using MelsecPLCCommunicator.Domain.DTOs;

namespace MelsecPLCCommunicator.Application.Interfaces
{
    /// <summary>
    /// 设置服务接口
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// 保存连接配置
        /// </summary>
        /// <param name="config">连接配置</param>
        /// <param name="fileName">文件名</param>
        /// <returns>是否保存成功</returns>
        bool SaveConnectionConfig(ConnectionConfigDto config, string fileName);

        /// <summary>
        /// 加载连接配置
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>连接配置</returns>
        ConnectionConfigDto LoadConnectionConfig(string fileName);
    }
}