using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MelsecPLCCommunicator.UI.Forms
{
    public partial class ErrorCodeLookupForm : Form
    {
        private List<ErrorCodeInfo> _errorCodes;

        public ErrorCodeLookupForm()
        {
            InitializeComponent();
            _errorCodes = GetErrorCodeList();
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "错误码查询";
            this.Size = new System.Drawing.Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            // 创建面板
            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120
            };

            var panelBottom = new Panel
            {
                Dock = DockStyle.Fill
            };

            // 创建顶部控件
            var labelErrorCode = new Label
            {
                Text = "错误码：",
                Location = new System.Drawing.Point(20, 20),
                AutoSize = true,
                Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular)
            };

            var txtErrorCode = new TextBox
            {
                Location = new System.Drawing.Point(100, 17),
                Width = 250,
                Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular)
            };

            var btnLookup = new Button
            {
                Text = "查询",
                Location = new System.Drawing.Point(360, 15),
                Width = 100,
                Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular)
            };

            var labelDescription = new Label
            {
                Text = "描述：",
                Location = new System.Drawing.Point(20, 60),
                AutoSize = true,
                Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular)
            };

            var txtDescription = new TextBox
            {
                Location = new System.Drawing.Point(100, 57),
                Width = 870,
                Height = 40,
                Multiline = true,
                ReadOnly = true,
                Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular),
                ScrollBars = ScrollBars.Vertical
            };

            // 添加顶部控件
            panelTop.Controls.Add(labelErrorCode);
            panelTop.Controls.Add(txtErrorCode);
            panelTop.Controls.Add(btnLookup);
            panelTop.Controls.Add(labelDescription);
            panelTop.Controls.Add(txtDescription);

            // 创建底部控件
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 错误码列表标签页
            var tabErrorCodeList = new TabPage("错误码列表");
            var dgvErrorCodeList = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true
            };
            
            // 设置列
            var colCode = new DataGridViewTextBoxColumn
            {
                Name = "Code",
                HeaderText = "错误码",
                Width = 120
            };
            var colDescription = new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "描述",
                Width = 200
            };
            var colCause = new DataGridViewTextBoxColumn
            {
                Name = "Cause",
                HeaderText = "原因",
                Width = 300
            };
            var colSolution = new DataGridViewTextBoxColumn
            {
                Name = "Solution",
                HeaderText = "解除方式",
                Width = 300
            };
            
            dgvErrorCodeList.Columns.AddRange(new DataGridViewColumn[] { colCode, colDescription, colCause, colSolution });

            // 填充错误码列表
            foreach (var errorCode in _errorCodes)
            {
                dgvErrorCodeList.Rows.Add(errorCode.Code, errorCode.Description, errorCode.Cause, errorCode.Solution);
            }

            tabErrorCodeList.Controls.Add(dgvErrorCodeList);

            // 报警历史标签页
            var tabAlarmHistory = new TabPage("报警历史");
            var dgvAlarmHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true
            };
            
            var colTime = new DataGridViewTextBoxColumn
            {
                Name = "Time",
                HeaderText = "时间",
                Width = 150
            };
            var colErrorCode = new DataGridViewTextBoxColumn
            {
                Name = "ErrorCode",
                HeaderText = "错误码",
                Width = 100
            };
            var colDesc = new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "描述",
                Width = 300
            };
            var colStatus = new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "状态",
                Width = 100
            };
            
            dgvAlarmHistory.Columns.AddRange(new DataGridViewColumn[] { colTime, colErrorCode, colDesc, colStatus });

            // 填充报警历史（模拟数据）
            dgvAlarmHistory.Rows.Add(DateTime.Now.AddMinutes(-30), "01 00", "命令错误", "已解决");
            dgvAlarmHistory.Rows.Add(DateTime.Now.AddHours(-1), "02 00", "格式错误", "已解决");
            dgvAlarmHistory.Rows.Add(DateTime.Now.AddHours(-2), "03 00", "数据范围错误", "已解决");

            tabAlarmHistory.Controls.Add(dgvAlarmHistory);

            // 添加标签页
            tabControl.TabPages.Add(tabErrorCodeList);
            tabControl.TabPages.Add(tabAlarmHistory);

            // 添加底部控件
            panelBottom.Controls.Add(tabControl);

            // 添加面板到窗体
            this.Controls.Add(panelTop);
            this.Controls.Add(panelBottom);

            // 查询按钮点击事件
            btnLookup.Click += (sender, e) =>
            {
                var code = txtErrorCode.Text.Trim();
                var errorCode = _errorCodes.Find(ec => ec.Code == code);
                if (errorCode != null)
                {
                    txtDescription.Text = $"{errorCode.Description}\r\n原因：{errorCode.Cause}\r\n解决方法：{errorCode.Solution}";
                }
                else
                {
                    txtDescription.Text = "未找到该错误码";
                }
            };

            // 错误码列表点击事件
            dgvErrorCodeList.CellClick += (sender, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var row = dgvErrorCodeList.Rows[e.RowIndex];
                    txtErrorCode.Text = row.Cells["Code"].Value.ToString();
                    btnLookup.PerformClick();
                }
            };
        }

        private List<ErrorCodeInfo> GetErrorCodeList()
        {
            // 基于三菱PLC通讯协议手册的错误码和报警代码
            return new List<ErrorCodeInfo>
            {
               
                
                // 三菱PLC通讯错误代码（Hex）
                new ErrorCodeInfo { Code = "0x0000", Description = "正常结束", Cause = "操作成功", Solution = "无需处理" },
                new ErrorCodeInfo { Code = "0x0400", Description = "保护：无法写入", Cause = "1. 尝试向只读区域写入（如程序区、部分系统寄存器）。2. 软元件被密码保护或写保护。3. PLC处于RUN模式，禁止写入程序。", Solution = "1. 确认目标地址是否为可读写区域。2. 检查PLC的保护设置（如密码、写保护开关）。3. 将PLC置于STOP模式进行写入操作（如果是写程序）。" },
                new ErrorCodeInfo { Code = "0x0401", Description = "指定的设备代码不存在", Cause = "1. 读写了一个PLC内存中不存在的软元件地址（如地址超出范围）。2. 地址格式错误（如 M99999，但最大只有 M8191）。3. 指定了不存在的特殊继电器/寄存器。", Solution = "1. 核对PLC型号，确认软元件的最大范围。2. 仔细检查请求中的地址拼写和格式（如 D100, M10, X10）。3. 确认目标软元件在当前PLC配置中确实存在。" },
                new ErrorCodeInfo { Code = "0x0402", Description = "设备已被使用", Cause = "1. 在批量读写或块读写中，指定了重叠的地址范围。2. 某些特殊功能模块占用了通讯使用的软元件，发生冲突。", Solution = "1. 检查批量操作的地址范围，确保没有重叠。2. 查阅PLC和通讯模块的手册，确认其占用的软元件范围，避免冲突。" },
                new ErrorCodeInfo { Code = "0x0403", Description = "指定的设备不允许", Cause = "1. 尝试访问了当前CPU或模块不支持的软元件类型或范围。", Solution = "1. 查阅CPU和通讯模块的手册，确认支持的软元件类型和访问范围。" },
                new ErrorCodeInfo { Code = "0x0404", Description = "指定的设备无法访问", Cause = "1. 尝试访问了当前状态下不可访问的软元件（如某些只在特定模式下可用）。", Solution = "1. 检查PLC的运行模式（RUN/STOP）以及目标软元件的访问条件。" },
                new ErrorCodeInfo { Code = "0x0501", Description = "CPU单元中未安装CPU模块", Cause = "CPU单元内未安装CPU模块，或CPU模块故障", Solution = "1. 确认CPU模块已正确安装并牢固连接。2. 检查CPU模块的状态指示灯。" },
                new ErrorCodeInfo { Code = "0x0502", Description = "CPU单元处于STOP状态", Cause = "CPU单元处于STOP状态", Solution = "1. 将CPU单元切换到RUN状态。" },
                new ErrorCodeInfo { Code = "0x0503", Description = "CPU单元处于TEST模式", Cause = "CPU单元处于TEST模式", Solution = "1. 将CPU单元切换到RUN模式。" },
                new ErrorCodeInfo { Code = "0x0504", Description = "CPU单元处于PROGRAM模式", Cause = "CPU单元处于PROGRAM模式", Solution = "1. 将CPU单元切换到RUN模式。" },
                new ErrorCodeInfo { Code = "0xC000", Description = "指令错误", Cause = "1. 发送了PLC无法识别或不支持的MC协议命令。2. 协议版本不匹配。", Solution = "1. 核对发送的命令格式是否符合MC协议规范。2. 确认PLC和通讯模块支持的协议版本。" },
                new ErrorCodeInfo { Code = "0xC001", Description = "参数错误", Cause = "1. 发送的命令参数格式错误或值超出范围。2. 软元件地址格式错误。", Solution = "1. 仔细检查命令报文中的参数部分。2. 确认地址格式（如 D*, M% 等）是否正确。" },
                new ErrorCodeInfo { Code = "0xC002", Description = "数据长度错误", Cause = "1. 请求读写的数据长度错误（如超过单次允许的最大长度）。2. 请求跨越了软元件边界（如从D0读取3个字节）。3. 发送的数据长度与命令中声明的长度不符。", Solution = "1. 核对MC协议手册中单次读写操作的最大数据长度限制。2. 确保请求的地址和长度不会跨越数据类型边界（如16位字、32位双字）。3. 检查发送报文的长度字段。" },
                new ErrorCodeInfo { Code = "0xC003", Description = "数据内容错误", Cause = "1. 发送的数据内容包含无效值。", Solution = "1. 检查发送的数据内容是否在有效范围内。" },
                new ErrorCodeInfo { Code = "0xC004", Description = "通讯缓冲区错误", Cause = "1. 通讯缓冲区溢出。 2. 通讯处理繁忙。", Solution = "1. 降低通讯频率。2. 检查PLC程序是否过于复杂，导致通讯处理延时。" },
                new ErrorCodeInfo { Code = "0xC005", Description = "CPU监视定时器错误", Cause = "1. CPU监视定时器超时，导致程序停止，进而影响通讯。", Solution = "1. 检查并优化PLC程序，减少CPU负载。2. 必要时延长CPU监视定时器的时间。" },
                new ErrorCodeInfo { Code = "0xC006", Description = "批量写入时设备组合错误", Cause = "1. 在批量写入操作中，指定了不兼容或不允许组合在一起的软元件类型。", Solution = "1. 查阅MC协议手册，确认哪些软元件类型可以组合批量写入。" },
                new ErrorCodeInfo { Code = "0xC007", Description = "禁止写入", Cause = "1. PLC处于RUN模式，禁止写入程序或某些受保护区域。2. 受密码保护。", Solution = "1. 将PLC置于STOP模式进行写入（如果是程序）。2. 检查并解除密码保护。" },
                new ErrorCodeInfo { Code = "0xC008", Description = "程序存储器容量不足", Cause = "1. 尝试下载的程序过大，超出了存储器容量。", Solution = "1. 删除不必要的程序段或扩展存储器。" },
                new ErrorCodeInfo { Code = "0xC009", Description = "程序错误", Cause = "1. PLC程序中存在语法错误或逻辑错误。", Solution = "1. 检查并修正PLC程序。" },
                new ErrorCodeInfo { Code = "0xC00A", Description = "模块错误", Cause = "1. 扩展模块故障或未正确安装。", Solution = "1. 检查扩展模块的安装和状态指示灯。" },
                new ErrorCodeInfo { Code = "0xC00B", Description = "机架错误", Cause = "1. 机架配置错误。", Solution = "1. 检查并修正PLC的机架配置。" },
                new ErrorCodeInfo { Code = "0xC00C", Description = "CPU模块错误", Cause = "1. CPU模块自身故障。", Solution = "1. 检查CPU模块状态，必要时更换。" },
                new ErrorCodeInfo { Code = "0xC00D", Description = "智能功能模块错误", Cause = "1. 智能功能模块故障或未正确配置。", Solution = "1. 检查智能功能模块的安装、配置和状态。" },
                new ErrorCodeInfo { Code = "0xC00E", Description = "编程错误", Cause = "1. 尝试了非法的编程操作。", Solution = "1. 检查编程操作的合法性。" },
                new ErrorCodeInfo { Code = "0xC00F", Description = "格式错误", Cause = "1. 通讯数据格式错误。", Solution = "1. 检查通讯协议格式设置。" },
                new ErrorCodeInfo { Code = "0xC010", Description = "串行通讯错误", Cause = "1. RS485/RS232线路连接错误或断线。2. 通讯参数（波特率、数据位、停止位、校验位）不匹配。", Solution = "1. 检查线路连接（A/B线是否接反，屏蔽层接地）。2. 确认上位机和PLC（或通讯模块）的串口参数设置完全一致。" },
                new ErrorCodeInfo { Code = "0xC011", Description = "超时错误", Cause = "1. 发送请求后，在规定时间内未收到PLC的响应。", Solution = "1. 检查网络/线路连接。2. 增加上位机的超时时间设置。3. 检查PLC是否繁忙。" },
                new ErrorCodeInfo { Code = "0xC012", Description = "响应错误", Cause = "1. 收到的响应数据格式错误或校验失败。", Solution = "1. 检查通讯线路是否有干扰。2. 核对协议格式和校验方式。" },
                new ErrorCodeInfo { Code = "0xC013", Description = "帧错误", Cause = "1. 接收到的数据帧格式错误（如帧头、帧尾、长度不匹配）。", Solution = "1. 检查通讯协议设置。2. 检查线路质量。" },
                new ErrorCodeInfo { Code = "0xC014", Description = "奇偶校验错误", Cause = "1. 串行通讯中数据的奇偶校验失败。", Solution = "1. 确认上位机和PLC（或通讯模块）的奇偶校验设置一致（None, Odd, Even）。" },
                new ErrorCodeInfo { Code = "0xC015", Description = "溢出错误", Cause = "1. 串行通讯接收缓冲区溢出。", Solution = "1. 降低数据传输速率。2. 增加接收缓冲区大小（如果上位机软件支持）。" },
                new ErrorCodeInfo { Code = "0xC016", Description = "帧格式错误", Cause = "1. 串行通讯中检测到帧格式错误（如停止位错误）。", Solution = "1. 检查上位机和PLC（或通讯模块）的停止位设置是否一致。" },
                new ErrorCodeInfo { Code = "0xC017", Description = "中断检测错误", Cause = "1. 串行通讯中检测到BREAK信号。", Solution = "1. 检查线路是否受到强干扰或连接错误。" },
                new ErrorCodeInfo { Code = "0xC018", Description = "缓冲区满错误", Cause = "1. 发送或接收缓冲区已满。", Solution = "1. 降低通讯频率。2. 检查程序处理速度。" },
                new ErrorCodeInfo { Code = "0xC019", Description = "存储器错误", Cause = "1. 通讯模块或CPU内部存储器错误。", Solution = "1. 重启PLC或通讯模块。2. 必要时联系技术支持。" },
                new ErrorCodeInfo { Code = "0xC01A", Description = "硬件错误", Cause = "1. 通讯模块或CPU硬件故障。", Solution = "1. 检查硬件状态指示灯。2. 尝试更换模块。" },
                new ErrorCodeInfo { Code = "0xC01B", Description = "软件错误", Cause = "1. 通讯模块或CPU内部软件错误。", Solution = "1. 重启设备。2. 必要时更新固件。" },
                new ErrorCodeInfo { Code = "0xC01C", Description = "初始化错误", Cause = "1. 通讯模块初始化失败。", Solution = "1. 检查模块配置。2. 重启设备。" },
                new ErrorCodeInfo { Code = "0xC01D", Description = "配置错误", Cause = "1. 通讯模块配置参数错误。", Solution = "1. 重新检查并配置通讯模块参数（IP、站号、协议等）。" },
                new ErrorCodeInfo { Code = "0xC01E", Description = "认证错误", Cause = "1. 通讯需要认证，但认证失败。", Solution = "1. 检查认证凭据（如果适用）。" },
                new ErrorCodeInfo { Code = "0xC01F", Description = "安全错误", Cause = "1. 通讯违反了安全策略。", Solution = "1. 检查PLC的安全设置。" },
                new ErrorCodeInfo { Code = "0xC020-0xC0FF", Description = "各种错误", Cause = "一系列特定于功能或模块的错误", Solution = "1. 查阅对应模块的详细手册。" },
                new ErrorCodeInfo { Code = "0xB503", Description = "指定的软元件不存在", Cause = "与 0x0401 基本相同，指定了不存在的软元件地址。", Solution = "1. 核对PLC型号和软元件范围。2. 检查地址拼写。" },
                new ErrorCodeInfo { Code = "0x1B00", Description = "网络模块未准备就绪", Cause = "1. 以太网通讯模块（如QJ71E71, FX3U-ENET）尚未完成启动。", Solution = "1. 等待模块启动完成（观察模块状态LED）。2. 检查模块电源和固件。" },
                new ErrorCodeInfo { Code = "0x1B01", Description = "网络模块未建立连接", Cause = "1. 上位机与PLC的以太网模块之间物理连接中断。2. IP地址、子网掩码、网关设置错误。3. 防火墙或路由器阻止了通讯。", Solution = "1. 检查网线、交换机、路由器连接。2. 确认上位机和PLC以太网模块的IP配置在同一网段。3. 检查并配置防火墙规则，开放相应端口。" },
                new ErrorCodeInfo { Code = "0x1B02", Description = "网络模块忙", Cause = "1. 网络模块处理请求过多，暂时无法响应。", Solution = "1. 降低通讯频率。" },
                new ErrorCodeInfo { Code = "0x1B03", Description = "网络模块响应超时", Cause = "1. 发送请求后，未在设定时间内收到PLC的响应。", Solution = "1. 检查网络延迟。2. 增加上位机的超时时间设置。3. 检查PLC程序是否繁忙。" },
                new ErrorCodeInfo { Code = "0x1B04", Description = "网络模块缓冲区溢出", Cause = "1. 发送数据速度过快，超过了网络模块的处理能力。", Solution = "1. 降低发送频率。" },
                new ErrorCodeInfo { Code = "0x1B05", Description = "网络模块参数错误", Cause = "1. 发送的请求报文格式错误，参数超出范围。", Solution = "1. 检查通讯程序，确保发送的命令和参数符合协议规范。" },
                new ErrorCodeInfo { Code = "0x1B06", Description = "网络模块内部错误", Cause = "1. 网络模块内部发生未知错误。", Solution = "1. 重启网络模块或CPU。2. 检查固件版本。" },
                new ErrorCodeInfo { Code = "0x1B07", Description = "网络模块未找到目标站点", Cause = "1. 请求的目标PLC站号、网络号、单元号不正确。", Solution = "1. 仔细核对上位机程序中设置的PLC网络号（Network Number）、PC号（PC Number / Station Number）、单元号（Unit Number）与PLC硬件（或GX Works2/3中的网络配置）的设置是否完全一致。" },
                new ErrorCodeInfo { Code = "0x1B08", Description = "网络模块未找到目标单元", Cause = "1. 目标单元号（Unit Number）错误。", Solution = "1. 检查上位机程序和PLC侧的单元号设置。" },
                new ErrorCodeInfo { Code = "0x1B09", Description = "网络模块帧格式错误", Cause = "1. 接收到的数据帧格式不符合协议。", Solution = "1. 检查通讯协议设置（如帧头、帧尾、校验位等）。" },
                new ErrorCodeInfo { Code = "0x1B0A", Description = "网络模块序列号错误", Cause = "1. 用于检测重复或乱序的帧。", Solution = "1. 通常是瞬时问题，重试即可。" },
                new ErrorCodeInfo { Code = "0x1B0B", Description = "网络模块正在切换模式", Cause = "1. 网络模块在RUN/STOP模式切换中。", Solution = "1. 等待PLC状态稳定后重试。" },
                new ErrorCodeInfo { Code = "0x1B0C", Description = "网络模块访问路径错误", Cause = "1. 通过多级网络访问时，中间节点配置错误。", Solution = "1. 检查整个网络路径上的所有设备配置。" },
                new ErrorCodeInfo { Code = "0x1B0D", Description = "网络模块访问权限错误", Cause = "1. 未授权访问某些资源。", Solution = "1. 检查PLC的安全设置。" },
                new ErrorCodeInfo { Code = "0x1B0E", Description = "网络模块资源不足", Cause = "1. 网络模块可用资源（如连接数）耗尽。", Solution = "1. 关闭不必要的连接，或升级硬件。" },
                new ErrorCodeInfo { Code = "0x1B0F", Description = "网络模块不支持的功能", Cause = "1. 使用了网络模块不支持的命令或功能。", Solution = "1. 检查网络模块型号和固件，确认支持的功能列表。" },
                new ErrorCodeInfo { Code = "0x1B10", Description = "网络模块不支持的协议版本", Cause = "1. 通讯协议版本不匹配。", Solution = "1. 检查上位机程序和网络模块的协议版本设置。" },
                new ErrorCodeInfo { Code = "0x1B11", Description = "网络模块数据类型不匹配", Cause = "1. 请求的数据类型与目标地址不匹配。", Solution = "1. 检查读写命令中指定的数据类型（如按字节读取双字）。" },
                new ErrorCodeInfo { Code = "0x1B12", Description = "网络模块数据长度错误", Cause = "1. 请求读写的字节数不正确（如跨越了软元件边界）。", Solution = "1. 检查请求的起始地址和长度，确保不跨越数据类型边界。" },
                new ErrorCodeInfo { Code = "0x1B13", Description = "网络模块数据值错误", Cause = "1. 请求中包含了无效的数值。", Solution = "1. 检查写入的数据值范围。" },
                new ErrorCodeInfo { Code = "0x1B14-0x1BFF", Description = "各种网络模块错误", Cause = "一系列特定于网络模块的错误", Solution = "1. 查阅对应网络模块的详细手册。" },
                
               
            };
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Name = "ErrorCodeLookupForm";
            this.Text = "错误码查询";
            this.ResumeLayout(false);
        }
    }

    public class ErrorCodeInfo
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string Cause { get; set; }
        public string Solution { get; set; }
    }
}