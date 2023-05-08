using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using TreeLog;
using CSharp411;
using LogViewer;
using Fluxus;

namespace ActTest
{
    public partial class frmMain : Form
    {
        private AppSettings mySettings = Program.Settings;
        public static LogWrapper Log = Program.LogWrap;
		public frmLogViewer frmLogView = null;
        private Actuator myController = null;
        private uint? eidInit = null;
        private const string szUnknown = "Unknown";
        private const string szEmpty = "";
        //private bool bIgnoreControlEvents = true;

        string[] ports;

        public frmMain()
        {
            InitializeComponent();
            WriteLogHeader();
        }

        private void RefreshPortList()
        {
            btnOpenPort.Enabled = false;
            btnSend.Enabled = false;
            cboPort.Text = szEmpty;
            if (myController != null)
                myController.AssignSerialPort(null);
            ports = Support_Serial.PortList();
            Support_Serial.PopulateComboBoxWithPortNames(ports, cboPort, szUnknown, szEmpty);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            myController = new Actuator();
            RefreshPortList();
        }


        private OpResult SendUserCommand(string UserCommand, string LogCaption)
        {
            string Response = null;
            OpType opType = Actuator.GetOpType(UserCommand);
            string Command = myController.AddCRC(UserCommand);
            OpResult result = myController.SendCommand(null, opType, Command, out Response, LogCaption);
            tsslStatus.Text = (result == OpResult.Success) ? "User command sent" : "User command failed";
            txtRawResult.Text = Response == null ? "" : Response;
            return result;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendUserCommand(txtCommand.Text, "User raw command");
        }

        #region Windows Info
        private string getWindowsInfo()
        {
            StringBuilder sb = new StringBuilder();

            string edition = OSInfo.Edition;
            string sp = OSInfo.ServicePack;

            sb.Append(OSInfo.Name);
            if (edition.Length > 0)
            {
                sb.Append(' ');
                sb.Append(edition);
            }
            if (sp.Length > 0)
            {
                sb.Append(' ');
                sb.Append(sp);
            }
            sb.Append(Environment.Is64BitOperatingSystem ? " 64-bit" : " 32-bit");
            return sb.ToString();
        }

        private string getWindowsInfoReg()
        {
            StringBuilder sb = new StringBuilder();

            string edition = OSInfo_Registry.Edition;
            string sp = OSInfo_Registry.ServicePack;

            sb.Append(OSInfo_Registry.Name);
            // Name info from registry already includes edition information, if appropriate
            // Edition info in registry is not formatted for display
#if false
            if (edition.Length > 0)
            {
                sb.Append(' ');
                sb.Append(edition);
            }
#endif
            if (sp.Length > 0)
            {
                sb.Append(' ');
                sb.Append(sp);
            }
            sb.Append(Environment.Is64BitOperatingSystem ? " 64-bit" : " 32-bit");
            return sb.ToString();
        }
        #endregion

        private void WriteLogHeader()
        {
            eidInit = Log.LogEntry(LogCat.Init, "Host environment info");
            Log.LogEntry(eidInit, LogCat.Init, "Data Path", mySettings.AppDataPath());
            Log.LogEntry(eidInit, LogCat.Init, "Log Filename", Log.InnerLog.Filename);
            Log.LogEntry(eidInit, LogCat.Init, "OS Version", Environment.OSVersion.ToString());
            if (OSInfo.RunningUnderWindows())
            {
                Log.LogEntry(eidInit, LogCat.Init, "Windows OS Description", getWindowsInfo());
                // Version compatibility settings can hide true OS version info, so also show what registry says.
                Log.LogEntry(eidInit, LogCat.Init, "OS Version via registry", OSInfo_Registry.VersionString);
                Log.LogEntry(eidInit, LogCat.Init, "Windows OS Description via registry", getWindowsInfoReg());
            }
            Log.LogEntry(eidInit, LogCat.Init, "MachineName", Environment.MachineName.ToString());
            Log.LogEntry(eidInit, LogCat.Init, "UserName", Environment.UserName.ToString()); 
            Log.LogEntry(eidInit, LogCat.Init, "Program", typeof(Program).Assembly.GetName().Name.ToString());
            Log.LogEntry(eidInit, LogCat.Init, "Program version", typeof(Program).Assembly.GetName().Version.ToString());
        }

        private void tsmiAbout_Click(object sender, EventArgs e)
        {
            frmAbout frm = new frmAbout();
            frm.ShowDialog();
        }

        private void cboPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnOpenPort.Enabled = (cboPort.SelectedIndex > 0);
            // first entry (0) is "Unknown"
            if (btnOpenPort.Enabled)
                tsslStatus.Text = "Click 'Open' button";
        }

        private void SetConnectionStatus(bool Connected)
        {
            btnSend.Enabled = Connected;
            grpReads.Enabled = Connected;
            grpWrites.Enabled = Connected;
            grpUser.Enabled = Connected;
            if (Connected)
            {
                grpWrites.Visible = true;
                grpReads.Visible = true;
                grpUser.Visible = true;
            }
            //Application.DoEvents(); // make groups visible
        }


        private void btnOpenPort_Click(object sender, EventArgs e)
        {
            SerialPort sp = Support_Serial.SelectPort(eidInit, Log, ports, cboPort.SelectedItem.ToString(), 115200, "Test");
            myController.AssignSerialPort(sp);
            bool Success = (myController.VerifyConnection(null) == OpResult.Success);
            tsslStatus.Text = Success ? "Actuator connection verified" : "Actuator not found";
            SetConnectionStatus(Success);
        }

        private void tsmiLog_Click(object sender, EventArgs e)
        {
            if (frmLogView == null)
            {
                frmLogView = new frmLogViewer();
                frmLogView.Manager = this;
                frmLogView.OpenFile(Log.InnerLog.Filename);
            }
            frmLogView.Show();
        }

        private void tsmiExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tsmiRefreshPortList_Click(object sender, EventArgs e)
        {
            RefreshPortList();
        }


        private void btnMoveUpper_Click(object sender, EventArgs e)
        {
            OpResult result = myController.MoveToUpperLimit();
            tsslStatus.Text = (result == OpResult.Success) ? "Moving to upper limit" : "Move failed";
        }

        private void btnQueryStatus_Click(object sender, EventArgs e)
        {
            ActStatus status = ActStatus.Faulted;
            OpResult result = myController.QueryStatus(out status);
            tsslStatus.Text = (result == OpResult.Success) ? "Status read" : "Query failed";
            txtQueryResult.Text = (result == OpResult.Success) ? Actuator.StatusDescription(status) : "Query failure. Check log.";
        }

        private void btnMoveLower_Click(object sender, EventArgs e)
        {
            OpResult result = myController.MoveToLowerLimit();
            tsslStatus.Text = (result == OpResult.Success) ? "Moving to lower limit" : "Move failed";
        }

        private void btnQueryError_Click(object sender, EventArgs e)
        {
            string Response = null;
            OpResult result = myController.QueryError(out Response);
            tsslStatus.Text = (result == OpResult.Success) ? "Error info read" : "Query failed";
            txtQueryResult.Text = (result == OpResult.Success) ? Response : "Query failure. Check log.";
        }
    }
}
