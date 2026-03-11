using System;
using System.Windows.Forms;
using MelsecPLCCommunicator.Application.Interfaces;

namespace MelsecPLCCommunicator.UI.Forms
{
    public partial class DeviceMonitorForm : Form
    {
        private readonly IPlcReadWriteService _readWriteService;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.GroupBox groupBoxDeviceStatus;
        private System.Windows.Forms.DataGridView dgvDeviceStatus;

        public DeviceMonitorForm(IPlcReadWriteService readWriteService)
        {
            _readWriteService = readWriteService;
            InitializeComponent();
            InitializeDataGridViewColumns();
            UpdateDeviceStatus();
        }

        private void InitializeDataGridViewColumns()
        {
            if (dgvDeviceStatus != null)
            {
                dgvDeviceStatus.Columns.Add("DeviceName", "设备名称");
                dgvDeviceStatus.Columns.Add("DeviceType", "设备类型");
                dgvDeviceStatus.Columns.Add("Status", "状态");
                dgvDeviceStatus.Columns.Add("LastUpdate", "最后更新时间");
                dgvDeviceStatus.Columns.Add("Details", "详细信息");

                // 设置列宽
                dgvDeviceStatus.Columns["DeviceName"].Width = 200;
                dgvDeviceStatus.Columns["DeviceType"].Width = 150;
                dgvDeviceStatus.Columns["Status"].Width = 120;
                dgvDeviceStatus.Columns["LastUpdate"].Width = 200;
                dgvDeviceStatus.Columns["Details"].Width = 300;
            }
        }

        private void UpdateDeviceStatus()
        {
            if (dgvDeviceStatus == null)
                return;

            // 清空现有列表
            dgvDeviceStatus.Rows.Clear();

            // 模拟设备状态数据
            var devices = new[]
            {
                new { Name = "PLC控制器", Type = "三菱Q系列", Status = "正常", LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Details = "运行中" },
                new { Name = "HMI触摸屏", Type = "三菱GT2000", Status = "正常", LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Details = "运行中" },
                new { Name = "变频器", Type = "三菱FR-F840", Status = "正常", LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Details = "运行中" },
                new { Name = "伺服驱动器", Type = "三菱MR-J4", Status = "正常", LastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Details = "运行中" }
            };

            foreach (var device in devices)
            {
                var row = dgvDeviceStatus.Rows.Add();
                dgvDeviceStatus.Rows[row].Cells["DeviceName"].Value = device.Name;
                dgvDeviceStatus.Rows[row].Cells["DeviceType"].Value = device.Type;
                dgvDeviceStatus.Rows[row].Cells["Status"].Value = device.Status;
                dgvDeviceStatus.Rows[row].Cells["LastUpdate"].Value = device.LastUpdate;
                dgvDeviceStatus.Rows[row].Cells["Details"].Value = device.Details;

                // 设置状态单元格的颜色
                if (device.Status == "正常")
                {
                    dgvDeviceStatus.Rows[row].Cells["Status"].Style.BackColor = System.Drawing.Color.Green;
                    dgvDeviceStatus.Rows[row].Cells["Status"].Style.ForeColor = System.Drawing.Color.White;
                }
                else if (device.Status == "警告")
                {
                    dgvDeviceStatus.Rows[row].Cells["Status"].Style.BackColor = System.Drawing.Color.Yellow;
                    dgvDeviceStatus.Rows[row].Cells["Status"].Style.ForeColor = System.Drawing.Color.Black;
                }
                else if (device.Status == "错误")
                {
                    dgvDeviceStatus.Rows[row].Cells["Status"].Style.BackColor = System.Drawing.Color.Red;
                    dgvDeviceStatus.Rows[row].Cells["Status"].Style.ForeColor = System.Drawing.Color.White;
                }
            }
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelMain = new System.Windows.Forms.Panel();
            this.groupBoxDeviceStatus = new System.Windows.Forms.GroupBox();
            this.dgvDeviceStatus = new System.Windows.Forms.DataGridView();
            this.panelMain.SuspendLayout();
            this.groupBoxDeviceStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeviceStatus)).BeginInit();
            this.SuspendLayout();
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.groupBoxDeviceStatus);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(0, 0);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(672, 188);
            this.panelMain.TabIndex = 0;
            // 
            // groupBoxDeviceStatus
            // 
            this.groupBoxDeviceStatus.Controls.Add(this.dgvDeviceStatus);
            this.groupBoxDeviceStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxDeviceStatus.Location = new System.Drawing.Point(0, 0);
            this.groupBoxDeviceStatus.Name = "groupBoxDeviceStatus";
            this.groupBoxDeviceStatus.Size = new System.Drawing.Size(672, 188);
            this.groupBoxDeviceStatus.TabIndex = 0;
            this.groupBoxDeviceStatus.TabStop = false;
            this.groupBoxDeviceStatus.Text = "设备状态";
            // 
            // dgvDeviceStatus
            // 
            this.dgvDeviceStatus.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvDeviceStatus.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvDeviceStatus.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDeviceStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvDeviceStatus.Location = new System.Drawing.Point(3, 22);
            this.dgvDeviceStatus.Name = "dgvDeviceStatus";
            this.dgvDeviceStatus.RowHeadersWidth = 60;
            this.dgvDeviceStatus.RowTemplate.Height = 40;
            this.dgvDeviceStatus.Size = new System.Drawing.Size(666, 163);
            this.dgvDeviceStatus.TabIndex = 0;
            // 
            // DeviceMonitorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(672, 188);
            this.Controls.Add(this.panelMain);
            this.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "DeviceMonitorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "设备监控";
            this.panelMain.ResumeLayout(false);
            this.groupBoxDeviceStatus.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvDeviceStatus)).EndInit();
            this.ResumeLayout(false);

        }
    }
}