using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using MelsecPLCCommunicator.Application.Interfaces;
using MelsecPLCCommunicator.Application.Services;
using MelsecPLCCommunicator.Domain.DTOs;
using MelsecPLCCommunicator.Domain.Enums;
using MelsecPLCCommunicator.Domain.Shared;
using MelsecPLCCommunicator.UI.Forms;

namespace MelsecPLCCommunicator.UI
{
    /// <summary>
    /// 主窗体
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly IPlcConnectionService _connectionService;
        private readonly IPlcReadWriteService _readWriteService;
        private readonly ILogService _logService;
        private readonly ISettingsService _settingsService;
        private readonly IServiceProvider _serviceProvider;
        private ConnectionConfigDto _currentConfig;
        private System.Threading.Timer _monitorTimer;
        private DataGridViewComboBoxColumn columnDataType;
        private DataGridViewTextBoxColumn columnAddress;
        private DataGridViewComboBoxColumn columnOperationType;
        private DataGridViewTextBoxColumn columnValue;
        private DataGridViewTextBoxColumn columnResult;
        private ToolStripProgressBar toolStripProgressBar1;
        private int _monitorInterval = 1000; // 默认1秒

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connectionService">连接服务</param>
        /// <param name="readWriteService">读写服务</param>
        /// <param name="logService">日志服务</param>
        /// <param name="settingsService">设置服务</param>
        /// <param name="serviceProvider">服务提供者</param>
        public MainForm(IPlcConnectionService connectionService, IPlcReadWriteService readWriteService, ILogService logService, ISettingsService settingsService, IServiceProvider serviceProvider)
        {
            _connectionService = connectionService;
            _readWriteService = readWriteService;
            _logService = logService;
            _settingsService = settingsService;
            _serviceProvider = serviceProvider;
            InitializeComponent();
            InitializeUI();
            
            // 订阅日志服务的LogAdded事件
            if (_logService is LogService logServiceImpl)
            {
                logServiceImpl.LogAdded += LogService_LogAdded;
            }
        }

        /// <summary>
        /// 初始化UI
        /// </summary>
        private void InitializeUI()
        {
            // 初始化菜单栏
            InitializeMenu();

            // 初始化连接状态
            UpdateConnectionStatus();

            // 初始化日志窗口
            InitializeLogWindow();

            // 初始化批量操作控件
            InitializeBatchOperationUI();

            // 绑定事件
            //btnDisconnect.Click += BtnDisconnect_Click;
            //btnDeleteRow.Click += BtnDeleteRow_Click;
            // btnReadAll.Click += BtnReadAll_Click; // 已在InitializeComponent中绑定
            // btnWriteAll.Click += BtnWriteAll_Click; // 已在InitializeComponent中绑定
            checkBoxMonitor.CheckedChanged += CheckBoxMonitor_CheckedChanged;
            txtMonitorInterval.TextChanged += TxtMonitorInterval_TextChanged;
        
            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;
            
            // 订阅通讯帧事件
            _connectionService.FrameReceived += ConnectionService_FrameReceived;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // 设置窗口大小
            this.Size = new System.Drawing.Size(2550, 1500);
            // 初始化时可以添加其他必要的代码
        }

        /// <summary>
        /// 初始化菜单栏
        /// </summary>
        private void InitializeMenu()
        {
            // 创建文件菜单
            var fileMenu = new ToolStripMenuItem("文件");
            fileMenu.DropDownItems.Add("新建连接", null, NewConnection_Click);
            fileMenu.DropDownItems.Add("保存配置", null, SaveConfig_Click);
            fileMenu.DropDownItems.Add("加载配置", null, LoadConfig_Click);
            fileMenu.DropDownItems.Add("退出", null, Exit_Click);

            // 创建操作菜单
            var operationMenu = new ToolStripMenuItem("操作");
            //operationMenu.DropDownItems.Add("批量读写", null, BatchOperation_Click);
            operationMenu.DropDownItems.Add("连接监控", null, ConnectionMonitor_Click);
           // operationMenu.DropDownItems.Add("设备监控", null, DeviceMonitor_Click);

            // 创建工具菜单
            var toolMenu = new ToolStripMenuItem("工具");
            toolMenu.DropDownItems.Add("错误码查询", null, ErrorCodeLookup_Click);
            toolMenu.DropDownItems.Add("帧分析器", null, FrameAnalyzer_Click);

            // 创建帮助菜单
            var helpMenu = new ToolStripMenuItem("帮助");
            helpMenu.DropDownItems.Add("关于", null, About_Click);

            // 添加到菜单栏
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileMenu, operationMenu, toolMenu, helpMenu });
        }

        /// <summary>
        /// 初始化日志窗口
        /// </summary>
        private void InitializeLogWindow()
        {
            // 设置日志窗口位置和大小
            panelLog.Dock = DockStyle.Right;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.WordWrap = false; // 禁用自动换行，便于查看长日志

            // 添加日志按钮
            btnClearLog.Click += BtnClearLog_Click;
            btnSaveLog.Click += BtnSaveLog_Click;
            btnLoadLog.Click += BtnLoadLog_Click;
        }

        /// <summary>
        /// 更新连接状态
        /// </summary>
        private void UpdateConnectionStatus()
        {
            var status = _connectionService.GetConnectionStatus();
            if (status.Success)
            {
                lblConnectionStatus.Text = status.Data ? "已连接" : "未连接";
                lblConnectionStatus.ForeColor = status.Data ? System.Drawing.Color.Green : System.Drawing.Color.Red;
            }
            else
            {
                lblConnectionStatus.Text = "状态未知";
                lblConnectionStatus.ForeColor = System.Drawing.Color.Yellow;
            }
        }

        /// <summary>
        /// 连接按钮点击事件
        /// </summary>
        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            using (var configForm = new ConnectionConfigForm(_connectionService, _logService))
            {
                if (configForm.ShowDialog() == DialogResult.OK)
                {
                    _currentConfig = configForm.ConnectionConfig;
                    var result = await _connectionService.ConnectAsync(_currentConfig);

                    if (result.Success)
                    {
                        UpdateConnectionStatus();
                        AppendLog($"成功连接到 {_currentConfig.ConnectionName}", "INFO");
                        // 更新连接名称下拉框
                        UpdateConnectionNameComboBox();
                    }
                    else
                    {
                        MessageBox.Show($"连接失败: {result.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        AppendLog($"连接失败: {result.Error.Message}", "ERROR");
                    }
                }
            }
        }

        /// <summary>
        /// 断开连接按钮点击事件
        /// </summary>
        private async void BtnDisconnect_Click(object sender, EventArgs e)
        {
            var result = await _connectionService.DisconnectAsync();
            if (result.Success)
                    {
                        UpdateConnectionStatus();
                        AppendLog("已断开连接", "INFO");
                        // 清空当前配置
                        _currentConfig = null;
                        // 更新连接名称下拉框
                        UpdateConnectionNameComboBox();
                    }
            else
            {
                MessageBox.Show($"断开连接失败: {result.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog($"断开连接失败: {result.Error.Message}", "ERROR");
            }
        }

        /// <summary>
        /// 数据类型转换方法
        /// </summary>
        private DataType StringToDataType(string dataTypeStr)
        {
            try
            {
                switch (dataTypeStr?.ToUpper())
                {
                    // 位数据类型
                    case "X": return DataType.X; // 输入寄存器
                    case "Y": return DataType.Y; // 输出寄存器
                    case "M": return DataType.M; // 中间寄存器
                    case "B": return DataType.B; // 连接继电器
                    case "S": return DataType.S; // 状态寄存器
                    case "F": return DataType.F; // 报警器
                    case "T": return DataType.T; // 定时器
                    case "C": return DataType.C; // 计数器
                    
                    // 字数据类型
                    case "D": return DataType.D; // 数据寄存器
                    case "W": return DataType.W; // 链接寄存器
                    case "R": return DataType.R; // 文件寄存器
                    case "TN": return DataType.TN; // 定时器当前值
                    case "CN": return DataType.CN; // 计数器当前值
                    
                    // 双字数据类型
                    case "D32": return DataType.D32; // 32位整型
                    case "FLOAT": return DataType.Float; // 浮点数
                    case "DFLOAT": return DataType.DFloat; // 双精度浮点
                    
                    default: return DataType.M; // 默认返回M类型
                }
            }
            catch (Exception ex)
            {
                _logService.Error("数据类型转换异常", ex);
                return DataType.M; // 默认返回M类型
            }
        }

        /// <summary>
        /// 初始化批量操作UI
        /// </summary>
        private void InitializeBatchOperationUI()
        {
            // 初始化连接名称下拉框
            // 显示已经连接的连接名称
            cmbConnectionName.Items.Clear();
            if (_currentConfig != null && !string.IsNullOrEmpty(_currentConfig.ConnectionName))
            {
                cmbConnectionName.Items.Add(_currentConfig.ConnectionName);
                cmbConnectionName.SelectedIndex = 0;
            }
            else
            {
                // 如果没有连接，添加默认项
                cmbConnectionName.Items.Add("无活动连接");
                cmbConnectionName.SelectedIndex = 0;
                cmbConnectionName.Enabled = false;
            }

            // 初始化读写模式下拉框
            cmbReadWriteMode.Items.AddRange(new string[] { "离散模式", "连续模式" });
            cmbReadWriteMode.SelectedIndex = 0; // 默认离散模式
            cmbReadWriteMode.SelectedIndexChanged += CmbReadWriteMode_SelectedIndexChanged;

            // 初始化数据数量DomainUpDown
            domainUpDownDataCount.Items.Clear();
            for (int i = 1; i <= 10; i++)
            {
                domainUpDownDataCount.Items.Add(i.ToString());
            }
            domainUpDownDataCount.Text = "1"; // 默认1

            // 初始化数据类型下拉框
            cmbDataType.Items.AddRange(new string[] { "X", "Y", "M", "B", "S", "F", "TS", "CS", "TC", "CC", "D", "W", "R", "TN", "CN", "D32", "Float", "DFloat" });
            if (cmbDataType.Items.Count > 0)
            {
                cmbDataType.SelectedIndex = 0;
            }

            // 初始化操作类型下拉框
            cmbOperationType.Items.AddRange(new string[] { "读取", "写入" });
            if (cmbOperationType.Items.Count > 0)
            {
                cmbOperationType.SelectedIndex = 0;
            }

            // 初始化监控间隔
            try
            {
                string monitorInterval = System.Configuration.ConfigurationManager.AppSettings["DefaultMonitorInterval"];
                if (!string.IsNullOrEmpty(monitorInterval) && int.TryParse(monitorInterval, out _monitorInterval))
                {
                    txtMonitorInterval.Text = _monitorInterval.ToString();
                }
                else
                {
                    txtMonitorInterval.Text = "1000";
                }
            }
            catch (Exception ex)
            {
                _logService.Error("读取配置异常", ex);
                txtMonitorInterval.Text = "1000";
            }

            // 初始化数据表格
            dataGridViewBatch.AutoGenerateColumns = false;
            dataGridViewBatch.AllowUserToAddRows = false;
            dataGridViewBatch.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewBatch.MultiSelect = false;

            // 初始化数据类型列
            columnDataType.Items.AddRange(new string[] { "X", "Y", "M", "B", "S", "F", "T", "C", "D", "W", "R", "TN", "CN", "D32", "Float", "DFloat" });
            columnDataType.Width = 80;

            // 初始化操作类型列
            columnOperationType.Items.AddRange(new string[] { "读取", "写入" });
            columnOperationType.Width = 80;
            
            // 设置其他列的宽度
            columnAddress.Width = 100;
            columnValue.Width = 120;
            columnResult.Width = 120;

            // 绑定监控模式事件，用于控制读写按钮的可用性
           // checkBoxMonitor.CheckedChanged += CheckBoxMonitor_CheckedChanged;
        }

        /// <summary>
        /// 添加行按钮点击事件
        /// </summary>
        /// <summary>
        /// 读写模式下拉框选择变化事件
        /// </summary>
        private void CmbReadWriteMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            string readWriteMode = cmbReadWriteMode.SelectedItem?.ToString();
            if (readWriteMode == "离散模式")
            {
                // 离散模式下，数据数量默认1
                domainUpDownDataCount.Text = "1";
            }
        }

        private void BtnAddRow_Click(object sender, EventArgs e)
        {
            string dataType = cmbDataType.SelectedItem?.ToString();
            string address = txtAddress.Text;
            string operationType = cmbOperationType.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(dataType) || string.IsNullOrEmpty(address))
            {
                MessageBox.Show("请选择数据类型并输入地址", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 获取读写模式
            string readWriteMode = cmbReadWriteMode.SelectedItem?.ToString();
            if (readWriteMode == "离散模式")
            {
                // 检查是否已存在相同的行
                bool isDuplicate = false;
                foreach (DataGridViewRow row in dataGridViewBatch.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        string rowDataType = row.Cells["columnDataType"].Value?.ToString();
                        string rowAddress = row.Cells["columnAddress"].Value?.ToString();
                        string rowOperationType = row.Cells["columnOperationType"].Value?.ToString();
                        
                        if (rowDataType == dataType && rowAddress == address && rowOperationType == operationType)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }
                }
                
                if (!isDuplicate)
                {
                    // 离散模式：添加单行数据
                    dataGridViewBatch.Rows.Add(dataType, address, operationType, "", "");
                }
                else
                {
                    MessageBox.Show("该数据行已存在，请勿重复添加", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else if (readWriteMode == "连续模式")
            {
                // 连续模式：添加多行数据
                int dataCount = 1;
                if (int.TryParse(domainUpDownDataCount.Text, out int count))
                {
                    dataCount = count;
                }

                // 解析地址，获取基础地址和偏移量
                string baseAddress = address;
                int offset = 0;

                // 尝试从地址中提取数字部分
                try
                {
                    // 提取地址中的数字部分
                    string numericPart = new string(address.Where(char.IsDigit).ToArray());
                    if (!string.IsNullOrEmpty(numericPart) && int.TryParse(numericPart, out offset))
                    {
                        // 提取地址中的字母部分
                        string letterPart = new string(address.Where(char.IsLetter).ToArray());
                        baseAddress = letterPart;
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error("地址解析异常", ex);
                }

                // 根据数据类型确定地址偏移量
                int addressIncrement = 1; // 默认每次增加1
                if (dataType == "D32" || dataType == "Float")
                {
                    addressIncrement = 2; // D32和Float类型每次增加2
                }
                else if (dataType == "F64")
                {
                    addressIncrement = 4; // F64类型每次增加4
                }

                // 检查并添加多行数据
                List<string> newAddresses = new List<string>();
                for (int i = 0; i < dataCount; i++)
                {
                    int currentOffset = offset + (i * addressIncrement);
                    string currentAddress = $"{baseAddress}{currentOffset}";
                    newAddresses.Add(currentAddress);
                }
                
                // 检查是否有重复
                List<string> duplicateAddresses = new List<string>();
                foreach (string currentAddress in newAddresses)
                {
                    foreach (DataGridViewRow row in dataGridViewBatch.Rows)
                    {
                        if (!row.IsNewRow)
                        {
                            string rowDataType = row.Cells["columnDataType"].Value?.ToString();
                            string rowAddress = row.Cells["columnAddress"].Value?.ToString();
                            string rowOperationType = row.Cells["columnOperationType"].Value?.ToString();
                            
                            if (rowDataType == dataType && rowAddress == currentAddress && rowOperationType == operationType)
                            {
                                duplicateAddresses.Add(currentAddress);
                                break;
                            }
                        }
                    }
                }
                
                if (duplicateAddresses.Count > 0)
                {
                    string message = duplicateAddresses.Count == newAddresses.Count 
                        ? "所有数据行已存在，请勿重复添加" 
                        : $"部分数据行已存在 ({string.Join(", ", duplicateAddresses)})，将添加剩余行";
                    MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
                // 添加不重复的行
                foreach (string currentAddress in newAddresses)
                {
                    bool isDuplicate = false;
                    foreach (DataGridViewRow row in dataGridViewBatch.Rows)
                    {
                        if (!row.IsNewRow)
                        {
                            string rowDataType = row.Cells["columnDataType"].Value?.ToString();
                            string rowAddress = row.Cells["columnAddress"].Value?.ToString();
                            string rowOperationType = row.Cells["columnOperationType"].Value?.ToString();
                            
                            if (rowDataType == dataType && rowAddress == currentAddress && rowOperationType == operationType)
                            {
                                isDuplicate = true;
                                break;
                            }
                        }
                    }
                    
                    if (!isDuplicate)
                    {
                        dataGridViewBatch.Rows.Add(dataType, currentAddress, operationType, "", "");
                    }
                }
            }
        }

        /// <summary>
        /// 删除行按钮点击事件
        /// </summary>
        private void BtnDeleteRow_Click(object sender, EventArgs e)
        {
            // 删除选中行
            if (dataGridViewBatch.SelectedRows.Count > 0)
            {
                dataGridViewBatch.Rows.RemoveAt(dataGridViewBatch.SelectedRows[0].Index);
            }
        }

        /// <summary>
        /// 批量读取按钮点击事件
        /// </summary>
        private async void BtnReadAll_Click(object sender, EventArgs e)
        {
            try
            {
                _logService.Info("开始批量读取操作");
                
                // 检查是否有数据行
                if (dataGridViewBatch.Rows.Count == 0)
                {
                    MessageBox.Show("请先添加数据行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // 收集所有读取请求
                var readRequests = new List<BatchReadRequest>();
                var validRows = new List<DataGridViewRow>();
                int invalidCount = 0;
                
                foreach (DataGridViewRow row in dataGridViewBatch.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        string address = row.Cells["columnAddress"].Value?.ToString();
                        string dataType = row.Cells["columnDataType"].Value?.ToString();
                        if (!string.IsNullOrEmpty(address) && !string.IsNullOrEmpty(dataType))
                        {
                            try
                            {
                                var dataTypeEnum = StringToDataType(dataType);
                                readRequests.Add(new BatchReadRequest
                                {
                                    Address = address,
                                    DataType = dataTypeEnum,
                                    Length = 1
                                });
                                validRows.Add(row);
                                row.Cells["columnResult"].Value = "读取中...";
                            }
                            catch (Exception ex)
                            {
                                row.Cells["columnResult"].Value = $"错误: {ex.Message}";
                                _logService.Warning($"数据类型转换错误: {dataType}");
                                invalidCount++;
                            }
                        }
                        else
                        {
                            row.Cells["columnResult"].Value = "错误: 地址或数据类型为空";
                            _logService.Warning("地址或数据类型为空");
                            invalidCount++;
                        }
                    }
                }
                
                // 执行批量读取
                if (readRequests.Count > 0)
                {
                    _logService.Info($"执行批量读取，共 {readRequests.Count} 条数据");
                    
                    // 禁用按钮，防止重复操作
                    btnReadAll.Enabled = false;
                    btnWriteAll.Enabled = false;
                    
                    try
                    {
                        var result = await _readWriteService.BatchReadAsync(readRequests.ToArray());
                        if (result.Success)
                        {
                            for (int i = 0; i < result.Data.Length && i < validRows.Count; i++)
                            {
                                var row = validRows[i];
                                var value = result.Data[i];
                                // 格式化显示值
                                if (value is Array array)
                                {
                                    if (array.Length == 1)
                                    {
                                        // 单个值，显示第一个元素
                                        row.Cells["columnValue"].Value = array.GetValue(0);
                                    }
                                    else
                                    {
                                        // 多个值，显示为逗号分隔的字符串
                                        row.Cells["columnValue"].Value = string.Join(", ", array.Cast<object>());
                                    }
                                }
                                else
                                {
                                    // 非数组类型，直接显示
                                    row.Cells["columnValue"].Value = value;
                                }
                                row.Cells["columnResult"].Value = "成功";
                                _logService.Info($"读取成功: {readRequests[i].Address} = {value}");
                            }
                            
                            if (invalidCount > 0)
                            {
                                MessageBox.Show($"批量读取完成，成功 {readRequests.Count} 条，失败 {invalidCount} 条", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("批量读取完成，全部成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            foreach (var row in validRows)
                            {
                                row.Cells["columnResult"].Value = $"失败: {result.Error.Message}";
                            }
                            _logService.Error("批量读取失败", null, result.Error.Message);
                            MessageBox.Show($"批量读取失败: {result.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    finally
                    {
                        // 重新启用按钮
                        btnReadAll.Enabled = true;
                        btnWriteAll.Enabled = true;
                    }
                }
                else
                {
                    MessageBox.Show("没有有效的读取请求", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                
                _logService.Info("批量读取操作完成");
            }
            catch (Exception ex)
            {
                _logService.Error("批量读取操作异常", ex);
                MessageBox.Show($"批量读取操作异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // 确保按钮重新启用
                btnReadAll.Enabled = true;
                btnWriteAll.Enabled = true;
            }
        }

        /// <summary>
        /// 批量写入按钮点击事件
        /// </summary>
        private async void BtnWriteAll_Click(object sender, EventArgs e)
        {
            try
            {
                _logService.Info("开始批量写入操作");
                
                // 检查是否有数据行
                if (dataGridViewBatch.Rows.Count == 0)
                {
                    MessageBox.Show("请先添加数据行", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // 收集所有写入请求
                var writeRequests = new List<BatchWriteRequest>();
                var validRows = new List<DataGridViewRow>();
                int invalidCount = 0;
                
                // 先检查所有数据是否有效
                foreach (DataGridViewRow row in dataGridViewBatch.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        string address = row.Cells["columnAddress"].Value?.ToString();
                        string dataType = row.Cells["columnDataType"].Value?.ToString();
                        object value = row.Cells["columnValue"].Value;
                        if (!string.IsNullOrEmpty(address) && !string.IsNullOrEmpty(dataType) && value != null)
                        {
                            try
                            {
                                var dataTypeEnum = StringToDataType(dataType);
                                writeRequests.Add(new BatchWriteRequest
                                {
                                    Address = address,
                                    DataType = dataTypeEnum,
                                    Value = value
                                });
                                validRows.Add(row);
                                row.Cells["columnResult"].Value = "写入中...";
                            }
                            catch (Exception ex)
                            {
                                row.Cells["columnResult"].Value = $"错误: {ex.Message}";
                                _logService.Warning($"数据类型转换错误: {dataType}");
                                invalidCount++;
                            }
                        }
                        else
                        {
                            row.Cells["columnResult"].Value = "错误: 地址、数据类型或值为空";
                            _logService.Warning("地址、数据类型或值为空");
                            invalidCount++;
                        }
                    }
                }
                
                if (validRows.Count == 0)
                {
                    MessageBox.Show("没有有效的写入请求", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // 写入确认
                var dialogResult = MessageBox.Show($"确定要写入 {validRows.Count} 条数据吗？", "确认写入", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                if (dialogResult != DialogResult.OK)
                {
                    foreach (var row in validRows)
                    {
                        row.Cells["columnResult"].Value = "取消";
                    }
                    _logService.Info("用户取消批量写入");
                    return;
                }
                
                // 执行批量写入
                if (writeRequests.Count > 0)
                {
                    _logService.Info($"执行批量写入，共 {writeRequests.Count} 条数据");
                    
                    // 禁用按钮，防止重复操作
                    btnReadAll.Enabled = false;
                    btnWriteAll.Enabled = false;
                    
                    try
                    {
                        var result = await _readWriteService.BatchWriteAsync(writeRequests.ToArray());
                        if (result.Success)
                        {
                            foreach (var row in validRows)
                            {
                                row.Cells["columnResult"].Value = "成功";
                            }
                            _logService.Info("批量写入成功");
                            
                            if (invalidCount > 0)
                            {
                                MessageBox.Show($"批量写入完成，成功 {validRows.Count} 条，失败 {invalidCount} 条", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("批量写入完成，全部成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            foreach (var row in validRows)
                            {
                                row.Cells["columnResult"].Value = $"失败: {result.Error.Message}";
                            }
                            _logService.Error("批量写入失败", null, result.Error.Message);
                            MessageBox.Show($"批量写入失败: {result.Error.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    finally
                    {
                        // 重新启用按钮
                        btnReadAll.Enabled = true;
                        btnWriteAll.Enabled = true;
                    }
                }
                
                _logService.Info("批量写入操作完成");
            }
            catch (Exception ex)
            {
                _logService.Error("批量写入操作异常", ex);
                MessageBox.Show($"批量写入操作异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // 确保按钮重新启用
                btnReadAll.Enabled = true;
                btnWriteAll.Enabled = true;
            }
        }

        /// <summary>
        /// 监控模式复选框状态变化事件
        /// </summary>
        private void CheckBoxMonitor_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxMonitor.Checked)
            {
                // 读取间隔值
                if (!int.TryParse(txtMonitorInterval.Text, out _monitorInterval))
                {
                    _monitorInterval = 1000; // 默认1秒
                    txtMonitorInterval.Text = "1000";
                }
                
                // 禁用读写按钮
                btnReadAll.Enabled = false;
                btnWriteAll.Enabled = false;
                
                // 启动监控定时器
                _monitorTimer = new System.Threading.Timer(async (state) =>
                {
                    try
                    {
                        await Task.Run(() => ReadAllData());
                    }
                    catch (Exception ex)
                    {
                        _logService.Error("监控定时器异常", ex);
                    }
                }, null, 0, _monitorInterval);
            }
            else
            {
                // 启用读写按钮
                btnReadAll.Enabled = true;
                btnWriteAll.Enabled = true;
                
                // 停止监控定时器
                if (_monitorTimer != null)
                {
                    _monitorTimer.Dispose();
                    _monitorTimer = null;
                }
            }
        }

        /// <summary>
        /// 监控间隔文本框变化事件
        /// </summary>
        private void TxtMonitorInterval_TextChanged(object sender, EventArgs e)
        {
            // 如果监控模式已启用，重新启动定时器
            if (checkBoxMonitor.Checked)
            {
                CheckBoxMonitor_CheckedChanged(sender, e);
            }
        }

        /// <summary>
        /// 读取所有数据（用于监控模式）
        /// </summary>
        private async void ReadAllData()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ReadAllData));
                return;
            }

            _logService.Debug("监控模式读取数据");

            // 收集有效的读取请求
            var readRequests = new List<BatchReadRequest>();
            var validRows = new List<DataGridViewRow>();
            
            foreach (DataGridViewRow row in dataGridViewBatch.Rows)
            {
                if (!row.IsNewRow)
                {
                    try
                    {
                        string address = row.Cells["columnAddress"].Value?.ToString();
                        string dataType = row.Cells["columnDataType"].Value?.ToString();
                        if (!string.IsNullOrEmpty(address) && !string.IsNullOrEmpty(dataType))
                        {
                            var dataTypeEnum = StringToDataType(dataType);
                            readRequests.Add(new BatchReadRequest
                            {
                                Address = address,
                                DataType = dataTypeEnum,
                                Length = 1
                            });
                            validRows.Add(row);
                        }
                    }
                    catch (Exception ex)
                    {
                        row.Cells["columnResult"].Value = $"错误: {ex.Message}";
                        _logService.Warning("监控读取数据类型转换错误");
                    }
                }
            }
            
            // 执行批量读取
            if (readRequests.Count > 0)
            {
                try
                {
                    var result = await _readWriteService.BatchReadAsync(readRequests.ToArray());
                    if (result.Success)
                    {
                        for (int i = 0; i < result.Data.Length && i < validRows.Count; i++)
                        {
                            var row = validRows[i];
                            row.Cells["columnValue"].Value = result.Data[i];
                            row.Cells["columnResult"].Value = "成功";
                            _logService.Debug($"监控读取: {readRequests[i].Address} = {result.Data[i]}");
                        }
                    }
                    else
                    {
                        foreach (var row in validRows)
                        {
                            row.Cells["columnResult"].Value = $"失败: {result.Error.Message}";
                        }
                        _logService.Error("监控批量读取失败", null, result.Error.Message);
                    }
                }
                catch (Exception ex)
                {
                    foreach (var row in validRows)
                    {
                        row.Cells["columnResult"].Value = $"错误: {ex.Message}";
                    }
                    _logService.Error("监控读取异常", ex);
                }
            }
        }

        /// <summary>
        /// 更新连接状态
        /// </summary>
        public void UpdateConnectionStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateConnectionStatus), status);
                return;
            }

            _logService.Info($"连接状态更新: {status}");
        }

        /// <summary>
        /// 主窗体关闭事件
        /// </summary>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 停止监控定时器
            if (_monitorTimer != null)
            {
                _monitorTimer.Dispose();
                _monitorTimer = null;
            }
        }

        /// <summary>
        /// 清除日志按钮点击事件
        /// </summary>
        private void BtnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        /// <summary>
        /// 保存日志按钮点击事件
        /// </summary>
        private void BtnSaveLog_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "日志文件|*.log|文本文件|*.txt";
                saveDialog.FileName = $"log_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllText(saveDialog.FileName, txtLog.Text);
                    AppendLog($"日志已保存到 {saveDialog.FileName}", "INFO");
                }
            }
        }

        /// <summary>
        /// 加载日志按钮点击事件
        /// </summary>
        private void BtnLoadLog_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "日志文件|*.log|文本文件|*.txt";
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        txtLog.Text = System.IO.File.ReadAllText(openDialog.FileName);
                        AppendLog($"已加载日志文件 {openDialog.FileName}", "INFO");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"加载日志失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// 追加日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="logType">日志类型</param>
        private void AppendLog(string message, string logType = "INFO")
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logEntry = $"[{timestamp}] [{logType}] {message}\r\n";
            txtLog.AppendText(logEntry);
            txtLog.ScrollToCaret();
        }

        /// <summary>
        /// 处理日志添加事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="logEntry">日志条目</param>
        private void LogService_LogAdded(object sender, string logEntry)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, string>(LogService_LogAdded), sender, logEntry);
                return;
            }

            txtLog.AppendText(logEntry + "\r\n");
            txtLog.ScrollToCaret();
        }
        
        /// <summary>
        /// 处理通讯帧事件
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void ConnectionService_FrameReceived(object sender, MelsecPLCCommunicator.Infrastructure.Adapters.FrameEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.SendFrame))
            {
                AppendLog($"发送帧: {e.SendFrame}", "DEBUG");
            }
            if (!string.IsNullOrEmpty(e.ReceiveFrame))
            {
                AppendLog($"接收帧: {e.ReceiveFrame}", "DEBUG");
            }
        }

        // 菜单事件处理
        private void NewConnection_Click(object sender, EventArgs e)
        {
            BtnConnect_Click(sender, e);
        }

        private void SaveConfig_Click(object sender, EventArgs e)
        {
            if (_currentConfig != null)
            {
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "配置文件|*.json";
                    saveDialog.FileName = $"config_{_currentConfig.ConnectionName}.json";
                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        bool saved = _settingsService.SaveConnectionConfig(_currentConfig, saveDialog.FileName);
                        if (saved)
                        {
                            AppendLog($"连接配置已保存到 {saveDialog.FileName}", "INFO");
                        }
                        else
                        {
                            AppendLog("保存连接配置失败", "ERROR");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("没有活动的连接配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LoadConfig_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "配置文件|*.json";
                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // 加载连接配置
                        _currentConfig = _settingsService.LoadConnectionConfig(openDialog.FileName);
                        if (_currentConfig != null)
                        {
                            AppendLog($"已加载连接配置文件 {openDialog.FileName}", "INFO");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BatchOperation_Click(object sender, EventArgs e)
        {
            // 批量操作功能已集成到主窗体
            // 无需打开新窗口
        }
       

        private void ConnectionMonitor_Click(object sender, EventArgs e)
        {
            try
            {
                // 打开连接监控窗体
                var monitorForm = _serviceProvider.GetRequiredService<ConnectionMonitorForm>();
                monitorForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开连接监控窗体失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog($"打开连接监控窗体失败: {ex.Message}", "ERROR");
            }
        }

        private void DeviceMonitor_Click(object sender, EventArgs e)
        {
            try
            {
                // 打开设备监控窗体
                var monitorForm = _serviceProvider.GetRequiredService<DeviceMonitorForm>();
                monitorForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开设备监控窗体失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog($"打开设备监控窗体失败: {ex.Message}", "ERROR");
            }
        }

        private void ErrorCodeLookup_Click(object sender, EventArgs e)
        {
            try
            {
                // 打开错误码查询窗体
                var lookupForm = _serviceProvider.GetRequiredService<ErrorCodeLookupForm>();
                lookupForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开错误码查询窗体失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog($"打开错误码查询窗体失败: {ex.Message}", "ERROR");
            }
        }

        private void FrameAnalyzer_Click(object sender, EventArgs e)
        {
            try
            {
                // 打开帧分析器窗体
                var analyzerForm = _serviceProvider.GetRequiredService<FrameAnalyzerForm>();
                analyzerForm.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开帧分析器窗体失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog($"打开帧分析器窗体失败: {ex.Message}", "ERROR");
            }
        }

        private void About_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Mitsubishi PLC Communication V3\n\n版本: 3.0.0\n\n用于与三菱PLC进行通信的应用程序", "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 清除表格按钮点击事件
        /// </summary>
        private void BtnClearMonitor_Click(object sender, EventArgs e)
        {
            dataGridViewBatch.Rows.Clear();
        }

        /// <summary>
        /// 更新连接名称下拉框
        /// </summary>
        private void UpdateConnectionNameComboBox()
        {
            cmbConnectionName.Items.Clear();
            if (_currentConfig != null && !string.IsNullOrEmpty(_currentConfig.ConnectionName))
            {
                cmbConnectionName.Items.Add(_currentConfig.ConnectionName);
                cmbConnectionName.SelectedIndex = 0;
                cmbConnectionName.Enabled = true;
            }
            else
            {
                // 如果没有连接，添加默认项
                cmbConnectionName.Items.Add("无活动连接");
                cmbConnectionName.SelectedIndex = 0;
                cmbConnectionName.Enabled = false;
            }
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.panelMain = new System.Windows.Forms.Panel();
            this.btnClearMonitor = new System.Windows.Forms.Button();
            this.groupBoxConnection = new System.Windows.Forms.GroupBox();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.btnConnect = new System.Windows.Forms.Button();
            this.lblConnectionStatus = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBoxSettings = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.cmbConnectionName = new System.Windows.Forms.ComboBox();
            this.checkBoxMonitor = new System.Windows.Forms.CheckBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txtMonitorInterval = new System.Windows.Forms.TextBox();
            this.groupBoxMode = new System.Windows.Forms.GroupBox();
            this.label13 = new System.Windows.Forms.Label();
            this.cmbReadWriteMode = new System.Windows.Forms.ComboBox();
            this.label14 = new System.Windows.Forms.Label();
            this.domainUpDownDataCount = new System.Windows.Forms.DomainUpDown();
            this.groupBoxBatch = new System.Windows.Forms.GroupBox();
            this.cmbOperationType = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.cmbDataType = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtAddress = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.btnAddRow = new System.Windows.Forms.Button();
            this.btnDeleteRow = new System.Windows.Forms.Button();
            this.dataGridViewBatch = new System.Windows.Forms.DataGridView();
            this.columnDataType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.columnAddress = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.columnOperationType = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.columnValue = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.columnResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnReadAll = new System.Windows.Forms.Button();
            this.btnWriteAll = new System.Windows.Forms.Button();
            this.panelLog = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.btnLoadLog = new System.Windows.Forms.Button();
            this.btnSaveLog = new System.Windows.Forms.Button();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.panelMain.SuspendLayout();
            this.groupBoxConnection.SuspendLayout();
            this.groupBoxSettings.SuspendLayout();
            this.groupBoxMode.SuspendLayout();
            this.groupBoxBatch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewBatch)).BeginInit();
            this.panelLog.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(40, 40);
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(5, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(2389, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.btnClearMonitor);
            this.panelMain.Controls.Add(this.groupBoxConnection);
            this.panelMain.Controls.Add(this.groupBoxSettings);
            this.panelMain.Controls.Add(this.groupBoxMode);
            this.panelMain.Controls.Add(this.groupBoxBatch);
            this.panelMain.Controls.Add(this.btnAddRow);
            this.panelMain.Controls.Add(this.btnDeleteRow);
            this.panelMain.Controls.Add(this.dataGridViewBatch);
            this.panelMain.Controls.Add(this.btnReadAll);
            this.panelMain.Controls.Add(this.btnWriteAll);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelMain.Location = new System.Drawing.Point(0, 24);
            this.panelMain.Margin = new System.Windows.Forms.Padding(8);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(1151, 1381);
            this.panelMain.TabIndex = 1;
            // 
            // btnClearMonitor
            // 
            this.btnClearMonitor.Location = new System.Drawing.Point(26, 520);
            this.btnClearMonitor.Margin = new System.Windows.Forms.Padding(8);
            this.btnClearMonitor.Name = "btnClearMonitor";
            this.btnClearMonitor.Size = new System.Drawing.Size(188, 70);
            this.btnClearMonitor.TabIndex = 9;
            this.btnClearMonitor.Text = "清除表格";
            this.btnClearMonitor.UseVisualStyleBackColor = true;
            this.btnClearMonitor.Click += new System.EventHandler(this.BtnClearMonitor_Click);
            // 
            // groupBoxConnection
            // 
            this.groupBoxConnection.Controls.Add(this.btnDisconnect);
            this.groupBoxConnection.Controls.Add(this.btnConnect);
            this.groupBoxConnection.Controls.Add(this.lblConnectionStatus);
            this.groupBoxConnection.Controls.Add(this.label1);
            this.groupBoxConnection.Location = new System.Drawing.Point(28, 28);
            this.groupBoxConnection.Margin = new System.Windows.Forms.Padding(8);
            this.groupBoxConnection.Name = "groupBoxConnection";
            this.groupBoxConnection.Padding = new System.Windows.Forms.Padding(8);
            this.groupBoxConnection.Size = new System.Drawing.Size(1099, 105);
            this.groupBoxConnection.TabIndex = 0;
            this.groupBoxConnection.TabStop = false;
            this.groupBoxConnection.Text = "连接状态";
            // 
            // btnDisconnect
            // 
            this.btnDisconnect.Location = new System.Drawing.Point(712, 28);
            this.btnDisconnect.Margin = new System.Windows.Forms.Padding(8);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(188, 70);
            this.btnDisconnect.TabIndex = 3;
            this.btnDisconnect.Text = "断开";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.BtnDisconnect_Click);
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(502, 28);
            this.btnConnect.Margin = new System.Windows.Forms.Padding(8);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(188, 70);
            this.btnConnect.TabIndex = 2;
            this.btnConnect.Text = "连接";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.BtnConnect_Click);
            // 
            // lblConnectionStatus
            // 
            this.lblConnectionStatus.AutoSize = true;
            this.lblConnectionStatus.Location = new System.Drawing.Point(220, 45);
            this.lblConnectionStatus.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.lblConnectionStatus.Name = "lblConnectionStatus";
            this.lblConnectionStatus.Size = new System.Drawing.Size(103, 30);
            this.lblConnectionStatus.TabIndex = 1;
            this.lblConnectionStatus.Text = "未连接";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(33, 45);
            this.label1.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(163, 30);
            this.label1.TabIndex = 0;
            this.label1.Text = "当前状态：";
            // 
            // groupBoxSettings
            // 
            this.groupBoxSettings.Controls.Add(this.label7);
            this.groupBoxSettings.Controls.Add(this.cmbConnectionName);
            this.groupBoxSettings.Controls.Add(this.checkBoxMonitor);
            this.groupBoxSettings.Controls.Add(this.label12);
            this.groupBoxSettings.Controls.Add(this.txtMonitorInterval);
            this.groupBoxSettings.Location = new System.Drawing.Point(28, 148);
            this.groupBoxSettings.Margin = new System.Windows.Forms.Padding(8);
            this.groupBoxSettings.Name = "groupBoxSettings";
            this.groupBoxSettings.Padding = new System.Windows.Forms.Padding(8);
            this.groupBoxSettings.Size = new System.Drawing.Size(1099, 129);
            this.groupBoxSettings.TabIndex = 1;
            this.groupBoxSettings.TabStop = false;
            this.groupBoxSettings.Text = "连接设置";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(23, 58);
            this.label7.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(163, 30);
            this.label7.TabIndex = 0;
            this.label7.Text = "连接名称：";
            // 
            // cmbConnectionName
            // 
            this.cmbConnectionName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbConnectionName.Location = new System.Drawing.Point(192, 58);
            this.cmbConnectionName.Margin = new System.Windows.Forms.Padding(8);
            this.cmbConnectionName.Name = "cmbConnectionName";
            this.cmbConnectionName.Size = new System.Drawing.Size(360, 38);
            this.cmbConnectionName.TabIndex = 1;
            // 
            // checkBoxMonitor
            // 
            this.checkBoxMonitor.AutoSize = true;
            this.checkBoxMonitor.Location = new System.Drawing.Point(568, 58);
            this.checkBoxMonitor.Margin = new System.Windows.Forms.Padding(8);
            this.checkBoxMonitor.Name = "checkBoxMonitor";
            this.checkBoxMonitor.Size = new System.Drawing.Size(171, 34);
            this.checkBoxMonitor.TabIndex = 2;
            this.checkBoxMonitor.Text = "监控模式";
            this.checkBoxMonitor.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(756, 58);
            this.label12.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(163, 30);
            this.label12.TabIndex = 3;
            this.label12.Text = "监控间隔：";
            // 
            // txtMonitorInterval
            // 
            this.txtMonitorInterval.Location = new System.Drawing.Point(921, 52);
            this.txtMonitorInterval.Margin = new System.Windows.Forms.Padding(8);
            this.txtMonitorInterval.Name = "txtMonitorInterval";
            this.txtMonitorInterval.Size = new System.Drawing.Size(135, 42);
            this.txtMonitorInterval.TabIndex = 4;
            this.txtMonitorInterval.Text = "1000";
            // 
            // groupBoxMode
            // 
            this.groupBoxMode.Controls.Add(this.label13);
            this.groupBoxMode.Controls.Add(this.cmbReadWriteMode);
            this.groupBoxMode.Controls.Add(this.label14);
            this.groupBoxMode.Controls.Add(this.domainUpDownDataCount);
            this.groupBoxMode.Location = new System.Drawing.Point(28, 277);
            this.groupBoxMode.Margin = new System.Windows.Forms.Padding(8);
            this.groupBoxMode.Name = "groupBoxMode";
            this.groupBoxMode.Padding = new System.Windows.Forms.Padding(8);
            this.groupBoxMode.Size = new System.Drawing.Size(1099, 117);
            this.groupBoxMode.TabIndex = 2;
            this.groupBoxMode.TabStop = false;
            this.groupBoxMode.Text = "操作模式";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(23, 47);
            this.label13.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(163, 30);
            this.label13.TabIndex = 0;
            this.label13.Text = "读写模式：";
            // 
            // cmbReadWriteMode
            // 
            this.cmbReadWriteMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbReadWriteMode.Location = new System.Drawing.Point(199, 39);
            this.cmbReadWriteMode.Margin = new System.Windows.Forms.Padding(8);
            this.cmbReadWriteMode.Name = "cmbReadWriteMode";
            this.cmbReadWriteMode.Size = new System.Drawing.Size(229, 38);
            this.cmbReadWriteMode.TabIndex = 1;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(445, 47);
            this.label14.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(163, 30);
            this.label14.TabIndex = 2;
            this.label14.Text = "数据数量：";
            // 
            // domainUpDownDataCount
            // 
            this.domainUpDownDataCount.Location = new System.Drawing.Point(609, 39);
            this.domainUpDownDataCount.Margin = new System.Windows.Forms.Padding(8);
            this.domainUpDownDataCount.Name = "domainUpDownDataCount";
            this.domainUpDownDataCount.Size = new System.Drawing.Size(141, 42);
            this.domainUpDownDataCount.TabIndex = 3;
            // 
            // groupBoxBatch
            // 
            this.groupBoxBatch.Controls.Add(this.cmbOperationType);
            this.groupBoxBatch.Controls.Add(this.label10);
            this.groupBoxBatch.Controls.Add(this.cmbDataType);
            this.groupBoxBatch.Controls.Add(this.label9);
            this.groupBoxBatch.Controls.Add(this.txtAddress);
            this.groupBoxBatch.Controls.Add(this.label8);
            this.groupBoxBatch.Location = new System.Drawing.Point(28, 394);
            this.groupBoxBatch.Margin = new System.Windows.Forms.Padding(8);
            this.groupBoxBatch.Name = "groupBoxBatch";
            this.groupBoxBatch.Padding = new System.Windows.Forms.Padding(8);
            this.groupBoxBatch.Size = new System.Drawing.Size(1099, 122);
            this.groupBoxBatch.TabIndex = 3;
            this.groupBoxBatch.TabStop = false;
            this.groupBoxBatch.Text = "添加数据";
            // 
            // cmbOperationType
            // 
            this.cmbOperationType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOperationType.Location = new System.Drawing.Point(712, 52);
            this.cmbOperationType.Margin = new System.Windows.Forms.Padding(8);
            this.cmbOperationType.Name = "cmbOperationType";
            this.cmbOperationType.Size = new System.Drawing.Size(126, 38);
            this.cmbOperationType.TabIndex = 5;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(548, 61);
            this.label10.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(163, 30);
            this.label10.TabIndex = 4;
            this.label10.Text = "操作类型：";
            // 
            // cmbDataType
            // 
            this.cmbDataType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDataType.Location = new System.Drawing.Point(129, 52);
            this.cmbDataType.Margin = new System.Windows.Forms.Padding(8);
            this.cmbDataType.Name = "cmbDataType";
            this.cmbDataType.Size = new System.Drawing.Size(107, 38);
            this.cmbDataType.TabIndex = 3;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(11, 61);
            this.label9.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(103, 30);
            this.label9.TabIndex = 2;
            this.label9.Text = "类型：";
            // 
            // txtAddress
            // 
            this.txtAddress.Location = new System.Drawing.Point(345, 52);
            this.txtAddress.Margin = new System.Windows.Forms.Padding(8);
            this.txtAddress.Name = "txtAddress";
            this.txtAddress.Size = new System.Drawing.Size(182, 42);
            this.txtAddress.TabIndex = 1;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(246, 61);
            this.label8.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(103, 30);
            this.label8.TabIndex = 0;
            this.label8.Text = "地址：";
            // 
            // btnAddRow
            // 
            this.btnAddRow.Location = new System.Drawing.Point(227, 520);
            this.btnAddRow.Margin = new System.Windows.Forms.Padding(8);
            this.btnAddRow.Name = "btnAddRow";
            this.btnAddRow.Size = new System.Drawing.Size(188, 70);
            this.btnAddRow.TabIndex = 4;
            this.btnAddRow.Text = "添加行";
            this.btnAddRow.UseVisualStyleBackColor = true;
            this.btnAddRow.Click += new System.EventHandler(this.BtnAddRow_Click);
            // 
            // btnDeleteRow
            // 
            this.btnDeleteRow.Location = new System.Drawing.Point(429, 520);
            this.btnDeleteRow.Margin = new System.Windows.Forms.Padding(8);
            this.btnDeleteRow.Name = "btnDeleteRow";
            this.btnDeleteRow.Size = new System.Drawing.Size(188, 70);
            this.btnDeleteRow.TabIndex = 5;
            this.btnDeleteRow.Text = "删除行";
            this.btnDeleteRow.UseVisualStyleBackColor = true;
            this.btnDeleteRow.Click += new System.EventHandler(this.BtnDeleteRow_Click);
            // 
            // dataGridViewBatch
            // 
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.LightCyan;
            this.dataGridViewBatch.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dataGridViewBatch.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.LightSteelBlue;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("微软雅黑", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.DarkBlue;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridViewBatch.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dataGridViewBatch.ColumnHeadersHeight = 60;
            this.dataGridViewBatch.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dataGridViewBatch.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.columnDataType,
            this.columnAddress,
            this.columnOperationType,
            this.columnValue,
            this.columnResult});
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle5.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("微软雅黑", 9.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle5.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewBatch.DefaultCellStyle = dataGridViewCellStyle5;
            this.dataGridViewBatch.GridColor = System.Drawing.Color.LightGray;
            this.dataGridViewBatch.Location = new System.Drawing.Point(28, 606);
            this.dataGridViewBatch.Margin = new System.Windows.Forms.Padding(8);
            this.dataGridViewBatch.Name = "dataGridViewBatch";
            this.dataGridViewBatch.RowHeadersWidth = 60;
            this.dataGridViewBatch.RowTemplate.Height = 50;
            this.dataGridViewBatch.Size = new System.Drawing.Size(1099, 679);
            this.dataGridViewBatch.TabIndex = 6;
            // 
            // columnDataType
            // 
            this.columnDataType.DropDownWidth = 180;
            this.columnDataType.HeaderText = "数据类型";
            this.columnDataType.MinimumWidth = 100;
            this.columnDataType.Name = "columnDataType";
            this.columnDataType.Width = 180;
            // 
            // columnAddress
            // 
            this.columnAddress.HeaderText = "地址";
            this.columnAddress.MinimumWidth = 150;
            this.columnAddress.Name = "columnAddress";
            this.columnAddress.Width = 220;
            // 
            // columnOperationType
            // 
            this.columnOperationType.DropDownWidth = 180;
            this.columnOperationType.HeaderText = "操作类型";
            this.columnOperationType.MinimumWidth = 100;
            this.columnOperationType.Name = "columnOperationType";
            this.columnOperationType.Width = 180;
            // 
            // columnValue
            // 
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.columnValue.DefaultCellStyle = dataGridViewCellStyle3;
            this.columnValue.HeaderText = "值";
            this.columnValue.MinimumWidth = 150;
            this.columnValue.Name = "columnValue";
            this.columnValue.Width = 200;
            // 
            // columnResult
            // 
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.columnResult.DefaultCellStyle = dataGridViewCellStyle4;
            this.columnResult.HeaderText = "结果";
            this.columnResult.MinimumWidth = 250;
            this.columnResult.Name = "columnResult";
            this.columnResult.Width = 320;
            // 
            // btnReadAll
            // 
            this.btnReadAll.Location = new System.Drawing.Point(720, 520);
            this.btnReadAll.Margin = new System.Windows.Forms.Padding(8);
            this.btnReadAll.Name = "btnReadAll";
            this.btnReadAll.Size = new System.Drawing.Size(188, 70);
            this.btnReadAll.TabIndex = 7;
            this.btnReadAll.Text = "批量读取";
            this.btnReadAll.UseVisualStyleBackColor = true;
            this.btnReadAll.Click += new System.EventHandler(this.BtnReadAll_Click);
            // 
            // btnWriteAll
            // 
            this.btnWriteAll.Location = new System.Drawing.Point(939, 520);
            this.btnWriteAll.Margin = new System.Windows.Forms.Padding(8);
            this.btnWriteAll.Name = "btnWriteAll";
            this.btnWriteAll.Size = new System.Drawing.Size(188, 70);
            this.btnWriteAll.TabIndex = 8;
            this.btnWriteAll.Text = "批量写入";
            this.btnWriteAll.UseVisualStyleBackColor = true;
            this.btnWriteAll.Click += new System.EventHandler(this.BtnWriteAll_Click);
            // 
            // panelLog
            // 
            this.panelLog.Controls.Add(this.label6);
            this.panelLog.Controls.Add(this.btnLoadLog);
            this.panelLog.Controls.Add(this.btnSaveLog);
            this.panelLog.Controls.Add(this.btnClearLog);
            this.panelLog.Controls.Add(this.txtLog);
            this.panelLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLog.Location = new System.Drawing.Point(1151, 24);
            this.panelLog.Margin = new System.Windows.Forms.Padding(8);
            this.panelLog.Name = "panelLog";
            this.panelLog.Size = new System.Drawing.Size(1238, 1381);
            this.panelLog.TabIndex = 2;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(38, 23);
            this.label6.Margin = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(103, 30);
            this.label6.TabIndex = 4;
            this.label6.Text = "日志：";
            // 
            // btnLoadLog
            // 
            this.btnLoadLog.Location = new System.Drawing.Point(527, 11);
            this.btnLoadLog.Margin = new System.Windows.Forms.Padding(8);
            this.btnLoadLog.Name = "btnLoadLog";
            this.btnLoadLog.Size = new System.Drawing.Size(176, 54);
            this.btnLoadLog.TabIndex = 3;
            this.btnLoadLog.Text = "加载";
            this.btnLoadLog.UseVisualStyleBackColor = true;
            // 
            // btnSaveLog
            // 
            this.btnSaveLog.Location = new System.Drawing.Point(338, 11);
            this.btnSaveLog.Margin = new System.Windows.Forms.Padding(8);
            this.btnSaveLog.Name = "btnSaveLog";
            this.btnSaveLog.Size = new System.Drawing.Size(176, 54);
            this.btnSaveLog.TabIndex = 2;
            this.btnSaveLog.Text = "保存";
            this.btnSaveLog.UseVisualStyleBackColor = true;
            // 
            // btnClearLog
            // 
            this.btnClearLog.Location = new System.Drawing.Point(148, 11);
            this.btnClearLog.Margin = new System.Windows.Forms.Padding(8);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(176, 54);
            this.btnClearLog.TabIndex = 1;
            this.btnClearLog.Text = "清除";
            this.btnClearLog.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(14, 73);
            this.txtLog.Margin = new System.Windows.Forms.Padding(8);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(1208, 1212);
            this.txtLog.TabIndex = 0;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(40, 40);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripProgressBar1});
            this.statusStrip1.Location = new System.Drawing.Point(1151, 1357);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(2, 0, 33, 0);
            this.statusStrip1.Size = new System.Drawing.Size(1238, 48);
            this.statusStrip1.TabIndex = 3;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 32);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(15F, 30F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2389, 1405);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.panelLog);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(8);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Mitsubishi PLC Communication V3";
            this.panelMain.ResumeLayout(false);
            this.groupBoxConnection.ResumeLayout(false);
            this.groupBoxConnection.PerformLayout();
            this.groupBoxSettings.ResumeLayout(false);
            this.groupBoxSettings.PerformLayout();
            this.groupBoxMode.ResumeLayout(false);
            this.groupBoxMode.PerformLayout();
            this.groupBoxBatch.ResumeLayout(false);
            this.groupBoxBatch.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewBatch)).EndInit();
            this.panelLog.ResumeLayout(false);
            this.panelLog.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.GroupBox groupBoxConnection;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Label lblConnectionStatus;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBoxSettings;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cmbConnectionName;
        private System.Windows.Forms.CheckBox checkBoxMonitor;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtMonitorInterval;
        private System.Windows.Forms.GroupBox groupBoxMode;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox cmbReadWriteMode;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.DomainUpDown domainUpDownDataCount;
        private System.Windows.Forms.GroupBox groupBoxBatch;
        private System.Windows.Forms.ComboBox cmbOperationType;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox cmbDataType;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtAddress;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button btnAddRow;
        private System.Windows.Forms.Button btnDeleteRow;
        private System.Windows.Forms.DataGridView dataGridViewBatch;
        private System.Windows.Forms.Button btnReadAll;
        private System.Windows.Forms.Button btnWriteAll;
        private System.Windows.Forms.Button btnClearMonitor;
        private System.Windows.Forms.Panel panelLog;
        private System.Windows.Forms.Button btnLoadLog;
        private System.Windows.Forms.Button btnSaveLog;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.StatusStrip statusStrip1;

       
    }
}