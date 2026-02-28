using System;
using System.Windows.Forms;
using MelsecPLCCommunicator.Application.Interfaces;

namespace MelsecPLCCommunicator.UI.Forms
{
    public partial class DeviceMonitorForm : Form
    {
        private readonly IPlcReadWriteService _readWriteService;

        public DeviceMonitorForm(IPlcReadWriteService readWriteService)
        {
            InitializeComponent();
            _readWriteService = readWriteService;
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = "设备监控";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(500, 400);
            this.Name = "DeviceMonitorForm";
            this.Text = "设备监控";
            this.ResumeLayout(false);
        }
    }
}