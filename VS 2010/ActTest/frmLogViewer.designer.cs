namespace LogViewer
{
    partial class frmLogViewer
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmLogViewer));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.tsmiFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiExit = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiView = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiClear = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiRefresh = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOptions = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiPropagate = new System.Windows.Forms.ToolStripMenuItem();
            this.autoExpandThresholdToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExpandAll = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExpandWarnings = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExpandErrors = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExpandFailures = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiExpandNever = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiBigIcons = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiShowTimestamps = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiFilter = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiCategory = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSeverity = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiShowAll = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiShowWarnings = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiShowErrors = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiShowFailures = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.imageListSmall = new System.Windows.Forms.ImageList(this.components);
            this.dlgOpenFile = new System.Windows.Forms.OpenFileDialog();
            this.imageListBig = new System.Windows.Forms.ImageList(this.components);
            this.tmrLogPolling = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiFile,
            this.tsmiView,
            this.tsmiOptions,
            this.tsmiFilter,
            this.tsmiHelp});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(703, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // tsmiFile
            // 
            this.tsmiFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiOpen,
            this.toolStripMenuItem1,
            this.tsmiExit});
            this.tsmiFile.Name = "tsmiFile";
            this.tsmiFile.Size = new System.Drawing.Size(37, 20);
            this.tsmiFile.Text = "&File";
            // 
            // tsmiOpen
            // 
            this.tsmiOpen.Name = "tsmiOpen";
            this.tsmiOpen.Size = new System.Drawing.Size(112, 22);
            this.tsmiOpen.Text = "&Open...";
            this.tsmiOpen.Click += new System.EventHandler(this.tsmiOpen_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(109, 6);
            // 
            // tsmiExit
            // 
            this.tsmiExit.Name = "tsmiExit";
            this.tsmiExit.Size = new System.Drawing.Size(112, 22);
            this.tsmiExit.Text = "E&xit";
            this.tsmiExit.Click += new System.EventHandler(this.tsmiExit_Click);
            // 
            // tsmiView
            // 
            this.tsmiView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiClear,
            this.tsmiRefresh});
            this.tsmiView.Name = "tsmiView";
            this.tsmiView.Size = new System.Drawing.Size(44, 20);
            this.tsmiView.Text = "&View";
            // 
            // tsmiClear
            // 
            this.tsmiClear.Name = "tsmiClear";
            this.tsmiClear.Size = new System.Drawing.Size(113, 22);
            this.tsmiClear.Text = "&Clear";
            this.tsmiClear.Click += new System.EventHandler(this.tsmiClear_Click);
            // 
            // tsmiRefresh
            // 
            this.tsmiRefresh.Name = "tsmiRefresh";
            this.tsmiRefresh.Size = new System.Drawing.Size(113, 22);
            this.tsmiRefresh.Text = "&Refresh";
            this.tsmiRefresh.Click += new System.EventHandler(this.tsmiRefresh_Click);
            // 
            // tsmiOptions
            // 
            this.tsmiOptions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiPropagate,
            this.autoExpandThresholdToolStripMenuItem,
            this.tsmiBigIcons,
            this.tsmiShowTimestamps});
            this.tsmiOptions.Name = "tsmiOptions";
            this.tsmiOptions.Size = new System.Drawing.Size(61, 20);
            this.tsmiOptions.Text = "&Options";
            // 
            // tsmiPropagate
            // 
            this.tsmiPropagate.AutoToolTip = true;
            this.tsmiPropagate.Checked = true;
            this.tsmiPropagate.CheckOnClick = true;
            this.tsmiPropagate.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsmiPropagate.Name = "tsmiPropagate";
            this.tsmiPropagate.Size = new System.Drawing.Size(201, 22);
            this.tsmiPropagate.Text = "&Propagate Severity";
            this.tsmiPropagate.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.tsmiPropagate.ToolTipText = "Propagate Severity to Ancestor Entries";
            this.tsmiPropagate.Click += new System.EventHandler(this.tsmiPropagate_Click);
            // 
            // autoExpandThresholdToolStripMenuItem
            // 
            this.autoExpandThresholdToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiExpandAll,
            this.tsmiExpandWarnings,
            this.tsmiExpandErrors,
            this.tsmiExpandFailures,
            this.tsmiExpandNever});
            this.autoExpandThresholdToolStripMenuItem.Name = "autoExpandThresholdToolStripMenuItem";
            this.autoExpandThresholdToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.autoExpandThresholdToolStripMenuItem.Text = "&Auto-Expand Threshold";
            // 
            // tsmiExpandAll
            // 
            this.tsmiExpandAll.Name = "tsmiExpandAll";
            this.tsmiExpandAll.Size = new System.Drawing.Size(246, 22);
            this.tsmiExpandAll.Tag = "0";
            this.tsmiExpandAll.Text = "Auto-expand &All";
            this.tsmiExpandAll.Click += new System.EventHandler(this.ExpansionChanged);
            // 
            // tsmiExpandWarnings
            // 
            this.tsmiExpandWarnings.Name = "tsmiExpandWarnings";
            this.tsmiExpandWarnings.Size = new System.Drawing.Size(246, 22);
            this.tsmiExpandWarnings.Tag = "1";
            this.tsmiExpandWarnings.Text = "Auto-expand &Warnings or Worse";
            this.tsmiExpandWarnings.Click += new System.EventHandler(this.ExpansionChanged);
            // 
            // tsmiExpandErrors
            // 
            this.tsmiExpandErrors.Name = "tsmiExpandErrors";
            this.tsmiExpandErrors.Size = new System.Drawing.Size(246, 22);
            this.tsmiExpandErrors.Tag = "2";
            this.tsmiExpandErrors.Text = "Auto-expand &Errors or Worse";
            this.tsmiExpandErrors.Click += new System.EventHandler(this.ExpansionChanged);
            // 
            // tsmiExpandFailures
            // 
            this.tsmiExpandFailures.Name = "tsmiExpandFailures";
            this.tsmiExpandFailures.Size = new System.Drawing.Size(246, 22);
            this.tsmiExpandFailures.Tag = "3";
            this.tsmiExpandFailures.Text = "Auto-expand &Failures Only";
            this.tsmiExpandFailures.Click += new System.EventHandler(this.ExpansionChanged);
            // 
            // tsmiExpandNever
            // 
            this.tsmiExpandNever.Name = "tsmiExpandNever";
            this.tsmiExpandNever.Size = new System.Drawing.Size(246, 22);
            this.tsmiExpandNever.Tag = "4";
            this.tsmiExpandNever.Text = "&No auto-expansion";
            this.tsmiExpandNever.Click += new System.EventHandler(this.ExpansionChanged);
            // 
            // tsmiBigIcons
            // 
            this.tsmiBigIcons.Name = "tsmiBigIcons";
            this.tsmiBigIcons.Size = new System.Drawing.Size(201, 22);
            this.tsmiBigIcons.Text = "&Big Icons";
            this.tsmiBigIcons.Click += new System.EventHandler(this.tsmiBigIcons_Click);
            // 
            // tsmiShowTimestamps
            // 
            this.tsmiShowTimestamps.Name = "tsmiShowTimestamps";
            this.tsmiShowTimestamps.Size = new System.Drawing.Size(201, 22);
            this.tsmiShowTimestamps.Text = "&Show Entry Timestamps";
            this.tsmiShowTimestamps.Click += new System.EventHandler(this.tsmiShowTimestamps_Click);
            // 
            // tsmiFilter
            // 
            this.tsmiFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiCategory,
            this.tsmiSeverity});
            this.tsmiFilter.Name = "tsmiFilter";
            this.tsmiFilter.Size = new System.Drawing.Size(45, 20);
            this.tsmiFilter.Text = "Fil&ter";
            // 
            // tsmiCategory
            // 
            this.tsmiCategory.Name = "tsmiCategory";
            this.tsmiCategory.Size = new System.Drawing.Size(147, 22);
            this.tsmiCategory.Text = "By &Category...";
            this.tsmiCategory.Click += new System.EventHandler(this.tsmiCategory_Click);
            // 
            // tsmiSeverity
            // 
            this.tsmiSeverity.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiShowAll,
            this.tsmiShowWarnings,
            this.tsmiShowErrors,
            this.tsmiShowFailures});
            this.tsmiSeverity.Name = "tsmiSeverity";
            this.tsmiSeverity.Size = new System.Drawing.Size(147, 22);
            this.tsmiSeverity.Text = "By &Severity";
            // 
            // tsmiShowAll
            // 
            this.tsmiShowAll.Name = "tsmiShowAll";
            this.tsmiShowAll.Size = new System.Drawing.Size(206, 22);
            this.tsmiShowAll.Tag = "0";
            this.tsmiShowAll.Text = "Show &All Entries";
            this.tsmiShowAll.Click += new System.EventHandler(this.SeverityChanged);
            // 
            // tsmiShowWarnings
            // 
            this.tsmiShowWarnings.Name = "tsmiShowWarnings";
            this.tsmiShowWarnings.Size = new System.Drawing.Size(206, 22);
            this.tsmiShowWarnings.Tag = "1";
            this.tsmiShowWarnings.Text = "Show &Warnings or Worse";
            this.tsmiShowWarnings.Click += new System.EventHandler(this.SeverityChanged);
            // 
            // tsmiShowErrors
            // 
            this.tsmiShowErrors.Name = "tsmiShowErrors";
            this.tsmiShowErrors.Size = new System.Drawing.Size(206, 22);
            this.tsmiShowErrors.Tag = "2";
            this.tsmiShowErrors.Text = "Show &Errors or Worse";
            this.tsmiShowErrors.Click += new System.EventHandler(this.SeverityChanged);
            // 
            // tsmiShowFailures
            // 
            this.tsmiShowFailures.Name = "tsmiShowFailures";
            this.tsmiShowFailures.Size = new System.Drawing.Size(206, 22);
            this.tsmiShowFailures.Tag = "3";
            this.tsmiShowFailures.Text = "Show &Failures Only";
            this.tsmiShowFailures.Click += new System.EventHandler(this.SeverityChanged);
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
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 504);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            this.statusStrip1.Size = new System.Drawing.Size(703, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tsslStatus
            // 
            this.tsslStatus.Name = "tsslStatus";
            this.tsslStatus.Size = new System.Drawing.Size(686, 17);
            this.tsslStatus.Spring = true;
            this.tsslStatus.Text = " ";
            this.tsslStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // treeView1
            // 
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.ImageIndex = 0;
            this.treeView1.ImageList = this.imageListSmall;
            this.treeView1.Location = new System.Drawing.Point(0, 24);
            this.treeView1.Name = "treeView1";
            this.treeView1.SelectedImageIndex = 0;
            this.treeView1.Size = new System.Drawing.Size(703, 480);
            this.treeView1.TabIndex = 2;
            this.treeView1.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView1_NodeMouseClick);
            // 
            // imageListSmall
            // 
            this.imageListSmall.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListSmall.ImageStream")));
            this.imageListSmall.TransparentColor = System.Drawing.Color.Fuchsia;
            this.imageListSmall.Images.SetKeyName(0, "small0.bmp");
            this.imageListSmall.Images.SetKeyName(1, "small1.bmp");
            this.imageListSmall.Images.SetKeyName(2, "small2.bmp");
            this.imageListSmall.Images.SetKeyName(3, "small3.bmp");
            // 
            // dlgOpenFile
            // 
            this.dlgOpenFile.DefaultExt = "log";
            this.dlgOpenFile.FileName = "*.log";
            this.dlgOpenFile.Filter = "Log files|*.log|CSV files|*.csv|All files|*.*";
            this.dlgOpenFile.Title = "Select Log File";
            // 
            // imageListBig
            // 
            this.imageListBig.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListBig.ImageStream")));
            this.imageListBig.TransparentColor = System.Drawing.Color.Fuchsia;
            this.imageListBig.Images.SetKeyName(0, "big0.bmp");
            this.imageListBig.Images.SetKeyName(1, "big1.bmp");
            this.imageListBig.Images.SetKeyName(2, "big2.bmp");
            this.imageListBig.Images.SetKeyName(3, "big3.bmp");
            // 
            // tmrLogPolling
            // 
            this.tmrLogPolling.Interval = 2000;
            this.tmrLogPolling.Tick += new System.EventHandler(this.tmrLogPolling_Tick);
            // 
            // frmLogViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(703, 526);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmLogViewer";
            this.Text = "Log Viewer";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmViewer_FormClosed);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsmiFile;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpen;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem tsmiExit;
        private System.Windows.Forms.ToolStripMenuItem tsmiHelp;
        private System.Windows.Forms.ToolStripMenuItem tsmiAbout;
        private System.Windows.Forms.ToolStripMenuItem tsmiFilter;
        private System.Windows.Forms.ToolStripMenuItem tsmiCategory;
        private System.Windows.Forms.ToolStripMenuItem tsmiSeverity;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.OpenFileDialog dlgOpenFile;
        private System.Windows.Forms.ImageList imageListSmall;
        private System.Windows.Forms.ImageList imageListBig;
        private System.Windows.Forms.ToolStripMenuItem tsmiShowAll;
        private System.Windows.Forms.ToolStripMenuItem tsmiShowWarnings;
        private System.Windows.Forms.ToolStripMenuItem tsmiShowErrors;
        private System.Windows.Forms.ToolStripMenuItem tsmiShowFailures;
        private System.Windows.Forms.ToolStripMenuItem tsmiOptions;
        private System.Windows.Forms.ToolStripMenuItem tsmiPropagate;
        private System.Windows.Forms.ToolStripMenuItem autoExpandThresholdToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tsmiExpandAll;
        private System.Windows.Forms.ToolStripMenuItem tsmiExpandWarnings;
        private System.Windows.Forms.ToolStripMenuItem tsmiExpandErrors;
        private System.Windows.Forms.ToolStripMenuItem tsmiExpandFailures;
        private System.Windows.Forms.ToolStripMenuItem tsmiExpandNever;
        private System.Windows.Forms.ToolStripMenuItem tsmiView;
        private System.Windows.Forms.ToolStripMenuItem tsmiClear;
        private System.Windows.Forms.ToolStripMenuItem tsmiRefresh;
        private System.Windows.Forms.ToolStripMenuItem tsmiBigIcons;
        private System.Windows.Forms.ToolStripMenuItem tsmiShowTimestamps;
        private System.Windows.Forms.Timer tmrLogPolling;
        private System.Windows.Forms.ToolStripStatusLabel tsslStatus;
    }
}

