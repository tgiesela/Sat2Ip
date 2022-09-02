namespace Sat2IpGui
{
    partial class FrmFindChannelsDVBC
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
            this.cmbProvider = new System.Windows.Forms.ComboBox();
            this.cmbFrequency = new System.Windows.Forms.ComboBox();
            this.cbScanAll = new System.Windows.Forms.CheckBox();
            this.btnScan = new System.Windows.Forms.Button();
            this.txtTransponder = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtChannels = new System.Windows.Forms.TextBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtNetworkID = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // cmbProvider
            // 
            this.cmbProvider.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProvider.FormattingEnabled = true;
            this.cmbProvider.Location = new System.Drawing.Point(28, 25);
            this.cmbProvider.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbProvider.Name = "cmbProvider";
            this.cmbProvider.Size = new System.Drawing.Size(200, 23);
            this.cmbProvider.TabIndex = 0;
            this.cmbProvider.SelectedIndexChanged += new System.EventHandler(this.cmbProvider_SelectedValueChanged);
            this.cmbProvider.SelectedValueChanged += new System.EventHandler(this.cmbProvider_SelectedValueChanged);
            // 
            // cmbFrequency
            // 
            this.cmbFrequency.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFrequency.FormattingEnabled = true;
            this.cmbFrequency.Location = new System.Drawing.Point(250, 25);
            this.cmbFrequency.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbFrequency.Name = "cmbFrequency";
            this.cmbFrequency.Size = new System.Drawing.Size(196, 23);
            this.cmbFrequency.TabIndex = 1;
            this.cmbFrequency.SelectedIndexChanged += new System.EventHandler(this.cmbFrequency_SelectedIndexChanged);
            // 
            // cbScanAll
            // 
            this.cbScanAll.AutoSize = true;
            this.cbScanAll.Location = new System.Drawing.Point(28, 70);
            this.cbScanAll.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbScanAll.Name = "cbScanAll";
            this.cbScanAll.Size = new System.Drawing.Size(138, 19);
            this.cbScanAll.TabIndex = 2;
            this.cbScanAll.Text = "Scan all transponders";
            this.cbScanAll.UseVisualStyleBackColor = true;
            this.cbScanAll.CheckedChanged += new System.EventHandler(this.cbScanAll_CheckedChanged);
            // 
            // btnScan
            // 
            this.btnScan.Location = new System.Drawing.Point(40, 173);
            this.btnScan.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnScan.Name = "btnScan";
            this.btnScan.Size = new System.Drawing.Size(88, 27);
            this.btnScan.TabIndex = 3;
            this.btnScan.Text = "Scan";
            this.btnScan.UseVisualStyleBackColor = true;
            this.btnScan.Click += new System.EventHandler(this.btnScan_ClickAsync);
            // 
            // txtTransponder
            // 
            this.txtTransponder.Enabled = false;
            this.txtTransponder.Location = new System.Drawing.Point(94, 104);
            this.txtTransponder.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtTransponder.Name = "txtTransponder";
            this.txtTransponder.Size = new System.Drawing.Size(143, 23);
            this.txtTransponder.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 108);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 15);
            this.label1.TabIndex = 5;
            this.label1.Text = "Scanning:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(252, 108);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(94, 15);
            this.label2.TabIndex = 6;
            this.label2.Text = "Channels found:";
            // 
            // txtChannels
            // 
            this.txtChannels.Enabled = false;
            this.txtChannels.Location = new System.Drawing.Point(354, 104);
            this.txtChannels.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtChannels.Name = "txtChannels";
            this.txtChannels.Size = new System.Drawing.Size(93, 23);
            this.txtChannels.TabIndex = 7;
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnClose.Location = new System.Drawing.Point(386, 173);
            this.btnClose.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(88, 27);
            this.btnClose.TabIndex = 8;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // btnStop
            // 
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(136, 173);
            this.btnStop.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(88, 27);
            this.btnStop.TabIndex = 10;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(28, 9);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(51, 15);
            this.label3.TabIndex = 12;
            this.label3.Text = "Provider";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(23, 144);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(69, 15);
            this.label4.TabIndex = 14;
            this.label4.Text = "Network ID:";
            // 
            // txtNetworkID
            // 
            this.txtNetworkID.Location = new System.Drawing.Point(94, 140);
            this.txtNetworkID.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtNetworkID.Name = "txtNetworkID";
            this.txtNetworkID.Size = new System.Drawing.Size(93, 23);
            this.txtNetworkID.TabIndex = 15;
            // 
            // FrmFindChannelsDVBC
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 222);
            this.Controls.Add(this.txtNetworkID);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.txtChannels);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtTransponder);
            this.Controls.Add(this.btnScan);
            this.Controls.Add(this.cbScanAll);
            this.Controls.Add(this.cmbFrequency);
            this.Controls.Add(this.cmbProvider);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "FrmFindChannelsDVBC";
            this.Text = "FrmFindChannels";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbProvider;
        private System.Windows.Forms.ComboBox cmbFrequency;
        private System.Windows.Forms.CheckBox cbScanAll;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.TextBox txtTransponder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtChannels;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtNetworkID;
    }
}