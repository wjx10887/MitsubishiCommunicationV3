using System;
using System.IO.Ports;
using System.Windows.Forms;
using MelsecPLCCommunicator.Application.Interfaces;
using MelsecPLCCommunicator.Application.Services;
using MelsecPLCCommunicator.Domain.DTOs;

namespace MelsecPLCCommunicator.UI.Forms
{
    /// <summary>
    /// 连接配置窗体
    /// </summary>
    public partial class ConnectionConfigForm : Form
    {
        private readonly IPlcConnectionService _connectionService;
        private ComboBox cmbPortName;
        private readonly ILogService _logService;
        private TextBox txtPcIpAddress;
        private Label label19;
        private TextBox txtPcPort;
        private Label label20;

        /// <summary>
        /// 连接配置
        /// </summary>
        public ConnectionConfigDto ConnectionConfig { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionService">连接服务</param>
        /// <param name="logService">日志服务</param>
        public ConnectionConfigForm(IPlcConnectionService connectionService, ILogService logService)
        {
            InitializeComponent();
            _connectionService = connectionService;
            _logService = logService;
            InitializeUI();
            LoadLastConfig();
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        private void InitializeUI()
        {
            // 初始化PLC系列下拉框
            cmbPlcSeries.Items.AddRange(new string[] { "FX3", "FX5", "Q", "L", "IQ-R" });
            cmbPlcSeries.SelectedIndex = 2; // 默认选择Q系列

            // 初始化串口参数
            // 填充实际可用的串口列表
            cmbPortName.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            if (ports.Length > 0)
            {
                cmbPortName.Items.AddRange(ports);
                cmbPortName.SelectedIndex = 0;
            }
            else
            {
                cmbPortName.Items.Add("无可用串口");
                cmbPortName.SelectedIndex = 0;
                cmbPortName.Enabled = false;
            }

            cmbBaudRate.Items.AddRange(new string[] { "9600", "19200", "38400", "57600", "115200" });
            cmbBaudRate.SelectedIndex = 0;

            cmbParity.Items.AddRange(new string[] { "None", "Odd", "Even" });
            cmbParity.SelectedIndex = 0;

            cmbDataBits.Items.AddRange(new string[] { "7", "8" });
            cmbDataBits.SelectedIndex = 1;

            cmbStopBits.Items.AddRange(new string[] { "1", "2" });
            cmbStopBits.SelectedIndex = 0;

            // 初始化高级参数
            txtNetworkNumber.Text = "1";
            txtStationNumber.Text = "1";
            txtConnectionTimeout.Text = "3000";
            txtReceiveTimeout.Text = "3000";
            chkAutoReconnect.Checked = false;
            txtReconnectInterval.Text = "5000";

            // 绑定事件
            cmbInterfaceType.SelectedIndexChanged += ComboBoxInterfaceType_SelectedIndexChanged;
            cmbPlcSeries.SelectedIndexChanged += CmbPlcSeries_SelectedIndexChanged;
            cmbProtocolType.SelectedIndexChanged += UpdateConnectionName;
            btnTestConnection.Click += BtnTestConnection_Click;
            this.FormClosing += ConnectionConfigForm_FormClosing;

            // 初始化接口类型下拉框（根据PLC系列）
            UpdateInterfaceTypeComboBox();
            
            // 初始化协议类型下拉框
            UpdateProtocolComboBox();
            
            // 初始化时更新参数界面布局
            UpdateParameterUI();
            
            // 自动生成连接名称
            UpdateConnectionName(null, EventArgs.Empty);
        }

        /// <summary>
        /// 更新连接名称
        /// </summary>
        private void UpdateConnectionName(object sender, EventArgs e)
        {
            string plcSeries = cmbPlcSeries?.SelectedItem?.ToString() ?? "FX";
            string interfaceType = cmbInterfaceType?.SelectedItem?.ToString() ?? "以太网";
            string protocolType = cmbProtocolType?.SelectedItem?.ToString() ?? "MC Protocol (3E)";
            
            // 简化接口类型和协议类型的显示
            string simplifiedInterface = interfaceType;
            switch (interfaceType)
            {
                case "以太网":
                case "以太网 (模块)":
                    simplifiedInterface = "以太网";
                    break;
                case "串口":
                case "串口 (内置/模块)":
                case "串口 (模块)":
                    simplifiedInterface = "串口";
                    break;
            }
            
            string simplifiedProtocol = protocolType;
            switch (protocolType)
            {
                case "MC Protocol (3E) - TCP (二进制)":
                    simplifiedProtocol = "MC3E-TCP-Bin";
                    break;
                case "MC Protocol (3E) - TCP (ASCII)":
                    simplifiedProtocol = "MC3E-TCP-ASCII";
                    break;
                case "MC Protocol (3E) - UDP (二进制)":
                    simplifiedProtocol = "MC3E-UDP-Bin";
                    break;
                case "MC Protocol (3E) - UDP (ASCII)":
                    simplifiedProtocol = "MC3E-UDP-ASCII";
                    break;
                case "MC Protocol (4E) - TCP":
                    simplifiedProtocol = "MC4E-TCP";
                    break;
                case "MC Protocol (1E) - TCP (二进制)":
                    simplifiedProtocol = "MC1E-TCP-Bin";
                    break;
                case "MC Protocol (1E) - TCP (ASCII)":
                    simplifiedProtocol = "MC1E-TCP-ASCII";
                    break;
                case "MC Protocol (1C) - 格式1/4":
                    simplifiedProtocol = "MC1C";
                    break;
                case "MC Protocol (3C)":
                    simplifiedProtocol = "MC3C";
                    break;
                case "MC Protocol (4C)":
                    simplifiedProtocol = "MC4C";
                    break;
                case "SLMP":
                case "SLMP (兼容3E)":
                    simplifiedProtocol = "SLMP";
                    break;
                case "Modbus TCP":
                    simplifiedProtocol = "Modbus TCP";
                    break;
                case "Modbus RTU":
                    simplifiedProtocol = "Modbus RTU";
                    break;
                case "Raw Socket":
                    simplifiedProtocol = "Raw Socket";
                    break;
                case "Free Format":
                    simplifiedProtocol = "Free";
                    break;
            }
            
            // 生成连接名称
            txtConnectionName.Text = $"{plcSeries}-{simplifiedInterface}-{simplifiedProtocol}";
        }

        /// <summary>
        /// 物理接口类型选择变化事件
        /// </summary>
        private void ComboBoxInterfaceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateProtocolComboBox();
            UpdateParameterUI();
            UpdateConnectionName(sender, e);
        }

        /// <summary>
        /// PLC系列选择变化事件
        /// </summary>
        private void CmbPlcSeries_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Q系列PLC没有编程口通讯，需要更新接口类型选项
            UpdateInterfaceTypeComboBox();
            UpdateProtocolComboBox();
            UpdateParameterUI();
            UpdateConnectionName(sender, e);
        }

        /// <summary>
        /// 更新接口类型下拉框
        /// </summary>
        private void UpdateInterfaceTypeComboBox()
        {
            if (cmbPlcSeries.SelectedItem == null)
                return;

            string plcSeries = cmbPlcSeries.SelectedItem.ToString();

            // 保存当前选择的接口类型
            string currentInterface = cmbInterfaceType.SelectedItem?.ToString();

            // 清空接口类型选项
            cmbInterfaceType.Items.Clear();

            // 根据PLC系列添加适合的接口类型
            switch (plcSeries)
            {
                case "Q":
                    // Q系列PLC接口类型
                    cmbInterfaceType.Items.AddRange(new string[] { "以太网", "串口 (模块)" });
                    break;
                case "FX3":
                    // FX3系列PLC接口类型
                    cmbInterfaceType.Items.AddRange(new string[] { "以太网", "串口 (内置/模块)" });
                    break;
                case "FX5":
                    // FX5系列PLC接口类型
                    cmbInterfaceType.Items.AddRange(new string[] { "以太网", "串口 (内置/模块)" });
                    break;
                case "L":
                case "iQ-R":
                
                default:
                    // 其他系列PLC
                    cmbInterfaceType.Items.AddRange(new string[] { "以太网", "串口" });
                    break;
            }

            // 选择合适的接口类型
            if (!string.IsNullOrEmpty(currentInterface) && cmbInterfaceType.Items.Contains(currentInterface))
            {
                cmbInterfaceType.SelectedItem = currentInterface;
            }
            else
            {
                // 默认选择第一个选项
                if (cmbInterfaceType.Items.Count > 0)
                {
                    cmbInterfaceType.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// 更新协议类型下拉框
        /// </summary>
        private void UpdateProtocolComboBox()
        {
            if (cmbInterfaceType.SelectedItem == null || cmbPlcSeries.SelectedItem == null)
                return;

            string interfaceType = cmbInterfaceType.SelectedItem.ToString();
            string plcSeries = cmbPlcSeries.SelectedItem.ToString();

            cmbProtocolType.Items.Clear();

            // 根据PLC系列和接口类型添加适合的协议类型
            if (plcSeries == "Q")
            {
                switch (interfaceType)
                {
                    case "以太网":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (3E) - TCP (二进制)", "MC Protocol (3E) - TCP (ASCII)", "MC Protocol (3E) - UDP (二进制)", "MC Protocol (3E) - UDP (ASCII)", "MC Protocol (4E) - TCP", "SLMP", "Modbus TCP", "Raw Socket" });
                        break;
                    case "串口 (模块)":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (3C)", "MC Protocol (4C)", "Free Format", "Modbus RTU" });
                        break;
                    default:
                        cmbProtocolType.Items.Add("MC Protocol (3E) - TCP (二进制)");
                        break;
                }
            }
            else if (plcSeries == "FX3")
            {
                switch (interfaceType)
                {
                    case "以太网 (模块)":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (1E) - TCP (二进制)", "MC Protocol (1E) - TCP (ASCII)", "SLMP", "Modbus TCP", "Raw Socket" });
                        break;
                    case "串口 (内置/模块)":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (1C) - 格式1/4", "Free Format", "Modbus RTU" });
                        break;
                    default:
                        cmbProtocolType.Items.Add("MC Protocol (1E) - TCP (二进制)");
                        break;
                }
            }
            else if (plcSeries == "FX5")
            {
                switch (interfaceType)
                {
                    case "以太网 (内置/模块)":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (3E) - TCP (二进制)", "MC Protocol (3E) - TCP (ASCII)", "SLMP", "Modbus TCP", "Raw Socket" });
                        break;
                    case "串口 (内置/模块)":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (3C)", "MC Protocol (4C)", "Free Format", "Modbus RTU" });
                        break;
                    default:
                        cmbProtocolType.Items.Add("MC Protocol (3E) - TCP (二进制)");
                        break;
                }
            }
            else if (plcSeries == "L")
            {
                switch (interfaceType)
                {
                    case "以太网":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (3E) - TCP (二进制)", "MC Protocol (3E) - TCP (ASCII)", "MC Protocol (3E) - UDP (二进制)", "MC Protocol (3E) - UDP (ASCII)", "SLMP", "Modbus TCP" });
                        break;
                    case "串口":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (3C)", "MC Protocol (4C)", "Free Format", "Modbus RTU" });
                        break;
                    default:
                        cmbProtocolType.Items.Add("MC Protocol (3E) - TCP (二进制)");
                        break;
                }
            }
            else if (plcSeries == "IQ-R")
            {
                switch (interfaceType)
                {
                    case "以太网":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (3E) - TCP (二进制)", "MC Protocol (3E) - TCP (ASCII)", "SLMP (兼容3E)", "Modbus TCP" });
                        break;
                    case "串口":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (3C)", "Free Format", "Modbus RTU" });
                        break;
                    default:
                        cmbProtocolType.Items.Add("MC Protocol (3E) - TCP (二进制)");
                        break;
                }
            }
            else
            {
                // 其他系列PLC（如A系列）
                switch (interfaceType)
                {
                    case "以太网":
                    case "以太网 (模块)":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (1E) - TCP (二进制)", "MC Protocol (1E) - TCP (ASCII)", "Modbus TCP" });
                        break;
                    case "串口":
                    case "串口 (内置/模块)":
                    case "串口 (模块)":
                        cmbProtocolType.Items.AddRange(new string[] { "MC Protocol (1C) - 格式1/4", "Free Format", "Modbus RTU" });
                        break;
                    default:
                        cmbProtocolType.Items.Add("MC Protocol (1E) - TCP (二进制)");
                        break;
                }
            }

            if (cmbProtocolType.Items.Count > 0)
            {
                cmbProtocolType.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 更新参数配置界面
        /// </summary>
        private void UpdateParameterUI()
        {
            if (cmbInterfaceType.SelectedItem == null)
                return;

            string interfaceType = cmbInterfaceType.SelectedItem.ToString();
            bool isEthernet = interfaceType.Contains("以太网");
            bool isSerial = interfaceType.Contains("串口");

            // 显示/隐藏网络参数
            if (groupBoxNetwork != null)
                groupBoxNetwork.Visible = isEthernet;
            if (groupBoxSerial != null)
                groupBoxSerial.Visible = isSerial;

            // 显示高级参数
            if (groupBoxAdvanced != null)
                groupBoxAdvanced.Visible = true;

            // 调整控件位置
            if (isEthernet)
            {
                // 网络接口布局
                groupBoxNetwork.Location = new System.Drawing.Point(12, 168);
                groupBoxAdvanced.Location = new System.Drawing.Point(12, 254);
                btnTestConnection.Location = new System.Drawing.Point(12, 380);
                btnOK.Location = new System.Drawing.Point(192, 380);
                btnCancel.Location = new System.Drawing.Point(292, 380);
                this.Height = 460;
            }
            else if (isSerial)
            {
                // 串口接口布局
                groupBoxSerial.Location = new System.Drawing.Point(12, 168);
                groupBoxAdvanced.Location = new System.Drawing.Point(12, 294);
                btnTestConnection.Location = new System.Drawing.Point(12, 420);
                btnOK.Location = new System.Drawing.Point(192, 420);
                btnCancel.Location = new System.Drawing.Point(292, 420);
                this.Height = 500;
            }
        }

        /// <summary>
        /// 测试连接按钮点击事件
        /// </summary>
        private async void BtnTestConnection_Click(object sender, EventArgs e)
        {
            try
            {
                var config = GetConnectionConfig();
                var result = await _connectionService.TestConnectionAsync(config);

                if (result.Success)
                {
                    MessageBox.Show(result.Data ? "连接测试成功！" : "连接测试失败！", "测试结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"测试失败: {result.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// OK按钮点击事件
        /// </summary>
        private void BtnOK_Click(object sender, EventArgs e)
        {
            try
            {
                ConnectionConfig = GetConnectionConfig();
                DialogResult = DialogResult.OK;
                // 对于模态窗口，设置DialogResult会自动关闭窗口，不需要再调用Close()方法
            }
            catch (Exception ex)
            {
                MessageBox.Show($"配置错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Cancel按钮点击事件
        /// </summary>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            // 对于模态窗口，设置DialogResult会自动关闭窗口，不需要再调用Close()方法
        }

        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        private void ConnectionConfigForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 保存当前配置
            if (DialogResult == DialogResult.OK && ConnectionConfig != null)
            {
                SaveConfig(ConnectionConfig);
            }
            // 这里可以添加一些清理代码，比如释放资源等
            // 不需要额外的关闭逻辑，因为DialogResult已经设置
        }

        /// <summary>
        /// 加载上次的配置信息
        /// </summary>
        private void LoadLastConfig()
        {
            try
            {
                var config = LoadConfig();
                if (config != null)
                {
                    // 加载基本配置
                    if (!string.IsNullOrEmpty(config.ConnectionName))
                        txtConnectionName.Text = config.ConnectionName;
                    
                    if (!string.IsNullOrEmpty(config.PlcSeries) && cmbPlcSeries.Items.Contains(config.PlcSeries))
                        cmbPlcSeries.SelectedItem = config.PlcSeries;
                    
                    // 更新接口类型和协议类型
                    UpdateInterfaceTypeComboBox();
                    
                    if (!string.IsNullOrEmpty(config.InterfaceType) && cmbInterfaceType.Items.Contains(config.InterfaceType))
                        cmbInterfaceType.SelectedItem = config.InterfaceType;
                    
                    UpdateProtocolComboBox();
                    
                    if (!string.IsNullOrEmpty(config.ProtocolType) && cmbProtocolType.Items.Contains(config.ProtocolType))
                        cmbProtocolType.SelectedItem = config.ProtocolType;
                    
                    // 加载网络参数
                    if (config.InterfaceType.Contains("以太网"))
                    {
                        if (!string.IsNullOrEmpty(config.IpAddress))
                            txtIpAddress.Text = config.IpAddress;
                        
                        if (config.Port > 0)
                            txtPort.Text = config.Port.ToString();
                        
                        // 加载本地IP和端口
                        if (!string.IsNullOrEmpty(config.LocalIpAddress))
                            txtPcIpAddress.Text = config.LocalIpAddress;
                        
                        if (config.LocalPort > 0)
                            txtPcPort.Text = config.LocalPort.ToString();
                    }
                    // 加载串口参数
                    else if (config.InterfaceType.Contains("串口"))
                    {
                        if (!string.IsNullOrEmpty(config.PortName) && cmbPortName.Items.Contains(config.PortName))
                            cmbPortName.SelectedItem = config.PortName;
                        
                        if (config.BaudRate > 0 && cmbBaudRate.Items.Contains(config.BaudRate.ToString()))
                            cmbBaudRate.SelectedItem = config.BaudRate.ToString();
                        
                        if (!string.IsNullOrEmpty(config.Parity) && cmbParity.Items.Contains(config.Parity))
                            cmbParity.SelectedItem = config.Parity;
                        
                        if (config.DataBits > 0 && cmbDataBits.Items.Contains(config.DataBits.ToString()))
                            cmbDataBits.SelectedItem = config.DataBits.ToString();
                        
                        if (config.StopBits > 0 && cmbStopBits.Items.Contains(config.StopBits.ToString()))
                            cmbStopBits.SelectedItem = config.StopBits.ToString();
                    }
                    
                    // 加载高级参数
                    txtNetworkNumber.Text = config.NetworkNumber.ToString();
                    txtStationNumber.Text = config.StationNumber.ToString();
                    txtConnectionTimeout.Text = config.ConnectionTimeout.ToString();
                    txtReceiveTimeout.Text = config.ReceiveTimeout.ToString();
                    chkAutoReconnect.Checked = config.AutoReconnect;
                    txtReconnectInterval.Text = config.ReconnectInterval.ToString();
                    
                    // 更新连接名称
                    UpdateConnectionName(null, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                _logService?.Error("加载配置异常", ex);
            }
        }

        /// <summary>
        /// 保存配置到本地文件
        /// </summary>
        /// <param name="config">连接配置</param>
        private void SaveConfig(ConnectionConfigDto config)
        {
            try
            {
                string configPath = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "last_connection_config.json");
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(config);
                System.IO.File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                _logService?.Error("保存配置异常", ex);
            }
        }

        /// <summary>
        /// 从本地文件加载配置
        /// </summary>
        /// <returns>连接配置</returns>
        private ConnectionConfigDto LoadConfig()
        {
            try
            {
                string configPath = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "last_connection_config.json");
                if (System.IO.File.Exists(configPath))
                {
                    string json = System.IO.File.ReadAllText(configPath);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<ConnectionConfigDto>(json);
                }
            }
            catch (Exception ex)
            {
                _logService?.Error("加载配置异常", ex);
            }
            return null;
        }

        /// <summary>
        /// 获取连接配置
        /// </summary>
        /// <returns>连接配置</returns>
        private ConnectionConfigDto GetConnectionConfig()
        {
            try
            {
                var config = new ConnectionConfigDto
                {
                    ConnectionName = txtConnectionName?.Text ?? "PLC连接",
                    PlcSeries = cmbPlcSeries?.SelectedItem?.ToString() ?? "FX3",
                    InterfaceType = cmbInterfaceType?.SelectedItem?.ToString() ?? "以太网",
                    ProtocolType = cmbProtocolType?.SelectedItem?.ToString() ?? "MC Protocol (3E) - TCP (二进制)",
                    PlcModel = "", // 移除了txtPlcModel控件
                    NetworkNumber = 0,
                    StationNumber = 0,
                    ConnectionTimeout = 3000,
                    ReceiveTimeout = 3000,
                    AutoReconnect = false,
                    ReconnectInterval = 5000
                };

                // 高级参数
                if (txtNetworkNumber != null && int.TryParse(txtNetworkNumber.Text, out int networkNumber))
                {
                    config.NetworkNumber = networkNumber;
                }
                if (txtStationNumber != null && int.TryParse(txtStationNumber.Text, out int stationNumber))
                {
                    config.StationNumber = stationNumber;
                }
                if (txtConnectionTimeout != null && int.TryParse(txtConnectionTimeout.Text, out int connectionTimeout))
                {
                    config.ConnectionTimeout = connectionTimeout;
                }
                if (txtReceiveTimeout != null && int.TryParse(txtReceiveTimeout.Text, out int receiveTimeout))
                {
                    config.ReceiveTimeout = receiveTimeout;
                }
                if (chkAutoReconnect != null)
                {
                    config.AutoReconnect = chkAutoReconnect.Checked;
                }
                if (txtReconnectInterval != null && int.TryParse(txtReconnectInterval.Text, out int reconnectInterval))
                {
                    config.ReconnectInterval = reconnectInterval;
                }

                // 网络参数
                if (config.InterfaceType.Contains("以太网"))
                {
                    config.IpAddress = txtIpAddress?.Text ?? "192.168.1.100";
                    if (txtPort != null && int.TryParse(txtPort.Text, out int port))
                    {
                        config.Port = port;
                    }
                    else
                    {
                        config.Port = 6000;
                    }
                    
                    // 本地IP和端口
                    config.LocalIpAddress = txtPcIpAddress?.Text ?? "192.168.1.100";
                    if (txtPcPort != null && int.TryParse(txtPcPort.Text, out int localPort))
                    {
                        config.LocalPort = localPort;
                    }
                    else
                    {
                        config.LocalPort = 3000;
                    }
                }
                // 串口参数
                else if (config.InterfaceType.Contains("串口"))
                {
                    config.PortName = cmbPortName?.SelectedItem?.ToString() ?? "COM1";
                    if (cmbBaudRate != null && cmbBaudRate.SelectedItem != null && int.TryParse(cmbBaudRate.SelectedItem.ToString(), out int baudRate))
                    {
                        config.BaudRate = baudRate;
                    }
                    else
                    {
                        config.BaudRate = 9600;
                    }
                    config.Parity = cmbParity?.SelectedItem?.ToString() ?? "None";
                    if (cmbDataBits != null && cmbDataBits.SelectedItem != null && int.TryParse(cmbDataBits.SelectedItem.ToString(), out int dataBits))
                    {
                        config.DataBits = dataBits;
                    }
                    else
                    {
                        config.DataBits = 8;
                    }
                    if (cmbStopBits != null && cmbStopBits.SelectedItem != null && int.TryParse(cmbStopBits.SelectedItem.ToString(), out int stopBits))
                    {
                        config.StopBits = stopBits;
                    }
                    else
                    {
                        config.StopBits = 1;
                    }
                }

                return config;
            }
            catch (Exception ex)
            {
                _logService?.Error("获取连接配置异常", ex);
                throw new Exception($"配置参数错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBoxBasic = new System.Windows.Forms.GroupBox();
            this.cmbProtocolType = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbInterfaceType = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbPlcSeries = new System.Windows.Forms.ComboBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtConnectionName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBoxNetwork = new System.Windows.Forms.GroupBox();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtIpAddress = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBoxSerial = new System.Windows.Forms.GroupBox();
            this.cmbStopBits = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.cmbDataBits = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.cmbParity = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cmbBaudRate = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.cmbPortName = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.groupBoxAdvanced = new System.Windows.Forms.GroupBox();
            this.txtReconnectInterval = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.chkAutoReconnect = new System.Windows.Forms.CheckBox();
            this.label17 = new System.Windows.Forms.Label();
            this.txtReceiveTimeout = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.txtConnectionTimeout = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.txtStationNumber = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.txtNetworkNumber = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBoxBasic.SuspendLayout();
            this.groupBoxNetwork.SuspendLayout();
            this.groupBoxSerial.SuspendLayout();
            this.groupBoxAdvanced.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxBasic
            // 
            this.groupBoxBasic.Controls.Add(this.cmbProtocolType);
            this.groupBoxBasic.Controls.Add(this.label3);
            this.groupBoxBasic.Controls.Add(this.cmbInterfaceType);
            this.groupBoxBasic.Controls.Add(this.label2);
            this.groupBoxBasic.Controls.Add(this.cmbPlcSeries);
            this.groupBoxBasic.Controls.Add(this.label12);
            this.groupBoxBasic.Controls.Add(this.txtConnectionName);
            this.groupBoxBasic.Controls.Add(this.label1);
            this.groupBoxBasic.Location = new System.Drawing.Point(30, 30);
            this.groupBoxBasic.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.groupBoxBasic.Name = "groupBoxBasic";
            this.groupBoxBasic.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.groupBoxBasic.Size = new System.Drawing.Size(900, 375);
            this.groupBoxBasic.TabIndex = 0;
            this.groupBoxBasic.TabStop = false;
            this.groupBoxBasic.Text = "基本配置";
            // 
            // cmbProtocolType
            // 
            this.cmbProtocolType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProtocolType.Location = new System.Drawing.Point(275, 218);
            this.cmbProtocolType.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.cmbProtocolType.Name = "cmbProtocolType";
            this.cmbProtocolType.Size = new System.Drawing.Size(544, 38);
            this.cmbProtocolType.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(50, 225);
            this.label3.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(163, 30);
            this.label3.TabIndex = 4;
            this.label3.Text = "协议类型：";
            // 
            // cmbInterfaceType
            // 
            this.cmbInterfaceType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbInterfaceType.Location = new System.Drawing.Point(275, 142);
            this.cmbInterfaceType.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.cmbInterfaceType.Name = "cmbInterfaceType";
            this.cmbInterfaceType.Size = new System.Drawing.Size(544, 38);
            this.cmbInterfaceType.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(50, 150);
            this.label2.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(163, 30);
            this.label2.TabIndex = 2;
            this.label2.Text = "物理接口：";
            // 
            // cmbPlcSeries
            // 
            this.cmbPlcSeries.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPlcSeries.Location = new System.Drawing.Point(275, 68);
            this.cmbPlcSeries.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.cmbPlcSeries.Name = "cmbPlcSeries";
            this.cmbPlcSeries.Size = new System.Drawing.Size(544, 38);
            this.cmbPlcSeries.TabIndex = 9;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(50, 75);
            this.label12.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(148, 30);
            this.label12.TabIndex = 8;
            this.label12.Text = "PLC系列：";
            // 
            // txtConnectionName
            // 
            this.txtConnectionName.Location = new System.Drawing.Point(275, 292);
            this.txtConnectionName.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.txtConnectionName.Name = "txtConnectionName";
            this.txtConnectionName.Size = new System.Drawing.Size(544, 42);
            this.txtConnectionName.TabIndex = 1;
            this.txtConnectionName.Text = "PLC连接";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(50, 300);
            this.label1.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(163, 30);
            this.label1.TabIndex = 0;
            this.label1.Text = "连接名称：";
            // 
            // groupBoxNetwork
            // 
            this.txtPcIpAddress = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.txtPcPort = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.groupBoxNetwork.Controls.Add(this.txtPort);
            this.groupBoxNetwork.Controls.Add(this.label6);
            this.groupBoxNetwork.Controls.Add(this.txtIpAddress);
            this.groupBoxNetwork.Controls.Add(this.label5);
            this.groupBoxNetwork.Controls.Add(this.txtPcIpAddress);
            this.groupBoxNetwork.Controls.Add(this.label19);
            this.groupBoxNetwork.Controls.Add(this.txtPcPort);
            this.groupBoxNetwork.Controls.Add(this.label20);
            this.groupBoxNetwork.Location = new System.Drawing.Point(30, 420);
            this.groupBoxNetwork.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.groupBoxNetwork.Name = "groupBoxNetwork";
            this.groupBoxNetwork.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.groupBoxNetwork.Size = new System.Drawing.Size(900, 200);
            this.groupBoxNetwork.TabIndex = 1;
            this.groupBoxNetwork.TabStop = false;
            this.groupBoxNetwork.Text = "网络参数";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(775, 68);
            this.txtPort.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(69, 42);
            this.txtPort.TabIndex = 3;
            this.txtPort.Text = "6000";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(675, 75);
            this.label6.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(103, 30);
            this.label6.TabIndex = 2;
            this.label6.Text = "端口：";
            // 
            // txtIpAddress
            // 
            this.txtIpAddress.Location = new System.Drawing.Point(275, 68);
            this.txtIpAddress.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.txtIpAddress.Name = "txtIpAddress";
            this.txtIpAddress.Size = new System.Drawing.Size(369, 42);
            this.txtIpAddress.TabIndex = 1;
            this.txtIpAddress.Text = "192.168.1.100";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(50, 75);
            this.label5.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(133, 30);
            this.label5.TabIndex = 0;
            this.label5.Text = "IP地址：";
            // 
            // txtPcIpAddress
            // 
            this.txtPcIpAddress.Location = new System.Drawing.Point(275, 142);
            this.txtPcIpAddress.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.txtPcIpAddress.Name = "txtPcIpAddress";
            this.txtPcIpAddress.Size = new System.Drawing.Size(369, 42);
            this.txtPcIpAddress.TabIndex = 4;
            this.txtPcIpAddress.Text = "192.168.1.100";
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(50, 150);
            this.label19.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(163, 30);
            this.label19.TabIndex = 5;
            this.label19.Text = "本地IP：";
            // 
            // txtPcPort
            // 
            this.txtPcPort.Location = new System.Drawing.Point(775, 142);
            this.txtPcPort.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.txtPcPort.Name = "txtPcPort";
            this.txtPcPort.Size = new System.Drawing.Size(69, 42);
            this.txtPcPort.TabIndex = 6;
            this.txtPcPort.Text = "3000";
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(675, 150);
            this.label20.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(133, 30);
            this.label20.TabIndex = 7;
            this.label20.Text = "本地端口：";
            // 
            // groupBoxSerial
            // 
            this.groupBoxSerial.Controls.Add(this.cmbStopBits);
            this.groupBoxSerial.Controls.Add(this.label11);
            this.groupBoxSerial.Controls.Add(this.cmbDataBits);
            this.groupBoxSerial.Controls.Add(this.label10);
            this.groupBoxSerial.Controls.Add(this.cmbParity);
            this.groupBoxSerial.Controls.Add(this.label9);
            this.groupBoxSerial.Controls.Add(this.cmbBaudRate);
            this.groupBoxSerial.Controls.Add(this.label8);
            this.groupBoxSerial.Controls.Add(this.cmbPortName);
            this.groupBoxSerial.Controls.Add(this.label7);
            this.groupBoxSerial.Location = new System.Drawing.Point(30, 495);
            this.groupBoxSerial.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.groupBoxSerial.Name = "groupBoxSerial";
            this.groupBoxSerial.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.groupBoxSerial.Size = new System.Drawing.Size(900, 300);
            this.groupBoxSerial.TabIndex = 2;
            this.groupBoxSerial.TabStop = false;
            this.groupBoxSerial.Text = "串口参数";
            // 
            // cmbStopBits
            // 
            this.cmbStopBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbStopBits.Items.AddRange(new object[] {
            "1",
            "2"});
            this.cmbStopBits.Location = new System.Drawing.Point(650, 218);
            this.cmbStopBits.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.cmbStopBits.Name = "cmbStopBits";
            this.cmbStopBits.Size = new System.Drawing.Size(169, 38);
            this.cmbStopBits.TabIndex = 9;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(425, 225);
            this.label11.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(133, 30);
            this.label11.TabIndex = 8;
            this.label11.Text = "停止位：";
            // 
            // cmbDataBits
            // 
            this.cmbDataBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDataBits.Items.AddRange(new object[] {
            "7",
            "8"});
            this.cmbDataBits.Location = new System.Drawing.Point(275, 218);
            this.cmbDataBits.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.cmbDataBits.Name = "cmbDataBits";
            this.cmbDataBits.Size = new System.Drawing.Size(119, 38);
            this.cmbDataBits.TabIndex = 7;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(50, 225);
            this.label10.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(133, 30);
            this.label10.TabIndex = 6;
            this.label10.Text = "数据位：";
            // 
            // cmbParity
            // 
            this.cmbParity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbParity.Items.AddRange(new object[] {
            "None",
            "Odd",
            "Even"});
            this.cmbParity.Location = new System.Drawing.Point(650, 142);
            this.cmbParity.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.cmbParity.Name = "cmbParity";
            this.cmbParity.Size = new System.Drawing.Size(169, 38);
            this.cmbParity.TabIndex = 5;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(550, 150);
            this.label9.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(103, 30);
            this.label9.TabIndex = 4;
            this.label9.Text = "校验：";
            // 
            // cmbBaudRate
            // 
            this.cmbBaudRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBaudRate.Items.AddRange(new object[] {
            "9600",
            "19200",
            "38400",
            "57600",
            "115200"});
            this.cmbBaudRate.Location = new System.Drawing.Point(275, 142);
            this.cmbBaudRate.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.cmbBaudRate.Name = "cmbBaudRate";
            this.cmbBaudRate.Size = new System.Drawing.Size(244, 38);
            this.cmbBaudRate.TabIndex = 3;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(50, 150);
            this.label8.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(133, 30);
            this.label8.TabIndex = 2;
            this.label8.Text = "波特率：";
            // 
            // cmbPortName
            // 
            this.cmbPortName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPortName.Location = new System.Drawing.Point(275, 68);
            this.cmbPortName.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.cmbPortName.Name = "cmbPortName";
            this.cmbPortName.Size = new System.Drawing.Size(544, 38);
            this.cmbPortName.TabIndex = 1;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(50, 75);
            this.label7.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(163, 30);
            this.label7.TabIndex = 0;
            this.label7.Text = "串口名称：";
            // 
            // groupBoxAdvanced
            // 
            this.groupBoxAdvanced.Controls.Add(this.txtReconnectInterval);
            this.groupBoxAdvanced.Controls.Add(this.label18);
            this.groupBoxAdvanced.Controls.Add(this.chkAutoReconnect);
            this.groupBoxAdvanced.Controls.Add(this.label17);
            this.groupBoxAdvanced.Controls.Add(this.txtReceiveTimeout);
            this.groupBoxAdvanced.Controls.Add(this.label16);
            this.groupBoxAdvanced.Controls.Add(this.txtConnectionTimeout);
            this.groupBoxAdvanced.Controls.Add(this.label15);
            this.groupBoxAdvanced.Controls.Add(this.txtStationNumber);
            this.groupBoxAdvanced.Controls.Add(this.label14);
            this.groupBoxAdvanced.Controls.Add(this.txtNetworkNumber);
            this.groupBoxAdvanced.Controls.Add(this.label13);
            this.groupBoxAdvanced.Location = new System.Drawing.Point(30, 810);
            this.groupBoxAdvanced.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.groupBoxAdvanced.Name = "groupBoxAdvanced";
            this.groupBoxAdvanced.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.groupBoxAdvanced.Size = new System.Drawing.Size(900, 300);
            this.groupBoxAdvanced.TabIndex = 6;
            this.groupBoxAdvanced.TabStop = false;
            this.groupBoxAdvanced.Text = "高级参数";
            // 
            // txtReconnectInterval
            // 
            this.txtReconnectInterval.Location = new System.Drawing.Point(650, 218);
            this.txtReconnectInterval.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.txtReconnectInterval.Name = "txtReconnectInterval";
            this.txtReconnectInterval.Size = new System.Drawing.Size(119, 42);
            this.txtReconnectInterval.TabIndex = 11;
            this.txtReconnectInterval.Text = "5000";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(425, 225);
            this.label18.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(163, 30);
            this.label18.TabIndex = 10;
            this.label18.Text = "重连间隔：";
            // 
            // chkAutoReconnect
            // 
            this.chkAutoReconnect.AutoSize = true;
            this.chkAutoReconnect.Location = new System.Drawing.Point(275, 225);
            this.chkAutoReconnect.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.chkAutoReconnect.Name = "chkAutoReconnect";
            this.chkAutoReconnect.Size = new System.Drawing.Size(34, 33);
            this.chkAutoReconnect.TabIndex = 9;
            this.chkAutoReconnect.UseVisualStyleBackColor = true;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(50, 225);
            this.label17.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(163, 30);
            this.label17.TabIndex = 8;
            this.label17.Text = "自动重连：";
            // 
            // txtReceiveTimeout
            // 
            this.txtReceiveTimeout.Location = new System.Drawing.Point(650, 142);
            this.txtReceiveTimeout.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.txtReceiveTimeout.Name = "txtReceiveTimeout";
            this.txtReceiveTimeout.Size = new System.Drawing.Size(119, 42);
            this.txtReceiveTimeout.TabIndex = 7;
            this.txtReceiveTimeout.Text = "3000";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(425, 150);
            this.label16.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(163, 30);
            this.label16.TabIndex = 6;
            this.label16.Text = "接收超时：";
            // 
            // txtConnectionTimeout
            // 
            this.txtConnectionTimeout.Location = new System.Drawing.Point(275, 142);
            this.txtConnectionTimeout.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.txtConnectionTimeout.Name = "txtConnectionTimeout";
            this.txtConnectionTimeout.Size = new System.Drawing.Size(119, 42);
            this.txtConnectionTimeout.TabIndex = 5;
            this.txtConnectionTimeout.Text = "3000";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(50, 150);
            this.label15.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(163, 30);
            this.label15.TabIndex = 4;
            this.label15.Text = "连接超时：";
            // 
            // txtStationNumber
            // 
            this.txtStationNumber.Location = new System.Drawing.Point(650, 68);
            this.txtStationNumber.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.txtStationNumber.Name = "txtStationNumber";
            this.txtStationNumber.Size = new System.Drawing.Size(119, 42);
            this.txtStationNumber.TabIndex = 3;
            this.txtStationNumber.Text = "0";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(425, 75);
            this.label14.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(103, 30);
            this.label14.TabIndex = 2;
            this.label14.Text = "站号：";
            // 
            // txtNetworkNumber
            // 
            this.txtNetworkNumber.Location = new System.Drawing.Point(275, 68);
            this.txtNetworkNumber.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.txtNetworkNumber.Name = "txtNetworkNumber";
            this.txtNetworkNumber.Size = new System.Drawing.Size(119, 42);
            this.txtNetworkNumber.TabIndex = 1;
            this.txtNetworkNumber.Text = "0";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(50, 75);
            this.label13.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(133, 30);
            this.label13.TabIndex = 0;
            this.label13.Text = "网络号：";
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.Location = new System.Drawing.Point(30, 1125);
            this.btnTestConnection.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(250, 75);
            this.btnTestConnection.TabIndex = 3;
            this.btnTestConnection.Text = "测试连接";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(480, 1125);
            this.btnOK.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(200, 75);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(730, 1125);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(200, 75);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "关闭窗口";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // ConnectionConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(15F, 30F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(960, 1225);
            this.Controls.Add(this.groupBoxAdvanced);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnTestConnection);
            this.Controls.Add(this.groupBoxSerial);
            this.Controls.Add(this.groupBoxNetwork);
            this.Controls.Add(this.groupBoxBasic);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConnectionConfigForm";
            this.Text = "连接配置";
            this.groupBoxBasic.ResumeLayout(false);
            this.groupBoxBasic.PerformLayout();
            this.groupBoxNetwork.ResumeLayout(false);
            this.groupBoxNetwork.PerformLayout();
            this.groupBoxSerial.ResumeLayout(false);
            this.groupBoxSerial.PerformLayout();
            this.groupBoxAdvanced.ResumeLayout(false);
            this.groupBoxAdvanced.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.GroupBox groupBoxBasic;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtConnectionName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbInterfaceType;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmbProtocolType;
        private System.Windows.Forms.GroupBox groupBoxNetwork;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtIpAddress;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.GroupBox groupBoxSerial;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cmbBaudRate;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cmbParity;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cmbDataBits;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox cmbStopBits;
        private System.Windows.Forms.Button btnTestConnection;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ComboBox cmbPlcSeries;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.GroupBox groupBoxAdvanced;
        private System.Windows.Forms.TextBox txtNetworkNumber;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtStationNumber;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox txtConnectionTimeout;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox txtReceiveTimeout;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.CheckBox chkAutoReconnect;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox txtReconnectInterval;
        private System.Windows.Forms.Label label18;
    }
}
