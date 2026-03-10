using System;
using System.Windows.Forms;
using MelsecPLCCommunicator.Application.Interfaces;
using MelsecPLCCommunicator.Domain.DTOs;

namespace MelsecPLCCommunicator.UI.Forms
{
    public partial class ConnectionMonitorForm : Form
    {
        private readonly IPlcConnectionService _connectionService;
        private System.Windows.Forms.Timer _statusTimer;

        public ConnectionMonitorForm(IPlcConnectionService connectionService)
        {
            InitializeComponent();
            _connectionService = connectionService;
            InitializeUI();
            StartStatusTimer();
        }

        private DataGridView dgvConnections;

        private void InitializeUI()
        {
            this.Text = "连接监控";
            this.Size = new System.Drawing.Size(1600, 1000);
            this.StartPosition = FormStartPosition.CenterParent;

            // 创建面板
            var panelMain = new Panel
            {
                Dock = DockStyle.Fill
            };

            // 创建连接列表
            var groupBoxConnections = new GroupBox
            {
                Text = "连接列表",
                Dock = DockStyle.Fill
            };

            dgvConnections = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                RowHeadersWidth = 60,
                Font = new System.Drawing.Font("微软雅黑", 11.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134))),
                ColumnHeadersDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
                {
                    Font = new System.Drawing.Font("微软雅黑", 11.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134))),
                    BackColor = System.Drawing.Color.LightSteelBlue,
                    ForeColor = System.Drawing.Color.DarkBlue
                },
                AlternatingRowsDefaultCellStyle = new System.Windows.Forms.DataGridViewCellStyle
                {
                    BackColor = System.Drawing.Color.LightCyan
                }
            };
            dgvConnections.RowTemplate.Height = 45;
            dgvConnections.Columns.Add("ConnectionName", "连接名称");
            dgvConnections.Columns.Add("ConnectionType", "连接方式");
            dgvConnections.Columns.Add("PlcSeries", "PLC系列");
            dgvConnections.Columns.Add("Protocol", "协议类型");
            dgvConnections.Columns.Add("Address", "地址");
            dgvConnections.Columns.Add("Port", "端口");
            dgvConnections.Columns.Add("Status", "连接状态");
            dgvConnections.Columns.Add("AlarmCode", "报警代码");

            // 设置列宽
            dgvConnections.Columns["ConnectionName"].Width = 200;
            dgvConnections.Columns["ConnectionType"].Width = 150;
            dgvConnections.Columns["PlcSeries"].Width = 120;
            dgvConnections.Columns["Protocol"].Width = 150;
            dgvConnections.Columns["Address"].Width = 200;
            dgvConnections.Columns["Port"].Width = 100;
            dgvConnections.Columns["Status"].Width = 120;
            dgvConnections.Columns["AlarmCode"].Width = 120;

            groupBoxConnections.Controls.Add(dgvConnections);
            panelMain.Controls.Add(groupBoxConnections);

            // 添加面板到窗体
            this.Controls.Add(panelMain);

            // 初始化连接列表
            UpdateConnectionList();

            // 定时更新连接状态
            _statusTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1秒更新一次
            };
            _statusTimer.Tick += (sender, e) =>
            {
                UpdateConnectionList();
            };
        }

        private void UpdateConnectionList()
        {
            // 清空现有列表
            dgvConnections.Rows.Clear();

            // 获取连接状态
            var status = _connectionService.GetConnectionStatus();
            
            // 获取当前连接配置
            var currentConfig = _connectionService.GetCurrentConfig();
            
            if (currentConfig != null)
            {
                // 添加当前连接
                var row = dgvConnections.Rows.Add();
                dgvConnections.Rows[row].Cells["ConnectionName"].Value = currentConfig.ConnectionName;
                dgvConnections.Rows[row].Cells["ConnectionType"].Value = currentConfig.InterfaceType;
                dgvConnections.Rows[row].Cells["PlcSeries"].Value = currentConfig.PlcSeries;
                dgvConnections.Rows[row].Cells["Protocol"].Value = currentConfig.ProtocolType;
                dgvConnections.Rows[row].Cells["Address"].Value = currentConfig.IpAddress ?? currentConfig.PortName;
                dgvConnections.Rows[row].Cells["Port"].Value = currentConfig.Port > 0 ? currentConfig.Port.ToString() : "-";
                
                if (status.Success)
                {
                    dgvConnections.Rows[row].Cells["Status"].Value = status.Data ? "已连接" : "未连接";
                    dgvConnections.Rows[row].Cells["Status"].Style.BackColor = status.Data ? System.Drawing.Color.Green : System.Drawing.Color.Red;
                    dgvConnections.Rows[row].Cells["Status"].Style.ForeColor = System.Drawing.Color.White;
                }
                else
                {
                    dgvConnections.Rows[row].Cells["Status"].Value = "状态未知";
                    dgvConnections.Rows[row].Cells["Status"].Style.BackColor = System.Drawing.Color.Yellow;
                    dgvConnections.Rows[row].Cells["Status"].Style.ForeColor = System.Drawing.Color.Black;
                }

                // 模拟报警代码（实际应该从连接服务获取）
                dgvConnections.Rows[row].Cells["AlarmCode"].Value = "无";
            }
            else
            {
                // 没有活动连接
                var row = dgvConnections.Rows.Add();
                dgvConnections.Rows[row].Cells["ConnectionName"].Value = "无活动连接";
                dgvConnections.Rows[row].Cells["ConnectionType"].Value = "-";
                dgvConnections.Rows[row].Cells["PlcSeries"].Value = "-";
                dgvConnections.Rows[row].Cells["Protocol"].Value = "-";
                dgvConnections.Rows[row].Cells["Address"].Value = "-";
                dgvConnections.Rows[row].Cells["Port"].Value = "-";
                dgvConnections.Rows[row].Cells["Status"].Value = "未连接";
                dgvConnections.Rows[row].Cells["Status"].Style.BackColor = System.Drawing.Color.Red;
                dgvConnections.Rows[row].Cells["Status"].Style.ForeColor = System.Drawing.Color.White;
                dgvConnections.Rows[row].Cells["AlarmCode"].Value = "-";
            }
        }

        private void StartStatusTimer()
        {
            _statusTimer.Start();
        }

        private void StopStatusTimer()
        {
            if (_statusTimer != null)
            {
                _statusTimer.Stop();
                _statusTimer.Dispose();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopStatusTimer();
            base.OnFormClosing(e);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ConnectionMonitorForm
            // 
            this.ClientSize = new System.Drawing.Size(1389, 744);
            this.Name = "ConnectionMonitorForm";
            this.Text = "连接监控";
            this.ResumeLayout(false);

        }

        
    }
}