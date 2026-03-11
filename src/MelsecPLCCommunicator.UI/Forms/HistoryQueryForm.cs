using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using MelsecPLCCommunicator.Application.Services;
using MelsecPLCCommunicator.Domain.Models;
using MelsecPLCCommunicator.Infrastructure.Services;

namespace MelsecPLCCommunicator.UI.Forms
{
    public partial class HistoryQueryForm : Form
    {
        private HistoryQueryService _historyQueryService;
        private List<PlcVariable> _variables;
        private List<string> _historyInputs;
        private CheckBox checkBoxAlarmLog;
        private CheckBox checkBoxHistoryQuery;
        private string _historyFilePath;

        public HistoryQueryForm()
        {
            InitializeComponent();
            _historyFilePath = Path.Combine(System.Windows.Forms.Application.StartupPath, "history_inputs.txt");
            LoadHistoryInputs();
            
            // 设置默认时间范围为最近24小时
            dtpStart.Value = DateTime.Now.AddHours(-24);
            dtpEnd.Value = DateTime.Now;
        }

        public void SetHistoryQueryService(HistoryQueryService historyQueryService)
        {
            _historyQueryService = historyQueryService;
            LoadVariables();
        }

        private void LoadHistoryInputs()
        {
            // 从文件中加载历史输入记录
            _historyInputs = new List<string>();
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var lines = File.ReadAllLines(_historyFilePath);
                    _historyInputs = lines.Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
            }
            catch { }
        }

        private void SaveHistoryInputs()
        {
            // 保存历史输入记录到文件
            try
            {
                var lines = _historyInputs.Take(10).ToArray(); // 只保留最近10条
                File.WriteAllLines(_historyFilePath, lines);
            }
            catch { }
        }

        private async void LoadVariables()
        {
            // 从数据库加载变量列表
            _variables = new List<PlcVariable>();
            try
            {
                // 创建一个临时的 DatabaseService 来获取所有变量
                var databaseService = new DatabaseService("Data Source=Database\\plc_communication.db;Version=3;");
                _variables = databaseService.GetAllVariables();
                databaseService.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载变量失败: {ex.Message}");
            }

            // 为了满足异步方法的要求，添加一个await
            await Task.Yield();

            cmbVariable.Items.Clear();
            cmbVariable.Items.Add("所有变量");
            
            // 添加历史输入记录
            foreach (var input in _historyInputs)
            {
                if (!cmbVariable.Items.Contains(input))
                {
                    cmbVariable.Items.Add(input);
                }
            }
            
            foreach (var variable in _variables)
            {
                var itemText = $"{variable.Name} ({variable.Address})";
                if (!cmbVariable.Items.Contains(itemText))
                {
                    cmbVariable.Items.Add(itemText);
                }
            }
            
            // 默认选择"所有变量"
            cmbVariable.SelectedIndex = 0;
        }

        /// <summary>
        /// 验证地址格式是否正确
        /// </summary>
        /// <param name="address">变量地址</param>
        /// <returns>地址格式是否正确</returns>
        private bool IsValidAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return false;
            }

            // 地址格式：字母 + 数字
            string pattern = @"^[A-Za-z]+\d+$";
            return System.Text.RegularExpressions.Regex.IsMatch(address, pattern);
        }

        private void btnQuery_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查是否选择了历史数据查询或报警查询
                bool isHistoryQuery = checkBoxHistoryQuery.Checked;
                bool isAlarmQuery = checkBoxAlarmLog.Checked;

                if (!isHistoryQuery && !isAlarmQuery)
                {
                    MessageBox.Show("请选择查询类型：历史数据查询或报警历史查询", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 如果选择了历史数据查询
                if (isHistoryQuery)
                {
                    string variableAddress = null;
                    int? variableId = null;
                    
                    // 处理用户输入
                    if (cmbVariable.SelectedIndex == 0)
                    {
                        // 选择了"所有变量"
                        variableAddress = null;
                        variableId = null;
                    }
                    else if (cmbVariable.SelectedIndex > 0)
                    {
                        // 从下拉框中选择
                        var selectedText = cmbVariable.SelectedItem.ToString();
                        
                        // 检查是否是历史输入记录（纯地址格式）
                        if (selectedText.StartsWith("D") || selectedText.StartsWith("M") || selectedText.StartsWith("Y") || selectedText.StartsWith("X"))
                        {
                            variableAddress = selectedText;
                        }
                        else
                        {
                            // 尝试从显示文本中提取地址
                            int startIndex = selectedText.LastIndexOf("(") + 1;
                            int endIndex = selectedText.LastIndexOf(")");
                            if (startIndex > 0 && endIndex > startIndex)
                            {
                                variableAddress = selectedText.Substring(startIndex, endIndex - startIndex);
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(cmbVariable.Text))
                    {
                        // 用户手动输入的地址
                        variableAddress = cmbVariable.Text.Trim();
                        
                        // 添加到历史记录
                        if (!string.IsNullOrEmpty(variableAddress) && !_historyInputs.Contains(variableAddress))
                        {
                            _historyInputs.Insert(0, variableAddress);
                            SaveHistoryInputs();
                            // 重新加载变量列表以显示新的历史记录
                            LoadVariables();
                        }
                    }

                    // 如果指定了变量地址，获取或创建变量
                    if (!string.IsNullOrEmpty(variableAddress))
                    {
                        // 验证地址格式
                        if (!IsValidAddress(variableAddress))
                        {
                            MessageBox.Show($"地址格式无效！请输入正确的地址格式，如 D0、M10 等。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        
                        try
                        {
                            // 尝试从数据库获取变量
                            var variable = _historyQueryService.GetOrCreateVariable(variableAddress);
                            if (variable == null)
                            {
                                MessageBox.Show($"变量 {variableAddress} 不存在！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                            variableId = variable.Id;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"获取或创建变量失败: {ex.Message}");
                            MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    var records = _historyQueryService.QueryReadRecords(
                        variableId,
                        dtpStart.Value,
                        dtpEnd.Value,
                        1000,
                        1);

                    // 转换为自定义的显示模型，按照要求显示：变量类型，变量地址，原始值，工程值，时间戳
                    var displayRecords = records.Select(r => new
                    {
                        //变量ID = r.VariableId,
                        变量地址 = $" {r.Variable?.DataType?.ToString() ?? ""}{r.Variable?.Address ?? ""}",
                        原始值 = r.RawValue,
                        工程值 = r.EngineeringValue,
                        时间戳 = r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                    }).ToList();

                    // 绑定到表格
                    dgvRecords.DataSource = displayRecords;
                    lblRecordCount.Text = $"共 {displayRecords.Count} 条记录";
                }

                // 如果选择了报警历史查询
                if (isAlarmQuery)
                {
                    var alarmLogs = _historyQueryService.QueryAlarmLogs(
                        dtpStart.Value,
                        dtpEnd.Value,
                        1000,
                        1);

                    // 转换为自定义的显示模型，按照要求显示：error_code，分级，发生时间与处理结果
                    var displayLogs = alarmLogs.Select(log => new
                    {
                        error_code = log.ErrorCode,
                        分级 = log.Severity,
                        发生时间 = log.Timestamp,
                        处理结果 = log.IsResolved ? (log.ResolvedAt.HasValue ? $"已处理 ({log.ResolvedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")})" : "已处理") : "未处理"
                    }).ToList();

                    // 绑定到表格
                    dgvRecords.DataSource = displayLogs;
                    lblRecordCount.Text = $"共 {displayLogs.Count} 条报警记录";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"查询失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV文件 (*.csv)|*.csv";
                    saveFileDialog.Title = "导出数据到CSV文件";
                    saveFileDialog.FileName = $"PLC数据_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.csv";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        int? variableId = null;
                        if (cmbVariable.SelectedIndex > 0)
                        {
                            variableId = _variables[cmbVariable.SelectedIndex - 1].Id;
                        }

                        var progressForm = new ProgressForm();
                        progressForm.Show();

                        bool success = _historyQueryService.ExportReadRecordsToCsv(
                            saveFileDialog.FileName,
                            variableId,
                            dtpStart.Value,
                            dtpEnd.Value,
                            (current, total) =>
                            {
                                progressForm.UpdateProgress(current, total);
                            });

                        progressForm.Close();

                        if (success)
                        {
                            MessageBox.Show("导出成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("导出失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAggregate_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbVariable.SelectedIndex <= 0)
                {
                    MessageBox.Show("请选择一个变量", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var variableId = _variables[cmbVariable.SelectedIndex - 1].Id;
                string interval = cmbInterval.SelectedItem.ToString();

                var aggregatedData = _historyQueryService.AggregateQuery(
                    variableId,
                    dtpStart.Value,
                    dtpEnd.Value,
                    interval);

                dgvRecords.DataSource = aggregatedData;
                lblRecordCount.Text = $"共 {aggregatedData.Count} 条记录";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"聚合查询失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnArchive_Click(object sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show("确定要执行数据归档吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    int daysToKeep = (int)nudDaysToKeep.Value;
                    bool success = _historyQueryService.ArchiveData(daysToKeep);
                    if (success)
                    {
                        MessageBox.Show("数据归档成功", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("数据归档失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据归档失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBoxAlarmLog = new System.Windows.Forms.CheckBox();
            this.checkBoxHistoryQuery = new System.Windows.Forms.CheckBox();
            this.btnQuery = new System.Windows.Forms.Button();
            this.dtpEnd = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.dtpStart = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbVariable = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnExport = new System.Windows.Forms.Button();
            this.dgvRecords = new System.Windows.Forms.DataGridView();
            this.lblRecordCount = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnAggregate = new System.Windows.Forms.Button();
            this.cmbInterval = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnArchive = new System.Windows.Forms.Button();
            this.nudDaysToKeep = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecords)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDaysToKeep)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxAlarmLog);
            this.groupBox1.Controls.Add(this.checkBoxHistoryQuery);
            this.groupBox1.Controls.Add(this.btnQuery);
            this.groupBox1.Controls.Add(this.dtpEnd);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.dtpStart);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cmbVariable);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(760, 70);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "查询条件";
            // 
            // checkBoxAlarmLog
            // 
            this.checkBoxAlarmLog.AutoSize = true;
            this.checkBoxAlarmLog.Location = new System.Drawing.Point(22, 50);
            this.checkBoxAlarmLog.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            this.checkBoxAlarmLog.Name = "checkBoxAlarmLog";
            this.checkBoxAlarmLog.Size = new System.Drawing.Size(96, 16);
            this.checkBoxAlarmLog.TabIndex = 7;
            this.checkBoxAlarmLog.Text = "报警历史查询";
            this.checkBoxAlarmLog.UseVisualStyleBackColor = true;
            // 
            // checkBoxHistoryQuery
            // 
            this.checkBoxHistoryQuery.AutoSize = true;
            this.checkBoxHistoryQuery.Location = new System.Drawing.Point(22, 28);
            this.checkBoxHistoryQuery.Margin = new System.Windows.Forms.Padding(1, 1, 1, 1);
            this.checkBoxHistoryQuery.Name = "checkBoxHistoryQuery";
            this.checkBoxHistoryQuery.Size = new System.Drawing.Size(96, 16);
            this.checkBoxHistoryQuery.TabIndex = 6;
            this.checkBoxHistoryQuery.Text = "历史数据查询";
            this.checkBoxHistoryQuery.UseVisualStyleBackColor = true;
            // 
            // btnQuery
            // 
            this.btnQuery.Location = new System.Drawing.Point(680, 23);
            this.btnQuery.Name = "btnQuery";
            this.btnQuery.Size = new System.Drawing.Size(75, 23);
            this.btnQuery.TabIndex = 6;
            this.btnQuery.Text = "查询";
            this.btnQuery.UseVisualStyleBackColor = true;
            this.btnQuery.Click += new System.EventHandler(this.btnQuery_Click);
            // 
            // dtpEnd
            // 
            this.dtpEnd.Location = new System.Drawing.Point(520, 25);
            this.dtpEnd.Name = "dtpEnd";
            this.dtpEnd.Size = new System.Drawing.Size(150, 21);
            this.dtpEnd.TabIndex = 5;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(470, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(59, 12);
            this.label3.TabIndex = 4;
            this.label3.Text = "结束时间:";
            // 
            // dtpStart
            // 
            this.dtpStart.Location = new System.Drawing.Point(300, 25);
            this.dtpStart.Name = "dtpStart";
            this.dtpStart.Size = new System.Drawing.Size(150, 21);
            this.dtpStart.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(250, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "开始时间:";
            // 
            // cmbVariable
            // 
            this.cmbVariable.FormattingEnabled = true;
            this.cmbVariable.Location = new System.Drawing.Point(138, 26);
            this.cmbVariable.Name = "cmbVariable";
            this.cmbVariable.Size = new System.Drawing.Size(80, 20);
            this.cmbVariable.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(144, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "变量地址";
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(697, 93);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 23);
            this.btnExport.TabIndex = 1;
            this.btnExport.Text = "导出CSV";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // dgvRecords
            // 
            this.dgvRecords.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvRecords.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvRecords.Location = new System.Drawing.Point(12, 122);
            this.dgvRecords.Name = "dgvRecords";
            this.dgvRecords.RowHeadersWidth = 102;
            this.dgvRecords.Size = new System.Drawing.Size(760, 325);
            this.dgvRecords.TabIndex = 2;
            // 
            // lblRecordCount
            // 
            this.lblRecordCount.AutoSize = true;
            this.lblRecordCount.Location = new System.Drawing.Point(10, 93);
            this.lblRecordCount.Name = "lblRecordCount";
            this.lblRecordCount.Size = new System.Drawing.Size(47, 12);
            this.lblRecordCount.TabIndex = 3;
            this.lblRecordCount.Text = "记录数:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnAggregate);
            this.groupBox2.Controls.Add(this.cmbInterval);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Location = new System.Drawing.Point(12, 453);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(370, 70);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "聚合查询";
            // 
            // btnAggregate
            // 
            this.btnAggregate.Location = new System.Drawing.Point(280, 25);
            this.btnAggregate.Name = "btnAggregate";
            this.btnAggregate.Size = new System.Drawing.Size(75, 23);
            this.btnAggregate.TabIndex = 2;
            this.btnAggregate.Text = "聚合";
            this.btnAggregate.UseVisualStyleBackColor = true;
            this.btnAggregate.Click += new System.EventHandler(this.btnAggregate_Click);
            // 
            // cmbInterval
            // 
            this.cmbInterval.FormattingEnabled = true;
            this.cmbInterval.Items.AddRange(new object[] {
            "hour",
            "day",
            "month"});
            this.cmbInterval.Location = new System.Drawing.Point(80, 25);
            this.cmbInterval.Name = "cmbInterval";
            this.cmbInterval.Size = new System.Drawing.Size(150, 20);
            this.cmbInterval.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(20, 28);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 12);
            this.label4.TabIndex = 0;
            this.label4.Text = "聚合间隔:";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnArchive);
            this.groupBox3.Controls.Add(this.nudDaysToKeep);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Location = new System.Drawing.Point(398, 453);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(374, 70);
            this.groupBox3.TabIndex = 5;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "数据归档";
            // 
            // btnArchive
            // 
            this.btnArchive.Location = new System.Drawing.Point(280, 25);
            this.btnArchive.Name = "btnArchive";
            this.btnArchive.Size = new System.Drawing.Size(75, 23);
            this.btnArchive.TabIndex = 2;
            this.btnArchive.Text = "归档";
            this.btnArchive.UseVisualStyleBackColor = true;
            this.btnArchive.Click += new System.EventHandler(this.btnArchive_Click);
            // 
            // nudDaysToKeep
            // 
            this.nudDaysToKeep.Location = new System.Drawing.Point(120, 25);
            this.nudDaysToKeep.Name = "nudDaysToKeep";
            this.nudDaysToKeep.Size = new System.Drawing.Size(100, 21);
            this.nudDaysToKeep.TabIndex = 1;
            this.nudDaysToKeep.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(20, 28);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(95, 12);
            this.label5.TabIndex = 0;
            this.label5.Text = "保留天数（天）:";
            // 
            // HistoryQueryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 535);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.lblRecordCount);
            this.Controls.Add(this.dgvRecords);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.groupBox1);
            this.Name = "HistoryQueryForm";
            this.Text = "PLC数据历史查询";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecords)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDaysToKeep)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnQuery;
        private System.Windows.Forms.DateTimePicker dtpEnd;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DateTimePicker dtpStart;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbVariable;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.DataGridView dgvRecords;
        private System.Windows.Forms.Label lblRecordCount;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnAggregate;
        private System.Windows.Forms.ComboBox cmbInterval;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button btnArchive;
        private System.Windows.Forms.NumericUpDown nudDaysToKeep;
        private System.Windows.Forms.Label label5;
    }

    public partial class ProgressForm : Form
    {
        public ProgressForm()
        {
            InitializeComponent();
        }

        public void UpdateProgress(int current, int total)
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(() => UpdateProgress(current, total)));
            }
            else
            {
                progressBar1.Maximum = total;
                progressBar1.Value = current;
                lblProgress.Text = $"正在导出: {current}/{total}";
            }
        }

        private void InitializeComponent()
        {
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblProgress = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(20, 40);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(300, 20);
            this.progressBar1.TabIndex = 0;
            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.lblProgress.Location = new System.Drawing.Point(20, 20);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(71, 12);
            this.lblProgress.TabIndex = 1;
            this.lblProgress.Text = "正在导出...";
            // 
            // ProgressForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(340, 80);
            this.Controls.Add(this.lblProgress);
            this.Controls.Add(this.progressBar1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "导出进度";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblProgress;
    }
}