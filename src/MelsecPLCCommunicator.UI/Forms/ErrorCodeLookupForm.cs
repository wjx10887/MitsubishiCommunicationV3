using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MelsecPLCCommunicator.UI.Forms
{
    public partial class ErrorCodeLookupForm : Form
    {
        private readonly List<ErrorCodeInfo> _errorCodes;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label labelErrorCode;
        private System.Windows.Forms.TextBox txtErrorCode;
        private System.Windows.Forms.Button btnLookup;
        private System.Windows.Forms.Label labelDescription;
        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabErrorCodeList;
        private System.Windows.Forms.DataGridView dgvErrorCodeList;
        private System.Windows.Forms.TabPage tabAlarmHistory;
        private System.Windows.Forms.DataGridView dgvAlarmHistory;
        private System.Windows.Forms.TabPage tabDiagnosticTools;
        private System.Windows.Forms.Panel panelDiagnostic;
        private System.Windows.Forms.Label labelDiagnosticTitle;
        private System.Windows.Forms.Label labelIpAddress;
        private System.Windows.Forms.TextBox txtIpAddress;
        private System.Windows.Forms.Button btnPing;
        private DataGridViewTextBoxColumn Time;
        private DataGridViewTextBoxColumn ErrorCode;
        private DataGridViewTextBoxColumn AlarmDescription;
        private DataGridViewTextBoxColumn AlarmSolution;
        private DataGridViewTextBoxColumn Code;
        private DataGridViewTextBoxColumn Description;
        private DataGridViewTextBoxColumn Cause;
        private DataGridViewTextBoxColumn Solution;
        private System.Windows.Forms.TextBox txtPingResult;

        public ErrorCodeLookupForm()
        {
            _errorCodes = GetErrorCodeList();
            InitializeComponent();
            InitializeData();
        }

        private void InitializeData()
        {
            // 填充错误码列表
            if (dgvErrorCodeList != null)
            {
                foreach (var errorCode in _errorCodes)
                {
                    dgvErrorCodeList.Rows.Add(errorCode.Code, errorCode.Description, errorCode.Cause, errorCode.Solution);
                }
            }

            // 填充报警历史（模拟数据）
            if (dgvAlarmHistory != null)
            {
                dgvAlarmHistory.Rows.Add(DateTime.Now.AddMinutes(-30), "01 00", "命令错误", "已解决");
                dgvAlarmHistory.Rows.Add(DateTime.Now.AddHours(-1), "02 00", "格式错误", "已解决");
                dgvAlarmHistory.Rows.Add(DateTime.Now.AddHours(-2), "03 00", "数据范围错误", "已解决");
                dgvAlarmHistory.Rows.Add(DateTime.Now.AddHours(-3), "0x0400", "保护：无法写入", "已解决");
                dgvAlarmHistory.Rows.Add(DateTime.Now.AddHours(-4), "0x0401", "指定的设备代码不存在", "已解决");
            }

            // 绑定事件
            if (btnLookup != null)
            {
                btnLookup.Click += BtnLookup_Click;
            }
            if (dgvErrorCodeList != null)
            {
                dgvErrorCodeList.CellClick += DgvErrorCodeList_CellClick;
            }
            if (dgvAlarmHistory != null)
            {
                dgvAlarmHistory.CellClick += DgvAlarmHistory_CellClick;
            }
            if (btnPing != null)
            {
                btnPing.Click += BtnPing_Click;
            }
        }

        private void BtnLookup_Click(object sender, EventArgs e)
        {
            if (txtErrorCode == null || txtDescription == null)
                return;

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
        }

        private void DgvErrorCodeList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvErrorCodeList != null && txtErrorCode != null && btnLookup != null)
            {
                var row = dgvErrorCodeList.Rows[e.RowIndex];
                txtErrorCode.Text = row.Cells["Code"].Value.ToString();
                btnLookup.PerformClick();
            }
        }

        private void DgvAlarmHistory_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvAlarmHistory != null && txtErrorCode != null && btnLookup != null)
            {
                var row = dgvAlarmHistory.Rows[e.RowIndex];
                txtErrorCode.Text = row.Cells["ErrorCode"].Value.ToString();
                btnLookup.PerformClick();
            }
        }

        private void BtnPing_Click(object sender, EventArgs e)
        {
            if (txtIpAddress == null || txtPingResult == null)
                return;

            var ipAddress = txtIpAddress.Text.Trim();
            if (string.IsNullOrEmpty(ipAddress))
            {
                txtPingResult.Text = "请输入PLC IP地址";
                return;
            }

            txtPingResult.Text = "正在执行Ping测试...\n";
            try
            {
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = ping.Send(ipAddress, 2000);
                    if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                    {
                        txtPingResult.Text += $"Ping成功！\n";
                        txtPingResult.Text += $"IP地址: {reply.Address}\n";
                        txtPingResult.Text += $"往返时间: {reply.RoundtripTime}ms\n";
                        txtPingResult.Text += $"TTL: {reply.Options.Ttl}\n";
                    }
                    else
                    {
                        txtPingResult.Text += $"Ping失败: {reply.Status}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                txtPingResult.Text += $"Ping测试异常: {ex.Message}\n";
            }
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
            this.Time = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ErrorCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AlarmDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.AlarmSolution = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panelTop = new System.Windows.Forms.Panel();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.labelDescription = new System.Windows.Forms.Label();
            this.btnLookup = new System.Windows.Forms.Button();
            this.txtErrorCode = new System.Windows.Forms.TextBox();
            this.labelErrorCode = new System.Windows.Forms.Label();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabErrorCodeList = new System.Windows.Forms.TabPage();
            this.dgvErrorCodeList = new System.Windows.Forms.DataGridView();
            this.Code = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Cause = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Solution = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabAlarmHistory = new System.Windows.Forms.TabPage();
            this.dgvAlarmHistory = new System.Windows.Forms.DataGridView();
            this.tabDiagnosticTools = new System.Windows.Forms.TabPage();
            this.panelDiagnostic = new System.Windows.Forms.Panel();
            this.txtPingResult = new System.Windows.Forms.TextBox();
            this.btnPing = new System.Windows.Forms.Button();
            this.txtIpAddress = new System.Windows.Forms.TextBox();
            this.labelIpAddress = new System.Windows.Forms.Label();
            this.labelDiagnosticTitle = new System.Windows.Forms.Label();
            this.panelTop.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabErrorCodeList.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvErrorCodeList)).BeginInit();
            this.tabAlarmHistory.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvAlarmHistory)).BeginInit();
            this.tabDiagnosticTools.SuspendLayout();
            this.panelDiagnostic.SuspendLayout();
            this.SuspendLayout();
            // 
            // Time
            // 
            this.Time.HeaderText = "时间";
            this.Time.MinimumWidth = 12;
            this.Time.Name = "Time";
            this.Time.ReadOnly = true;
            // 
            // ErrorCode
            // 
            this.ErrorCode.HeaderText = "错误码";
            this.ErrorCode.MinimumWidth = 12;
            this.ErrorCode.Name = "ErrorCode";
            this.ErrorCode.ReadOnly = true;
            // 
            // AlarmDescription
            // 
            this.AlarmDescription.HeaderText = "描述";
            this.AlarmDescription.MinimumWidth = 12;
            this.AlarmDescription.Name = "AlarmDescription";
            this.AlarmDescription.ReadOnly = true;
            // 
            // AlarmSolution
            // 
            this.AlarmSolution.HeaderText = "解决方法";
            this.AlarmSolution.MinimumWidth = 12;
            this.AlarmSolution.Name = "AlarmSolution";
            this.AlarmSolution.ReadOnly = true;
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.txtDescription);
            this.panelTop.Controls.Add(this.labelDescription);
            this.panelTop.Controls.Add(this.btnLookup);
            this.panelTop.Controls.Add(this.txtErrorCode);
            this.panelTop.Controls.Add(this.labelErrorCode);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(961, 143);
            this.panelTop.TabIndex = 0;
            // 
            // txtDescription
            // 
            this.txtDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDescription.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtDescription.Location = new System.Drawing.Point(196, 69);
            this.txtDescription.Multiline = true;
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.ReadOnly = true;
            this.txtDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDescription.Size = new System.Drawing.Size(753, 51);
            this.txtDescription.TabIndex = 4;
            // 
            // labelDescription
            // 
            this.labelDescription.AutoSize = true;
            this.labelDescription.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelDescription.Location = new System.Drawing.Point(30, 72);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(79, 20);
            this.labelDescription.TabIndex = 3;
            this.labelDescription.Text = "详细信息：";
            // 
            // btnLookup
            // 
            this.btnLookup.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnLookup.Location = new System.Drawing.Point(573, 12);
            this.btnLookup.Name = "btnLookup";
            this.btnLookup.Size = new System.Drawing.Size(110, 38);
            this.btnLookup.TabIndex = 2;
            this.btnLookup.Text = "查询";
            this.btnLookup.UseVisualStyleBackColor = true;
            // 
            // txtErrorCode
            // 
            this.txtErrorCode.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtErrorCode.Location = new System.Drawing.Point(196, 22);
            this.txtErrorCode.Name = "txtErrorCode";
            this.txtErrorCode.Size = new System.Drawing.Size(300, 26);
            this.txtErrorCode.TabIndex = 1;
            // 
            // labelErrorCode
            // 
            this.labelErrorCode.AutoSize = true;
            this.labelErrorCode.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelErrorCode.Location = new System.Drawing.Point(30, 30);
            this.labelErrorCode.Name = "labelErrorCode";
            this.labelErrorCode.Size = new System.Drawing.Size(65, 20);
            this.labelErrorCode.TabIndex = 0;
            this.labelErrorCode.Text = "错误码：";
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.tabControl);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBottom.Location = new System.Drawing.Point(0, 143);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(961, 426);
            this.panelBottom.TabIndex = 1;
            // 
            // tabControl
            // 
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabErrorCodeList);
            this.tabControl.Controls.Add(this.tabAlarmHistory);
            this.tabControl.Controls.Add(this.tabDiagnosticTools);
            this.tabControl.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabControl.Location = new System.Drawing.Point(3, 6);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(958, 420);
            this.tabControl.TabIndex = 0;
            // 
            // tabErrorCodeList
            // 
            this.tabErrorCodeList.Controls.Add(this.dgvErrorCodeList);
            this.tabErrorCodeList.Location = new System.Drawing.Point(4, 29);
            this.tabErrorCodeList.Name = "tabErrorCodeList";
            this.tabErrorCodeList.Padding = new System.Windows.Forms.Padding(3);
            this.tabErrorCodeList.Size = new System.Drawing.Size(950, 387);
            this.tabErrorCodeList.TabIndex = 0;
            this.tabErrorCodeList.Text = "错误码列表";
            this.tabErrorCodeList.UseVisualStyleBackColor = true;
            // 
            // dgvErrorCodeList
            // 
            this.dgvErrorCodeList.AllowUserToAddRows = false;
            this.dgvErrorCodeList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvErrorCodeList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvErrorCodeList.ColumnHeadersHeight = 30;
            this.dgvErrorCodeList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Code,
            this.Description,
            this.Cause,
            this.Solution});
            this.dgvErrorCodeList.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvErrorCodeList.Location = new System.Drawing.Point(6, 6);
            this.dgvErrorCodeList.Name = "dgvErrorCodeList";
            this.dgvErrorCodeList.ReadOnly = true;
            this.dgvErrorCodeList.RowHeadersVisible = false;
            this.dgvErrorCodeList.RowHeadersWidth = 102;
            this.dgvErrorCodeList.RowTemplate.Height = 60;
            this.dgvErrorCodeList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvErrorCodeList.Size = new System.Drawing.Size(941, 378);
            this.dgvErrorCodeList.TabIndex = 0;
            // 
            // Code
            // 
            this.Code.HeaderText = "错误码";
            this.Code.MinimumWidth = 12;
            this.Code.Name = "Code";
            this.Code.ReadOnly = true;
            // 
            // Description
            // 
            this.Description.HeaderText = "描述";
            this.Description.MinimumWidth = 12;
            this.Description.Name = "Description";
            this.Description.ReadOnly = true;
            // 
            // Cause
            // 
            this.Cause.HeaderText = "原因";
            this.Cause.MinimumWidth = 12;
            this.Cause.Name = "Cause";
            this.Cause.ReadOnly = true;
            // 
            // Solution
            // 
            this.Solution.HeaderText = "解决方法";
            this.Solution.MinimumWidth = 12;
            this.Solution.Name = "Solution";
            this.Solution.ReadOnly = true;
            // 
            // tabAlarmHistory
            // 
            this.tabAlarmHistory.Controls.Add(this.dgvAlarmHistory);
            this.tabAlarmHistory.Location = new System.Drawing.Point(4, 29);
            this.tabAlarmHistory.Name = "tabAlarmHistory";
            this.tabAlarmHistory.Padding = new System.Windows.Forms.Padding(3);
            this.tabAlarmHistory.Size = new System.Drawing.Size(950, 387);
            this.tabAlarmHistory.TabIndex = 1;
            this.tabAlarmHistory.Text = "报警历史";
            this.tabAlarmHistory.UseVisualStyleBackColor = true;
            // 
            // dgvAlarmHistory
            // 
            this.dgvAlarmHistory.AllowUserToAddRows = false;
            this.dgvAlarmHistory.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvAlarmHistory.ColumnHeadersHeight = 30;
            this.dgvAlarmHistory.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Time,
            this.ErrorCode,
            this.AlarmDescription,
            this.AlarmSolution});
            this.dgvAlarmHistory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvAlarmHistory.Font = new System.Drawing.Font("微软雅黑", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvAlarmHistory.GridColor = System.Drawing.SystemColors.ButtonShadow;
            this.dgvAlarmHistory.Location = new System.Drawing.Point(3, 3);
            this.dgvAlarmHistory.Name = "dgvAlarmHistory";
            this.dgvAlarmHistory.ReadOnly = true;
            this.dgvAlarmHistory.RowHeadersVisible = false;
            this.dgvAlarmHistory.RowHeadersWidth = 60;
            this.dgvAlarmHistory.RowTemplate.Height = 60;
            this.dgvAlarmHistory.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvAlarmHistory.Size = new System.Drawing.Size(944, 381);
            this.dgvAlarmHistory.TabIndex = 0;
            // 
            // tabDiagnosticTools
            // 
            this.tabDiagnosticTools.Controls.Add(this.panelDiagnostic);
            this.tabDiagnosticTools.Location = new System.Drawing.Point(4, 29);
            this.tabDiagnosticTools.Name = "tabDiagnosticTools";
            this.tabDiagnosticTools.Padding = new System.Windows.Forms.Padding(3);
            this.tabDiagnosticTools.Size = new System.Drawing.Size(950, 387);
            this.tabDiagnosticTools.TabIndex = 2;
            this.tabDiagnosticTools.Text = "诊断工具";
            this.tabDiagnosticTools.UseVisualStyleBackColor = true;
            // 
            // panelDiagnostic
            // 
            this.panelDiagnostic.Controls.Add(this.txtPingResult);
            this.panelDiagnostic.Controls.Add(this.btnPing);
            this.panelDiagnostic.Controls.Add(this.txtIpAddress);
            this.panelDiagnostic.Controls.Add(this.labelIpAddress);
            this.panelDiagnostic.Controls.Add(this.labelDiagnosticTitle);
            this.panelDiagnostic.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelDiagnostic.Location = new System.Drawing.Point(3, 3);
            this.panelDiagnostic.Name = "panelDiagnostic";
            this.panelDiagnostic.Size = new System.Drawing.Size(944, 381);
            this.panelDiagnostic.TabIndex = 0;
            // 
            // txtPingResult
            // 
            this.txtPingResult.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPingResult.Location = new System.Drawing.Point(5, 120);
            this.txtPingResult.Multiline = true;
            this.txtPingResult.Name = "txtPingResult";
            this.txtPingResult.ReadOnly = true;
            this.txtPingResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPingResult.Size = new System.Drawing.Size(1131, 316);
            this.txtPingResult.TabIndex = 4;
            // 
            // btnPing
            // 
            this.btnPing.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPing.Location = new System.Drawing.Point(442, 69);
            this.btnPing.Name = "btnPing";
            this.btnPing.Size = new System.Drawing.Size(120, 42);
            this.btnPing.TabIndex = 3;
            this.btnPing.Text = "Ping测试";
            this.btnPing.UseVisualStyleBackColor = true;
            // 
            // txtIpAddress
            // 
            this.txtIpAddress.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtIpAddress.Location = new System.Drawing.Point(186, 77);
            this.txtIpAddress.Name = "txtIpAddress";
            this.txtIpAddress.Size = new System.Drawing.Size(217, 26);
            this.txtIpAddress.TabIndex = 2;
            // 
            // labelIpAddress
            // 
            this.labelIpAddress.AutoSize = true;
            this.labelIpAddress.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelIpAddress.Location = new System.Drawing.Point(50, 80);
            this.labelIpAddress.Name = "labelIpAddress";
            this.labelIpAddress.Size = new System.Drawing.Size(93, 20);
            this.labelIpAddress.TabIndex = 1;
            this.labelIpAddress.Text = "PLC IP地址：";
            // 
            // labelDiagnosticTitle
            // 
            this.labelDiagnosticTitle.AutoSize = true;
            this.labelDiagnosticTitle.Font = new System.Drawing.Font("微软雅黑", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.labelDiagnosticTitle.Location = new System.Drawing.Point(30, 30);
            this.labelDiagnosticTitle.Name = "labelDiagnosticTitle";
            this.labelDiagnosticTitle.Size = new System.Drawing.Size(99, 19);
            this.labelDiagnosticTitle.TabIndex = 0;
            this.labelDiagnosticTitle.Text = "网络诊断工具";
            // 
            // ErrorCodeLookupForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(961, 569);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "ErrorCodeLookupForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "错误码查询与诊断";
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.tabErrorCodeList.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvErrorCodeList)).EndInit();
            this.tabAlarmHistory.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvAlarmHistory)).EndInit();
            this.tabDiagnosticTools.ResumeLayout(false);
            this.panelDiagnostic.ResumeLayout(false);
            this.panelDiagnostic.PerformLayout();
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