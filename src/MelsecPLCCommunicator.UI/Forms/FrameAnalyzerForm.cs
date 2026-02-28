using System;
using System.Windows.Forms;
using MelsecPLCCommunicator.Infrastructure.Services;

namespace MelsecPLCCommunicator.UI.Forms
{
    public partial class FrameAnalyzerForm : Form
    {
        private readonly FrameParserService _frameParserService;

        public FrameAnalyzerForm()
        {
            InitializeComponent();
            _frameParserService = new FrameParserService();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "帧分析器";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            // 创建面板
            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100
            };

            var panelBottom = new Panel
            {
                Dock = DockStyle.Fill
            };

            // 创建顶部控件
            var labelFrame = new Label
            {
                Text = "通讯帧：",
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };

            var txtFrame = new TextBox
            {
                Location = new System.Drawing.Point(80, 10),
                Width = 680,
                Height = 60,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            var btnParse = new Button
            {
                Text = "解析",
                Location = new System.Drawing.Point(770, 10),
                Width = 80
            };
            btnParse.Click += (sender, e) => ParseFrame(txtFrame.Text);

            var chkIsSendFrame = new CheckBox
            {
                Text = "是发送帧",
                Location = new System.Drawing.Point(80, 75),
                Checked = true
            };

            // 添加顶部控件
            panelTop.Controls.Add(labelFrame);
            panelTop.Controls.Add(txtFrame);
            panelTop.Controls.Add(btnParse);
            panelTop.Controls.Add(chkIsSendFrame);

            // 创建底部控件
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 基本信息标签页
            var tabBasic = new TabPage("基本信息");
            var txtBasicInfo = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };
            tabBasic.Controls.Add(txtBasicInfo);

            // 数据详情标签页
            var tabData = new TabPage("数据详情");
            var txtDataInfo = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };
            tabData.Controls.Add(txtDataInfo);

            // 软元件列表标签页
            var tabItems = new TabPage("软元件列表");
            var dgvItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false
            };
            dgvItems.Columns.Add("DeviceCode", "软元件代码");
            dgvItems.Columns.Add("DeviceName", "软元件名称");
            dgvItems.Columns.Add("Address", "地址");
            dgvItems.Columns.Add("Length", "长度");
            tabItems.Controls.Add(dgvItems);

            // 添加标签页
            tabControl.TabPages.Add(tabBasic);
            tabControl.TabPages.Add(tabData);
            tabControl.TabPages.Add(tabItems);

            // 添加底部控件
            panelBottom.Controls.Add(tabControl);

            // 添加面板到窗体
            this.Controls.Add(panelTop);
            this.Controls.Add(panelBottom);

            // 解析按钮点击事件
            btnParse.Click += (sender, e) =>
            {
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
            };
        }

        private void ParseFrame(string frame)
        {
            // 解析逻辑已在InitializeUI中实现
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Name = "FrameAnalyzerForm";
            this.Text = "帧分析器";
            this.ResumeLayout(false);
        }
    }
}