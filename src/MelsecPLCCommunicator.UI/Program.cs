using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using MelsecPLCCommunicator.Application.Interfaces;
using MelsecPLCCommunicator.Application.Services;
using MelsecPLCCommunicator.Infrastructure.Adapters;
using MelsecPLCCommunicator.UI.Forms;

namespace MelsecPLCCommunicator.UI
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

                // 配置依赖注入
                var serviceProvider = ConfigureServices();

                // 创建主窗体
                var mainForm = serviceProvider.GetRequiredService<MainForm>();

                // 运行应用程序
                System.Windows.Forms.Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用程序异常: {ex.Message}\n{ex.StackTrace}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 配置服务
        /// </summary>
        /// <returns>服务提供器</returns>
        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // 注册基础设施层服务
            services.AddSingleton<ICommunicationAdapterFactory, CommunicationAdapterFactory>();

            // 注册应用层服务
            services.AddSingleton<IPlcConnectionService, PlcConnectionService>();
            services.AddSingleton<IPlcReadWriteService, PlcReadWriteService>();
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<ISettingsService, SettingsService>();

            // 注册服务提供者本身
            services.AddSingleton<IServiceProvider>(sp => sp);

            // 注册UI层服务
            services.AddTransient<MainForm>();
            services.AddTransient<ConnectionConfigForm>();
            services.AddTransient<ConnectionMonitorForm>();
            services.AddTransient<DeviceMonitorForm>();
            services.AddTransient<ErrorCodeLookupForm>();
            services.AddTransient<FrameAnalyzerForm>();

            return services.BuildServiceProvider();
        }
    }
}