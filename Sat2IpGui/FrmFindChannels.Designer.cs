namespace Sat2IpGui
{
    partial class FrmFindChannels
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
            this.cmbLNB = new System.Windows.Forms.ComboBox();
            this.cmbTransponder = new System.Windows.Forms.ComboBox();
            this.cbScanAll = new System.Windows.Forms.CheckBox();
            this.btnScan = new System.Windows.Forms.Button();
            this.txtTransponder = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtChannels = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cmbLNB
            // 
            this.cmbLNB.FormattingEnabled = true;
            this.cmbLNB.Location = new System.Drawing.Point(24, 22);
            this.cmbLNB.Name = "cmbLNB";
            this.cmbLNB.Size = new System.Drawing.Size(172, 21);
            this.cmbLNB.TabIndex = 0;
            this.cmbLNB.SelectedValueChanged += new System.EventHandler(this.cmbLNB_SelectedValueChanged);
            // 
            // cmbTransponder
            // 
            this.cmbTransponder.FormattingEnabled = true;
            this.cmbTransponder.Location = new System.Drawing.Point(214, 22);
            this.cmbTransponder.Name = "cmbTransponder";
            this.cmbTransponder.Size = new System.Drawing.Size(169, 21);
            this.cmbTransponder.TabIndex = 1;
            this.cmbTransponder.SelectedIndexChanged += new System.EventHandler(this.cmbTransponder_SelectedIndexChanged);
            // 
            // cbScanAll
            // 
            this.cbScanAll.AutoSize = true;
            this.cbScanAll.Location = new System.Drawing.Point(24, 61);
            this.cbScanAll.Name = "cbScanAll";
            this.cbScanAll.Size = new System.Drawing.Size(128, 17);
            this.cbScanAll.TabIndex = 2;
            this.cbScanAll.Text = "Scan all transponders";
            this.cbScanAll.UseVisualStyleBackColor = true;
            this.cbScanAll.CheckedChanged += new System.EventHandler(this.cbScanAll_CheckedChanged);
            // 
            // btnScan
            // 
            this.btnScan.Location = new System.Drawing.Point(34, 150);
            this.btnScan.Name = "btnScan";
            this.btnScan.Size = new System.Drawing.Size(75, 23);
            this.btnScan.TabIndex = 3;
            this.btnScan.Text = "Scan";
            this.btnScan.UseVisualStyleBackColor = true;
            this.btnScan.Click += new System.EventHandler(this.btnScan_ClickAsync);
            // 
            // txtTransponder
            // 
            this.txtTransponder.Enabled = false;
            this.txtTransponder.Location = new System.Drawing.Point(81, 90);
            this.txtTransponder.Name = "txtTransponder";
            this.txtTransponder.Size = new System.Drawing.Size(123, 20);
            this.txtTransponder.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 90);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Scanning:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(216, 90);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(84, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Channels found:";
            // 
            // txtChannels
            // 
            this.txtChannels.Enabled = false;
            this.txtChannels.Location = new System.Drawing.Point(303, 90);
            this.txtChannels.Name = "txtChannels";
            this.txtChannels.Size = new System.Drawing.Size(80, 20);
            this.txtChannels.TabIndex = 7;
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(331, 150);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 8;
            this.button1.Text = "Close";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // FrmFindChannels
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(446, 192);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.txtChannels);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtTransponder);
            this.Controls.Add(this.btnScan);
            this.Controls.Add(this.cbScanAll);
            this.Controls.Add(this.cmbTransponder);
            this.Controls.Add(this.cmbLNB);
            this.Name = "FrmFindChannels";
            this.Text = "FrmFindChannels";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbLNB;
        private System.Windows.Forms.ComboBox cmbTransponder;
        private System.Windows.Forms.CheckBox cbScanAll;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.TextBox txtTransponder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtChannels;
        private System.Windows.Forms.Button button1;
    }
}