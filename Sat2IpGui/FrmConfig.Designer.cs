namespace Sat2IpGui
{
    partial class FrmConfig
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
            this.cbLNB1 = new System.Windows.Forms.CheckBox();
            this.cbLNB2 = new System.Windows.Forms.CheckBox();
            this.cbLNB3 = new System.Windows.Forms.CheckBox();
            this.cbLNB4 = new System.Windows.Forms.CheckBox();
            this.cmbSatellites1 = new System.Windows.Forms.ComboBox();
            this.cmbSatellites4 = new System.Windows.Forms.ComboBox();
            this.cmbSatellites3 = new System.Windows.Forms.ComboBox();
            this.cmbSatellites2 = new System.Windows.Forms.ComboBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cbLNB1
            // 
            this.cbLNB1.AutoSize = true;
            this.cbLNB1.Location = new System.Drawing.Point(27, 32);
            this.cbLNB1.Name = "cbLNB1";
            this.cbLNB1.Size = new System.Drawing.Size(56, 17);
            this.cbLNB1.TabIndex = 0;
            this.cbLNB1.Text = "LNB 1";
            this.cbLNB1.UseVisualStyleBackColor = true;
            this.cbLNB1.CheckedChanged += new System.EventHandler(this.cbLNB1_CheckedChanged);
            // 
            // cbLNB2
            // 
            this.cbLNB2.AutoSize = true;
            this.cbLNB2.Location = new System.Drawing.Point(27, 55);
            this.cbLNB2.Name = "cbLNB2";
            this.cbLNB2.Size = new System.Drawing.Size(56, 17);
            this.cbLNB2.TabIndex = 1;
            this.cbLNB2.Text = "LNB 2";
            this.cbLNB2.UseVisualStyleBackColor = true;
            this.cbLNB2.CheckedChanged += new System.EventHandler(this.cbLNB2_CheckedChanged);
            // 
            // cbLNB3
            // 
            this.cbLNB3.AutoSize = true;
            this.cbLNB3.Location = new System.Drawing.Point(27, 78);
            this.cbLNB3.Name = "cbLNB3";
            this.cbLNB3.Size = new System.Drawing.Size(56, 17);
            this.cbLNB3.TabIndex = 2;
            this.cbLNB3.Text = "LNB 3";
            this.cbLNB3.UseVisualStyleBackColor = true;
            this.cbLNB3.CheckedChanged += new System.EventHandler(this.cbLNB3_CheckedChanged);
            // 
            // cbLNB4
            // 
            this.cbLNB4.AutoSize = true;
            this.cbLNB4.Location = new System.Drawing.Point(27, 101);
            this.cbLNB4.Name = "cbLNB4";
            this.cbLNB4.Size = new System.Drawing.Size(56, 17);
            this.cbLNB4.TabIndex = 3;
            this.cbLNB4.Text = "LNB 4";
            this.cbLNB4.UseVisualStyleBackColor = true;
            this.cbLNB4.CheckedChanged += new System.EventHandler(this.cbLNB4_CheckedChanged);
            // 
            // cmbSatellites1
            // 
            this.cmbSatellites1.FormattingEnabled = true;
            this.cmbSatellites1.Location = new System.Drawing.Point(89, 30);
            this.cmbSatellites1.Name = "cmbSatellites1";
            this.cmbSatellites1.Size = new System.Drawing.Size(228, 21);
            this.cmbSatellites1.TabIndex = 4;
            // 
            // cmbSatellites4
            // 
            this.cmbSatellites4.FormattingEnabled = true;
            this.cmbSatellites4.Location = new System.Drawing.Point(89, 99);
            this.cmbSatellites4.Name = "cmbSatellites4";
            this.cmbSatellites4.Size = new System.Drawing.Size(228, 21);
            this.cmbSatellites4.TabIndex = 5;
            // 
            // cmbSatellites3
            // 
            this.cmbSatellites3.FormattingEnabled = true;
            this.cmbSatellites3.Location = new System.Drawing.Point(89, 76);
            this.cmbSatellites3.Name = "cmbSatellites3";
            this.cmbSatellites3.Size = new System.Drawing.Size(228, 21);
            this.cmbSatellites3.TabIndex = 6;
            // 
            // cmbSatellites2
            // 
            this.cmbSatellites2.FormattingEnabled = true;
            this.cmbSatellites2.Location = new System.Drawing.Point(89, 53);
            this.cmbSatellites2.Name = "cmbSatellites2";
            this.cmbSatellites2.Size = new System.Drawing.Size(228, 21);
            this.cmbSatellites2.TabIndex = 7;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(315, 304);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(396, 304);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 9;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // FrmConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(483, 330);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.cmbSatellites2);
            this.Controls.Add(this.cmbSatellites3);
            this.Controls.Add(this.cmbSatellites4);
            this.Controls.Add(this.cmbSatellites1);
            this.Controls.Add(this.cbLNB4);
            this.Controls.Add(this.cbLNB3);
            this.Controls.Add(this.cbLNB2);
            this.Controls.Add(this.cbLNB1);
            this.Name = "FrmConfig";
            this.Text = "Configuration";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox cbLNB1;
        private System.Windows.Forms.CheckBox cbLNB2;
        private System.Windows.Forms.CheckBox cbLNB3;
        private System.Windows.Forms.CheckBox cbLNB4;
        private System.Windows.Forms.ComboBox cmbSatellites1;
        private System.Windows.Forms.ComboBox cmbSatellites4;
        private System.Windows.Forms.ComboBox cmbSatellites3;
        private System.Windows.Forms.ComboBox cmbSatellites2;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
    }
}