using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MelsecPLCCommunicator.Application.Interfaces;
using MelsecPLCCommunicator.Application.Services;
using MelsecPLCCommunicator.Infrastructure.Adapters;
using MelsecPLCCommunicator.Infrastructure.Services;
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

            // 预加载 PlcDataLogger，确保它被初始化
            try
            {
                var plcDataLogger = serviceProvider.GetRequiredService<PlcDataLogger>();
                Console.WriteLine("PlcDataLogger 初始化成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PlcDataLogger 初始化失败: {ex.Message}");
                throw;
            }

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

            // 加载配置文件
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // 获取数据库连接字符串
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=Database\\plc_communication.db;Version=3;";

            // 确保 Database 文件夹存在
            string databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database");
            if (!Directory.Exists(databasePath))
            {
                Directory.CreateDirectory(databasePath);
                Console.WriteLine("创建 Database 文件夹: {0}", databasePath);
            }

            // 注册基础设施层服务
            services.AddSingleton<ICommunicationAdapterFactory, CommunicationAdapterFactory>();
            services.AddSingleton(new ConnectionPool(connectionString));
            services.AddSingleton(new DatabaseService(connectionString));
            services.AddSingleton(new CsvExporter());
            services.AddSingleton(new DataRecordQueue());

            // 注册应用层服务
            services.AddSingleton<IPlcConnectionService, PlcConnectionService>();
            services.AddSingleton<IPlcReadWriteService, PlcReadWriteService>();
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<HistoryQueryService>();
            services.AddSingleton<PlcDataLogger>();

            // 注册服务提供者本身
            services.AddSingleton<IServiceProvider>(sp => sp);

            // 注册UI层服务
            services.AddTransient<MainForm>();
            services.AddTransient<ConnectionConfigForm>();
            services.AddTransient<ConnectionMonitorForm>();
            services.AddTransient<DeviceMonitorForm>();
            services.AddTransient<ErrorCodeLookupForm>();
            services.AddTransient<FrameAnalyzerForm>();
            services.AddTransient<HistoryQueryForm>();

            return services.BuildServiceProvider();
        }
    }
}