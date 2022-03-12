namespace Sat2IpGui
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
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.myVlcControl = new Vlc.DotNet.Forms.VlcControl();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.serverToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.satelliteSetupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findChannelsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lbChannels = new System.Windows.Forms.ListBox();
            this.cbRadio = new System.Windows.Forms.CheckBox();
            this.cbTV = new System.Windows.Forms.CheckBox();
            this.cbData = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.myVlcControl)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnPlay
            // 
            this.btnPlay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnPlay.Location = new System.Drawing.Point(12, 342);
            this.btnPlay.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(72, 24);
            this.btnPlay.TabIndex = 1;
            this.btnPlay.Text = "Play";
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // btnStop
            // 
            this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnStop.Location = new System.Drawing.Point(91, 342);
            this.btnStop.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(72, 24);
            this.btnStop.TabIndex = 2;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // myVlcControl
            // 
            this.myVlcControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.myVlcControl.BackColor = System.Drawing.Color.Black;
            this.myVlcControl.Location = new System.Drawing.Point(14, 29);
            this.myVlcControl.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.myVlcControl.Name = "myVlcControl";
            this.myVlcControl.Size = new System.Drawing.Size(457, 306);
            this.myVlcControl.Spu = -1;
            this.myVlcControl.TabIndex = 3;
            this.myVlcControl.Text = "vlcControl1";
            this.myVlcControl.VlcLibDirectory = null;
            this.myVlcControl.VlcMediaplayerOptions = null;
            this.myVlcControl.VlcLibDirectoryNeeded += new System.EventHandler<Vlc.DotNet.Forms.VlcLibDirectoryNeededEventArgs>(this.myVlcControl_VlcLibDirectoryNeeded);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(813, 24);
            this.menuStrip1.TabIndex = 5;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.serverToolStripMenuItem,
            this.satelliteSetupToolStripMenuItem,
            this.findChannelsToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // serverToolStripMenuItem
            // 
            this.serverToolStripMenuItem.Name = "serverToolStripMenuItem";
            this.serverToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.serverToolStripMenuItem.Text = "Server...";
            this.serverToolStripMenuItem.Click += new System.EventHandler(this.ServerToolStripMenuItem_Click);
            // 
            // satelliteSetupToolStripMenuItem
            // 
            this.satelliteSetupToolStripMenuItem.Name = "satelliteSetupToolStripMenuItem";
            this.satelliteSetupToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.satelliteSetupToolStripMenuItem.Text = "Satellite setup...";
            this.satelliteSetupToolStripMenuItem.Click += new System.EventHandler(this.satelliteSetupToolStripMenuItem_Click);
            // 
            // findChannelsToolStripMenuItem
            // 
            this.findChannelsToolStripMenuItem.Name = "findChannelsToolStripMenuItem";
            this.findChannelsToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.findChannelsToolStripMenuItem.Text = "Find channels...";
            this.findChannelsToolStripMenuItem.Click += new System.EventHandler(this.findChannelsToolStripMenuItem_Click);
            // 
            // lbChannels
            // 
            this.lbChannels.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbChannels.FormattingEnabled = true;
            this.lbChannels.ItemHeight = 15;
            this.lbChannels.Location = new System.Drawing.Point(500, 77);
            this.lbChannels.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.lbChannels.Name = "lbChannels";
            this.lbChannels.Size = new System.Drawing.Size(285, 259);
            this.lbChannels.TabIndex = 6;
            this.lbChannels.SelectedIndexChanged += new System.EventHandler(this.lbChannels_SelectedIndexChanged);
            this.lbChannels.DoubleClick += new System.EventHandler(this.lbChannels_DoubleClick);
            // 
            // cbRadio
            // 
            this.cbRadio.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbRadio.AutoSize = true;
            this.cbRadio.Location = new System.Drawing.Point(502, 37);
            this.cbRadio.Name = "cbRadio";
            this.cbRadio.Size = new System.Drawing.Size(56, 19);
            this.cbRadio.TabIndex = 7;
            this.cbRadio.Text = "Radio";
            this.cbRadio.UseVisualStyleBackColor = true;
            this.cbRadio.CheckedChanged += new System.EventHandler(this.cbRadio_CheckedChanged);
            // 
            // cbTV
            // 
            this.cbTV.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbTV.AutoSize = true;
            this.cbTV.Location = new System.Drawing.Point(564, 37);
            this.cbTV.Name = "cbTV";
            this.cbTV.Size = new System.Drawing.Size(39, 19);
            this.cbTV.TabIndex = 8;
            this.cbTV.Text = "TV";
            this.cbTV.UseVisualStyleBackColor = true;
            this.cbTV.CheckedChanged += new System.EventHandler(this.cbTV_CheckedChanged);
            // 
            // cbData
            // 
            this.cbData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbData.AutoSize = true;
            this.cbData.Location = new System.Drawing.Point(609, 37);
            this.cbData.Name = "cbData";
            this.cbData.Size = new System.Drawing.Size(50, 19);
            this.cbData.TabIndex = 9;
            this.cbData.Text = "Data";
            this.cbData.UseVisualStyleBackColor = true;
            this.cbData.CheckedChanged += new System.EventHandler(this.cbData_CheckedChanged);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(813, 380);
            this.Controls.Add(this.cbData);
            this.Controls.Add(this.cbTV);
            this.Controls.Add(this.cbRadio);
            this.Controls.Add(this.lbChannels);
            this.Controls.Add(this.myVlcControl);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "frmMain";
            this.Text = "Sat2Ip GUI";
            ((System.ComponentModel.ISupportInitialize)(this.myVlcControl)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Button btnStop;
        private Vlc.DotNet.Forms.VlcControl myVlcControl;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem serverToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem satelliteSetupToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem findChannelsToolStripMenuItem;
        private System.Windows.Forms.ListBox lbChannels;
        private System.Windows.Forms.CheckBox cbRadio;
        private System.Windows.Forms.CheckBox cbTV;
        private System.Windows.Forms.CheckBox cbData;
    }
}

