namespace ActTest
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.tsmiFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiRefreshPortList = new System.Windows.Forms.ToolStripMenuItem();
            this.tssSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiExit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiLog = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.grpUser = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtRawResult = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtCommand = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.grpCOM = new System.Windows.Forms.GroupBox();
            this.btnOpenPort = new System.Windows.Forms.Button();
            this.cboPort = new System.Windows.Forms.ComboBox();
            this.grpReads = new System.Windows.Forms.GroupBox();
            this.btnQueryError = new System.Windows.Forms.Button();
            this.btnQueryStatus = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.txtQueryResult = new System.Windows.Forms.TextBox();
            this.grpWrites = new System.Windows.Forms.GroupBox();
            this.btnMoveLower = new System.Windows.Forms.Button();
            this.btnMoveUpper = new System.Windows.Forms.Button();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.grpUser.SuspendLayout();
            this.grpCOM.SuspendLayout();
            this.grpReads.SuspendLayout();
            this.grpWrites.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 543);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(255, 22);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tsslStatus
            // 
            this.tsslStatus.Name = "tsslStatus";
            this.tsslStatus.Size = new System.Drawing.Size(240, 17);
            this.tsslStatus.Spring = true;
            this.tsslStatus.Text = "Select COM port";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiFile,
            this.tsmiLog,
            this.tsmiHelp});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(255, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // tsmiFile
            // 
            this.tsmiFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiRefreshPortList,
            this.tssSep1,
            this.tsmiExit});
            this.tsmiFile.Name = "tsmiFile";
            this.tsmiFile.Size = new System.Drawing.Size(37, 20);
            this.tsmiFile.Text = "&File";
            // 
            // tsmiRefreshPortList
            // 
            this.tsmiRefreshPortList.Name = "tsmiRefreshPortList";
            this.tsmiRefreshPortList.Size = new System.Drawing.Size(187, 22);
            this.tsmiRefreshPortList.Text = "&Refresh COM port list";
            this.tsmiRefreshPortList.Click += new System.EventHandler(this.tsmiRefreshPortList_Click);
            // 
            // tssSep1
            // 
            this.tssSep1.Name = "tssSep1";
            this.tssSep1.Size = new System.Drawing.Size(184, 6);
            // 
            // tsmiExit
            // 
            this.tsmiExit.Name = "tsmiExit";
            this.tsmiExit.Size = new System.Drawing.Size(187, 22);
            this.tsmiExit.Text = "E&xit";
            this.tsmiExit.Click += new System.EventHandler(this.tsmiExit_Click);
            // 
            // tsmiLog
            // 
            this.tsmiLog.Name = "tsmiLog";
            this.tsmiLog.Size = new System.Drawing.Size(39, 20);
            this.tsmiLog.Text = "&Log";
            this.tsmiLog.Click += new System.EventHandler(this.tsmiLog_Click);
            // 
            // tsmiHelp
            // 
            this.tsmiHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiAbout});
            this.tsmiHelp.Name = "tsmiHelp";
            this.tsmiHelp.Size = new System.Drawing.Size(44, 20);
            this.tsmiHelp.Text = "&Help";
            // 
            // tsmiAbout
            // 
            this.tsmiAbout.Name = "tsmiAbout";
            this.tsmiAbout.Size = new System.Drawing.Size(116, 22);
            this.tsmiAbout.Text = "&About...";
            this.tsmiAbout.Click += new System.EventHandler(this.tsmiAbout_Click);
            // 
            // grpUser
            // 
            this.grpUser.Controls.Add(this.label2);
            this.grpUser.Controls.Add(this.txtRawResult);
            this.grpUser.Controls.Add(this.label1);
            this.grpUser.Controls.Add(this.txtCommand);
            this.grpUser.Controls.Add(this.btnSend);
            this.grpUser.Location = new System.Drawing.Point(12, 358);
            this.grpUser.Name = "grpUser";
            this.grpUser.Size = new System.Drawing.Size(232, 182);
            this.grpUser.TabIndex = 2;
            this.grpUser.TabStop = false;
            this.grpUser.Text = "User Commands";
            this.grpUser.Visible = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(4, 74);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Response Value";
            // 
            // txtRawResult
            // 
            this.txtRawResult.Location = new System.Drawing.Point(7, 90);
            this.txtRawResult.Multiline = true;
            this.txtRawResult.Name = "txtRawResult";
            this.txtRawResult.ReadOnly = true;
            this.txtRawResult.Size = new System.Drawing.Size(212, 86);
            this.txtRawResult.TabIndex = 9;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Command String";
            // 
            // txtCommand
            // 
            this.txtCommand.Location = new System.Drawing.Point(6, 39);
            this.txtCommand.Name = "txtCommand";
            this.txtCommand.Size = new System.Drawing.Size(124, 20);
            this.txtCommand.TabIndex = 7;
            // 
            // btnSend
            // 
            this.btnSend.Enabled = false;
            this.btnSend.Location = new System.Drawing.Point(149, 38);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(70, 23);
            this.btnSend.TabIndex = 6;
            this.btnSend.Text = "&Send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // grpCOM
            // 
            this.grpCOM.Controls.Add(this.btnOpenPort);
            this.grpCOM.Controls.Add(this.cboPort);
            this.grpCOM.Location = new System.Drawing.Point(12, 27);
            this.grpCOM.Name = "grpCOM";
            this.grpCOM.Size = new System.Drawing.Size(232, 53);
            this.grpCOM.TabIndex = 3;
            this.grpCOM.TabStop = false;
            this.grpCOM.Text = "COM Port";
            // 
            // btnOpenPort
            // 
            this.btnOpenPort.Enabled = false;
            this.btnOpenPort.Location = new System.Drawing.Point(149, 16);
            this.btnOpenPort.Name = "btnOpenPort";
            this.btnOpenPort.Size = new System.Drawing.Size(70, 25);
            this.btnOpenPort.TabIndex = 1;
            this.btnOpenPort.Text = "&Open";
            this.btnOpenPort.UseVisualStyleBackColor = true;
            this.btnOpenPort.Click += new System.EventHandler(this.btnOpenPort_Click);
            // 
            // cboPort
            // 
            this.cboPort.FormattingEnabled = true;
            this.cboPort.Location = new System.Drawing.Point(6, 19);
            this.cboPort.Name = "cboPort";
            this.cboPort.Size = new System.Drawing.Size(121, 21);
            this.cboPort.TabIndex = 0;
            this.cboPort.SelectedIndexChanged += new System.EventHandler(this.cboPort_SelectedIndexChanged);
            // 
            // grpReads
            // 
            this.grpReads.Controls.Add(this.btnQueryError);
            this.grpReads.Controls.Add(this.btnQueryStatus);
            this.grpReads.Controls.Add(this.label3);
            this.grpReads.Controls.Add(this.txtQueryResult);
            this.grpReads.Enabled = false;
            this.grpReads.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.grpReads.Location = new System.Drawing.Point(12, 86);
            this.grpReads.Name = "grpReads";
            this.grpReads.Size = new System.Drawing.Size(232, 150);
            this.grpReads.TabIndex = 4;
            this.grpReads.TabStop = false;
            this.grpReads.Text = "Status Queries";
            this.grpReads.Visible = false;
            // 
            // btnQueryError
            // 
            this.btnQueryError.Location = new System.Drawing.Point(127, 19);
            this.btnQueryError.Name = "btnQueryError";
            this.btnQueryError.Size = new System.Drawing.Size(92, 23);
            this.btnQueryError.TabIndex = 12;
            this.btnQueryError.Tag = "1";
            this.btnQueryError.Text = "Query Last Error";
            this.btnQueryError.UseVisualStyleBackColor = true;
            this.btnQueryError.Click += new System.EventHandler(this.btnQueryError_Click);
            // 
            // btnQueryStatus
            // 
            this.btnQueryStatus.Location = new System.Drawing.Point(7, 19);
            this.btnQueryStatus.Name = "btnQueryStatus";
            this.btnQueryStatus.Size = new System.Drawing.Size(92, 23);
            this.btnQueryStatus.TabIndex = 11;
            this.btnQueryStatus.Tag = "1";
            this.btnQueryStatus.Text = "Query Status";
            this.btnQueryStatus.UseVisualStyleBackColor = true;
            this.btnQueryStatus.Click += new System.EventHandler(this.btnQueryStatus_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 57);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(132, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Decoded Response Value";
            // 
            // txtQueryResult
            // 
            this.txtQueryResult.Location = new System.Drawing.Point(9, 73);
            this.txtQueryResult.Multiline = true;
            this.txtQueryResult.Name = "txtQueryResult";
            this.txtQueryResult.ReadOnly = true;
            this.txtQueryResult.Size = new System.Drawing.Size(212, 71);
            this.txtQueryResult.TabIndex = 9;
            // 
            // grpWrites
            // 
            this.grpWrites.Controls.Add(this.btnMoveLower);
            this.grpWrites.Controls.Add(this.btnMoveUpper);
            this.grpWrites.Enabled = false;
            this.grpWrites.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.grpWrites.Location = new System.Drawing.Point(12, 242);
            this.grpWrites.Name = "grpWrites";
            this.grpWrites.Size = new System.Drawing.Size(232, 110);
            this.grpWrites.TabIndex = 5;
            this.grpWrites.TabStop = false;
            this.grpWrites.Text = "Position Setting";
            this.grpWrites.Visible = false;
            // 
            // btnMoveLower
            // 
            this.btnMoveLower.Location = new System.Drawing.Point(51, 65);
            this.btnMoveLower.Name = "btnMoveLower";
            this.btnMoveLower.Size = new System.Drawing.Size(129, 23);
            this.btnMoveLower.TabIndex = 9;
            this.btnMoveLower.Tag = "16";
            this.btnMoveLower.Text = "Lower Limit";
            this.btnMoveLower.UseVisualStyleBackColor = true;
            this.btnMoveLower.Click += new System.EventHandler(this.btnMoveLower_Click);
            // 
            // btnMoveUpper
            // 
            this.btnMoveUpper.Location = new System.Drawing.Point(51, 19);
            this.btnMoveUpper.Name = "btnMoveUpper";
            this.btnMoveUpper.Size = new System.Drawing.Size(129, 23);
            this.btnMoveUpper.TabIndex = 6;
            this.btnMoveUpper.Text = "Upper Limit";
            this.btnMoveUpper.UseVisualStyleBackColor = true;
            this.btnMoveUpper.Click += new System.EventHandler(this.btnMoveUpper_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(255, 565);
            this.Controls.Add(this.grpWrites);
            this.Controls.Add(this.grpReads);
            this.Controls.Add(this.grpCOM);
            this.Controls.Add(this.grpUser);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmMain";
            this.Text = "ActTest GUI";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.grpUser.ResumeLayout(false);
            this.grpUser.PerformLayout();
            this.grpCOM.ResumeLayout(false);
            this.grpReads.ResumeLayout(false);
            this.grpReads.PerformLayout();
            this.grpWrites.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsmiFile;
        private System.Windows.Forms.GroupBox grpUser;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtRawResult;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox grpCOM;
        private System.Windows.Forms.ToolStripMenuItem tsmiLog;
        private System.Windows.Forms.ToolStripMenuItem tsmiHelp;
        private System.Windows.Forms.ToolStripMenuItem tsmiAbout;
        private System.Windows.Forms.Button btnOpenPort;
        private System.Windows.Forms.ComboBox cboPort;
        private System.Windows.Forms.ToolStripStatusLabel tsslStatus;
        private System.Windows.Forms.ToolStripSeparator tssSep1;
        private System.Windows.Forms.ToolStripMenuItem tsmiExit;
        private System.Windows.Forms.GroupBox grpReads;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtQueryResult;
        private System.Windows.Forms.ToolStripMenuItem tsmiRefreshPortList;
        private System.Windows.Forms.Button btnQueryStatus;
        private System.Windows.Forms.GroupBox grpWrites;
        private System.Windows.Forms.Button btnMoveUpper;
        private System.Windows.Forms.Button btnMoveLower;
        private System.Windows.Forms.TextBox txtCommand;
        private System.Windows.Forms.Button btnQueryError;
    }
}

