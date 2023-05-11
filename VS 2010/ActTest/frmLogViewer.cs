using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;
using TreeLog;
using ActTest;

namespace LogViewer
{
    public partial class frmLogViewer : Form
    {
        public LogReader myReader;
        public frmMain Manager = null;

        public frmLogViewer()
        {
            InitializeComponent();
            myReader = new LogReader();
            myReader.tv = treeView1;
            myReader.myOwner = this;    // so myReader can call this.SetStatusMessage()

            // make sure GUI reflects default options
            UpdateVisibilityMenuCheckMarks();
            UpdateExpansionMenuCheckMarks();
        }

        private void tsmiExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void tsmiAbout_Click(object sender, EventArgs e)
        {
            new frmAbout().ShowDialog();
        }

        private void tsmiOpen_Click(object sender, EventArgs e)
        {
            dlgOpenFile.InitialDirectory = Application.StartupPath;
            if (dlgOpenFile.ShowDialog() == DialogResult.OK)
            {
                OpenFile(dlgOpenFile.FileName);
            }
        }

        public void OpenFile(string Filename)
        {
            myReader.tv.Nodes.Clear();
            myReader.Entries.Clear();
            myReader.OpenLogFile(Filename);
            EnableLogPolling(null);
        }

        public void SetStatusMessage(string msg)
        {
            tsslStatus.Text = msg;
            Application.DoEvents();
        }

        private void EnableLogPolling(string Msg)
        {
            if (Msg != null)
                tsslStatus.Text = Msg;
            tmrLogPolling.Enabled = true;
        }

        private void DisableLogPolling(string Msg)
        {
            if (Msg != null)
                tsslStatus.Text = Msg;
            tmrLogPolling.Enabled = false; // disable polling until next refresh
        }

        private void UpdateVisibilityMenuCheckMarks()
        {
            tsmiShowAll.Checked = (myReader.VisibleSeverityThreshold == SeverityCode.Info);
            tsmiShowWarnings.Checked = (myReader.VisibleSeverityThreshold == SeverityCode.Warning);
            tsmiShowErrors.Checked = (myReader.VisibleSeverityThreshold == SeverityCode.Error);
            tsmiShowFailures.Checked = (myReader.VisibleSeverityThreshold == SeverityCode.Failure);
        }
        
        private void SeverityChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;
            if (tsmi != null)
            {
                myReader.VisibleSeverityThreshold = (SeverityCode)Convert.ToInt32(tsmi.Tag);
                myReader.RebuildTree();

                UpdateVisibilityMenuCheckMarks();
            }
        }

        private void UpdateExpansionMenuCheckMarks()
        {
            tsmiExpandAll.Checked = (myReader.AutoExpandThreshold == 0);
            tsmiExpandWarnings.Checked = (myReader.AutoExpandThreshold == 1);
            tsmiExpandErrors.Checked = (myReader.AutoExpandThreshold == 2);
            tsmiExpandFailures.Checked = (myReader.AutoExpandThreshold == 3);
            tsmiExpandNever.Checked = (myReader.AutoExpandThreshold == 4);
        }

        private void ExpansionChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;
            if (tsmi != null)
            {
                myReader.AutoExpandThreshold = Convert.ToInt32(tsmi.Tag);
                myReader.RebuildTree();

                UpdateExpansionMenuCheckMarks();
            }
        }

        private void tsmiPropagate_Click(object sender, EventArgs e)
        {
            myReader.UsePropagatedSeverity = !myReader.UsePropagatedSeverity;
            myReader.RebuildTree();
        }

        private void tsmiRefresh_Click(object sender, EventArgs e)
        {
            myReader.GetNewData();
            EnableLogPolling("");
            //myReader.RebuildTree();
            // Rebuild will collapse all expanded nodes
        }

        private void tsmiClear_Click(object sender, EventArgs e)
        {
            myReader.Entries.Clear();
            myReader.RebuildTree();
        }

        private void tsmiBigIcons_Click(object sender, EventArgs e)
        {
            tsmiBigIcons.Checked = !tsmiBigIcons.Checked;
            Font newFont;
            Size mySize = this.Size;
            // Remember current size, since changing font size has a side effect of changing form size
            
            if (tsmiBigIcons.Checked)
            {
                newFont = new Font(this.Font.FontFamily, 16);
                treeView1.ImageList = imageListBig;
            }
            else
            {
                newFont = new Font(this.Font.FontFamily, 9);
                treeView1.ImageList = imageListSmall;
            }

            menuStrip1.Font = newFont;
            this.Font = newFont;
            this.Size = mySize; // restore original form size
        }

        private void tsmiShowTimestamps_Click(object sender, EventArgs e)
        {
            tsmiShowTimestamps.Checked = !tsmiShowTimestamps.Checked;

            myReader.ShowTimeStamps = tsmiShowTimestamps.Checked;
            myReader.RebuildTree();
        }

        private void frmViewer_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Manager != null)
                Manager.frmLogView = null;

            SetStatusMessage("Closing...");
            treeView1.Nodes.Clear();
            if (myReader != null)
            {
                myReader.Dispose();
                myReader = null;
            }
        }

        private void tsmiCategory_Click(object sender, EventArgs e)
        {
            frmCategories frm = new frmCategories();
            frm.Reader = myReader;
            System.Windows.Forms.DialogResult result = frm.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                myReader.RebuildTree();
        }

        private void tmrLogPolling_Tick(object sender, EventArgs e)
        {
            if (myReader != null && myReader.NewDataIsAvailable())
                DisableLogPolling("New data is available. Click on View/Refresh to see it.");
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right && e.Node != null)
            {
                // TODO: replace with entry viewing form
                LogEntry entry = e.Node.Tag as LogEntry;
                if (entry != null)
                {
                    string msg = LogEntry.FirstNlines(entry.Msg2, 20);    // Only truncated if needed
                    MessageBox.Show(msg);
                }
            }
        }
    }
}
