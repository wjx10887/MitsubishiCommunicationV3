using System;
using System.Windows.Forms;
using MelsecPLCCommunicator.Infrastructure.Services;

namespace MelsecPLCCommunicator.UI.Forms
{
    public partial class FrameAnalyzerForm : Form
    {
        private readonly FrameParserService _frameParserService;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label labelFrame;
        private System.Windows.Forms.TextBox txtFrame;
        private System.Windows.Forms.Button btnParse;
        private System.Windows.Forms.CheckBox chkIsSendFrame;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabBasic;
        private System.Windows.Forms.TextBox txtBasicInfo;
        private System.Windows.Forms.TabPage tabData;
        private System.Windows.Forms.TextBox txtDataInfo;
        private System.Windows.Forms.TabPage tabItems;
        private System.Windows.Forms.DataGridView dgvItems;

        public FrameAnalyzerForm()
        {
            _frameParserService = new FrameParserService();
            InitializeComponent();
            BindEvents();
        }

        private void BindEvents()
        {
            if (btnParse != null)
            {
                btnParse.Click += BtnParse_Click;
            }
            if (dgvItems != null)
            {
                InitializeDataGridViewColumns();
            }
        }

        private void InitializeDataGridViewColumns()
        {
            dgvItems.Columns.Add("DeviceCode", "软元件代码");
            dgvItems.Columns.Add("DeviceName", "软元件名称");
            dgvItems.Columns.Add("Address", "地址");
            dgvItems.Columns.Add("Length", "长度");
            dgvItems.Columns["DeviceCode"].Width = 200;
            dgvItems.Columns["DeviceName"].Width = 280;
            dgvItems.Columns["Address"].Width = 220;
            dgvItems.Columns["Length"].Width = 150;
        }

        private void BtnParse_Click(object sender, EventArgs e)
        {
            if (txtFrame == null || chkIsSendFrame == null || txtBasicInfo == null || txtDataInfo == null || dgvItems == null)
                return;

            var frame = txtFrame.Text.Trim();
            var isSendFrame = chkIsSendFrame.Checked;
            var result = _frameParserService.ParseMcFrame(frame, isSendFrame);

            // 显示基本信息
            txtBasicInfo.Clear();
            if (result.Success)
            {
                txtBasicInfo.AppendText($"原始帧: {result.RawFrame}\r\n");
                txtBasicInfo.AppendText($"字节长度: {result.ByteLength}\r\n");
                txtBasicInfo.AppendText($"帧头: {result.Header}\r\n");
                txtBasicInfo.AppendText($"网络号: {result.NetworkNumber}\r\n");
                txtBasicInfo.AppendText($"站号: {result.StationNumber}\r\n");
                txtBasicInfo.AppendText($"预留: {result.Reserved}\r\n");
                txtBasicInfo.AppendText($"命令码: {result.CommandCode} ({result.CommandName})\r\n");
                txtBasicInfo.AppendText($"数据长度: {result.DataLength}\r\n");

                if (!string.IsNullOrEmpty(result.ResponseCode))
                {
                    txtBasicInfo.AppendText($"响应码: {result.ResponseCode} ({result.ResponseMessage})\r\n");
                }
            }
            else
            {
                txtBasicInfo.AppendText($"解析失败: {result.Error}\r\n");
            }

            // 显示数据详情
            txtDataInfo.Clear();
            if (result.Success)
            {
                if (!string.IsNullOrEmpty(result.Data))
                {
                    txtDataInfo.AppendText($"数据部分: {result.Data}\r\n");
                }
                if (!string.IsNullOrEmpty(result.ResponseData))
                {
                    txtDataInfo.AppendText($"响应数据: {result.ResponseData}\r\n");
                }
            }

            // 显示软元件列表
            dgvItems.Rows.Clear();
            if (result.Success && result.Items != null)
            {
                foreach (var item in result.Items)
                {
                    dgvItems.Rows.Add(item.DeviceCode, item.DeviceName, item.Address, item.Length);
                }
            }
        }

        

        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelTop = new System.Windows.Forms.Panel();
            this.chkIsSendFrame = new System.Windows.Forms.CheckBox();
            this.btnParse = new System.Windows.Forms.Button();
            this.txtFrame = new System.Windows.Forms.TextBox();
            this.labelFrame = new System.Windows.Forms.Label();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabBasic = new System.Windows.Forms.TabPage();
            this.txtBasicInfo = new System.Windows.Forms.TextBox();
            this.tabData = new System.Windows.Forms.TabPage();
            this.txtDataInfo = new System.Windows.Forms.TextBox();
            this.tabItems = new System.Windows.Forms.TabPage();
            this.dgvItems = new System.Windows.Forms.DataGridView();
            this.panelTop.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabBasic.SuspendLayout();
            this.tabData.SuspendLayout();
            this.tabItems.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.chkIsSendFrame);
            this.panelTop.Controls.Add(this.btnParse);
            this.panelTop.Controls.Add(this.txtFrame);
            this.panelTop.Controls.Add(this.labelFrame);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1763, 340);
            this.panelTop.TabIndex = 0;
            // 
            // chkIsSendFrame
            // 
            this.chkIsSendFrame.AutoSize = true;
            this.chkIsSendFrame.Checked = true;
            this.chkIsSendFrame.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIsSendFrame.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chkIsSendFrame.Location = new System.Drawing.Point(199, 265);
            this.chkIsSendFrame.Name = "chkIsSendFrame";
            this.chkIsSendFrame.Size = new System.Drawing.Size(220, 56);
            this.chkIsSendFrame.TabIndex = 3;
            this.chkIsSendFrame.Text = "是发送帧";
            this.chkIsSendFrame.UseVisualStyleBackColor = true;
            // 
            // btnParse
            // 
            this.btnParse.BackColor = System.Drawing.Color.LightSteelBlue;
            this.btnParse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnParse.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnParse.ForeColor = System.Drawing.Color.DarkBlue;
            this.btnParse.Location = new System.Drawing.Point(1472, 40);
            this.btnParse.Name = "btnParse";
            this.btnParse.Size = new System.Drawing.Size(160, 100);
            this.btnParse.TabIndex = 2;
            this.btnParse.Text = "解析";
            this.btnParse.UseVisualStyleBackColor = false;
            // 
            // txtFrame
            // 
            this.txtFrame.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtFrame.Location = new System.Drawing.Point(199, 40);
            this.txtFrame.Multiline = true;
            this.txtFrame.Name = "txtFrame";
            this.txtFrame.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtFrame.Size = new System.Drawing.Size(1200, 219);
            this.txtFrame.TabIndex = 1;
            // 
            // labelFrame
            // 
            this.labelFrame.AutoSize = true;
            this.labelFrame.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelFrame.Location = new System.Drawing.Point(40, 40);
            this.labelFrame.Name = "labelFrame";
            this.labelFrame.Size = new System.Drawing.Size(182, 52);
            this.labelFrame.TabIndex = 0;
            this.labelFrame.Text = "通讯帧：";
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.tabControl);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBottom.Location = new System.Drawing.Point(0, 340);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(1763, 892);
            this.panelBottom.TabIndex = 1;
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabBasic);
            this.tabControl.Controls.Add(this.tabData);
            this.tabControl.Controls.Add(this.tabItems);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Drawing.Point(15, 15);
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1763, 892);
            this.tabControl.TabIndex = 0;
            // 
            // tabBasic
            // 
            this.tabBasic.Controls.Add(this.txtBasicInfo);
            this.tabBasic.Location = new System.Drawing.Point(10, 89);
            this.tabBasic.Name = "tabBasic";
            this.tabBasic.Padding = new System.Windows.Forms.Padding(3);
            this.tabBasic.Size = new System.Drawing.Size(1743, 793);
            this.tabBasic.TabIndex = 0;
            this.tabBasic.Text = "基本信息";
            this.tabBasic.UseVisualStyleBackColor = true;
            // 
            // txtBasicInfo
            // 
            this.txtBasicInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtBasicInfo.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtBasicInfo.Location = new System.Drawing.Point(3, 3);
            this.txtBasicInfo.Multiline = true;
            this.txtBasicInfo.Name = "txtBasicInfo";
            this.txtBasicInfo.ReadOnly = true;
            this.txtBasicInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtBasicInfo.Size = new System.Drawing.Size(1737, 787);
            this.txtBasicInfo.TabIndex = 0;
            // 
            // tabData
            // 
            this.tabData.Controls.Add(this.txtDataInfo);
            this.tabData.Location = new System.Drawing.Point(10, 89);
            this.tabData.Name = "tabData";
            this.tabData.Padding = new System.Windows.Forms.Padding(3);
            this.tabData.Size = new System.Drawing.Size(1743, 793);
            this.tabData.TabIndex = 1;
            this.tabData.Text = "数据详情";
            this.tabData.UseVisualStyleBackColor = true;
            // 
            // txtDataInfo
            // 
            this.txtDataInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDataInfo.Font = new System.Drawing.Font("Consolas", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDataInfo.Location = new System.Drawing.Point(3, 3);
            this.txtDataInfo.Multiline = true;
            this.txtDataInfo.Name = "txtDataInfo";
            this.txtDataInfo.ReadOnly = true;
            this.txtDataInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDataInfo.Size = new System.Drawing.Size(1737, 787);
            this.txtDataInfo.TabIndex = 0;
            // 
            // tabItems
            // 
            this.tabItems.Controls.Add(this.dgvItems);
            this.tabItems.Location = new System.Drawing.Point(10, 89);
            this.tabItems.Name = "tabItems";
            this.tabItems.Padding = new System.Windows.Forms.Padding(3);
            this.tabItems.Size = new System.Drawing.Size(1743, 793);
            this.tabItems.TabIndex = 2;
            this.tabItems.Text = "软元件列表";
            this.tabItems.UseVisualStyleBackColor = true;
            // 
            // dgvItems
            // 
            this.dgvItems.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvItems.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvItems.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvItems.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvItems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvItems.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvItems.GridColor = System.Drawing.Color.LightGray;
            this.dgvItems.Location = new System.Drawing.Point(3, 3);
            this.dgvItems.Name = "dgvItems";
            this.dgvItems.RowHeadersWidth = 80;
            this.dgvItems.RowTemplate.Height = 50;
            this.dgvItems.Size = new System.Drawing.Size(1737, 787);
            this.dgvItems.TabIndex = 0;
            // 
            // FrameAnalyzerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(24F, 52F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1763, 1232);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "FrameAnalyzerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "帧分析器";
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.tabBasic.ResumeLayout(false);
            this.tabBasic.PerformLayout();
            this.tabData.ResumeLayout(false);
            this.tabData.PerformLayout();
            this.tabItems.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvItems)).EndInit();
            this.ResumeLayout(false);

        }
    }
}