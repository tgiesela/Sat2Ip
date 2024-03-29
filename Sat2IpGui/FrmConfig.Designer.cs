﻿namespace Sat2IpGui
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
            this.txtOscamserver = new System.Windows.Forms.TextBox();
            this.txtOscamport = new System.Windows.Forms.TextBox();
            this.lblOscam = new System.Windows.Forms.Label();
            this.lblOscamport = new System.Windows.Forms.Label();
            this.cbFixedTuner = new System.Windows.Forms.CheckBox();
            this.numTuner = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.txtIpAddressDevice = new System.Windows.Forms.TextBox();
            this.gbType = new System.Windows.Forms.GroupBox();
            this.rbDVBC = new System.Windows.Forms.RadioButton();
            this.rbDVBS = new System.Windows.Forms.RadioButton();
            this.gbLNB = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.numTuner)).BeginInit();
            this.gbType.SuspendLayout();
            this.gbLNB.SuspendLayout();
            this.SuspendLayout();
            // 
            // cbLNB1
            // 
            this.cbLNB1.AutoSize = true;
            this.cbLNB1.Location = new System.Drawing.Point(15, 22);
            this.cbLNB1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbLNB1.Name = "cbLNB1";
            this.cbLNB1.Size = new System.Drawing.Size(57, 19);
            this.cbLNB1.TabIndex = 0;
            this.cbLNB1.Text = "LNB 1";
            this.cbLNB1.UseVisualStyleBackColor = true;
            this.cbLNB1.CheckedChanged += new System.EventHandler(this.cbLNB1_CheckedChanged);
            // 
            // cbLNB2
            // 
            this.cbLNB2.AutoSize = true;
            this.cbLNB2.Location = new System.Drawing.Point(15, 48);
            this.cbLNB2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbLNB2.Name = "cbLNB2";
            this.cbLNB2.Size = new System.Drawing.Size(57, 19);
            this.cbLNB2.TabIndex = 1;
            this.cbLNB2.Text = "LNB 2";
            this.cbLNB2.UseVisualStyleBackColor = true;
            this.cbLNB2.CheckedChanged += new System.EventHandler(this.cbLNB2_CheckedChanged);
            // 
            // cbLNB3
            // 
            this.cbLNB3.AutoSize = true;
            this.cbLNB3.Location = new System.Drawing.Point(15, 75);
            this.cbLNB3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbLNB3.Name = "cbLNB3";
            this.cbLNB3.Size = new System.Drawing.Size(57, 19);
            this.cbLNB3.TabIndex = 2;
            this.cbLNB3.Text = "LNB 3";
            this.cbLNB3.UseVisualStyleBackColor = true;
            this.cbLNB3.CheckedChanged += new System.EventHandler(this.cbLNB3_CheckedChanged);
            // 
            // cbLNB4
            // 
            this.cbLNB4.AutoSize = true;
            this.cbLNB4.Location = new System.Drawing.Point(15, 102);
            this.cbLNB4.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cbLNB4.Name = "cbLNB4";
            this.cbLNB4.Size = new System.Drawing.Size(57, 19);
            this.cbLNB4.TabIndex = 3;
            this.cbLNB4.Text = "LNB 4";
            this.cbLNB4.UseVisualStyleBackColor = true;
            this.cbLNB4.CheckedChanged += new System.EventHandler(this.cbLNB4_CheckedChanged);
            // 
            // cmbSatellites1
            // 
            this.cmbSatellites1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSatellites1.FormattingEnabled = true;
            this.cmbSatellites1.Location = new System.Drawing.Point(88, 20);
            this.cmbSatellites1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbSatellites1.Name = "cmbSatellites1";
            this.cmbSatellites1.Size = new System.Drawing.Size(265, 23);
            this.cmbSatellites1.TabIndex = 4;
            // 
            // cmbSatellites4
            // 
            this.cmbSatellites4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSatellites4.FormattingEnabled = true;
            this.cmbSatellites4.Location = new System.Drawing.Point(88, 99);
            this.cmbSatellites4.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbSatellites4.Name = "cmbSatellites4";
            this.cmbSatellites4.Size = new System.Drawing.Size(265, 23);
            this.cmbSatellites4.TabIndex = 5;
            // 
            // cmbSatellites3
            // 
            this.cmbSatellites3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSatellites3.FormattingEnabled = true;
            this.cmbSatellites3.Location = new System.Drawing.Point(88, 73);
            this.cmbSatellites3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbSatellites3.Name = "cmbSatellites3";
            this.cmbSatellites3.Size = new System.Drawing.Size(265, 23);
            this.cmbSatellites3.TabIndex = 6;
            // 
            // cmbSatellites2
            // 
            this.cmbSatellites2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSatellites2.FormattingEnabled = true;
            this.cmbSatellites2.Location = new System.Drawing.Point(88, 46);
            this.cmbSatellites2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbSatellites2.Name = "cmbSatellites2";
            this.cmbSatellites2.Size = new System.Drawing.Size(265, 23);
            this.cmbSatellites2.TabIndex = 7;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(368, 351);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(88, 27);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(462, 351);
            this.btnOK.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(88, 27);
            this.btnOK.TabIndex = 9;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // txtOscamserver
            // 
            this.txtOscamserver.Location = new System.Drawing.Point(110, 234);
            this.txtOscamserver.Name = "txtOscamserver";
            this.txtOscamserver.Size = new System.Drawing.Size(100, 23);
            this.txtOscamserver.TabIndex = 10;
            // 
            // txtOscamport
            // 
            this.txtOscamport.Location = new System.Drawing.Point(248, 234);
            this.txtOscamport.Name = "txtOscamport";
            this.txtOscamport.Size = new System.Drawing.Size(62, 23);
            this.txtOscamport.TabIndex = 11;
            // 
            // lblOscam
            // 
            this.lblOscam.AutoSize = true;
            this.lblOscam.Location = new System.Drawing.Point(26, 237);
            this.lblOscam.Name = "lblOscam";
            this.lblOscam.Size = new System.Drawing.Size(78, 15);
            this.lblOscam.TabIndex = 12;
            this.lblOscam.Text = "Oscam server";
            // 
            // lblOscamport
            // 
            this.lblOscamport.AutoSize = true;
            this.lblOscamport.Location = new System.Drawing.Point(213, 237);
            this.lblOscamport.Name = "lblOscamport";
            this.lblOscamport.Size = new System.Drawing.Size(29, 15);
            this.lblOscamport.TabIndex = 13;
            this.lblOscamport.Text = "Port";
            // 
            // cbFixedTuner
            // 
            this.cbFixedTuner.AutoSize = true;
            this.cbFixedTuner.Location = new System.Drawing.Point(26, 274);
            this.cbFixedTuner.Name = "cbFixedTuner";
            this.cbFixedTuner.Size = new System.Drawing.Size(85, 19);
            this.cbFixedTuner.TabIndex = 14;
            this.cbFixedTuner.Text = "Fixed tuner";
            this.cbFixedTuner.UseVisualStyleBackColor = true;
            this.cbFixedTuner.CheckedChanged += new System.EventHandler(this.cbFixedTuner_CheckedChanged);
            // 
            // numTuner
            // 
            this.numTuner.Location = new System.Drawing.Point(117, 270);
            this.numTuner.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numTuner.Name = "numTuner";
            this.numTuner.Size = new System.Drawing.Size(49, 23);
            this.numTuner.TabIndex = 16;
            this.numTuner.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(26, 208);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 15);
            this.label1.TabIndex = 18;
            this.label1.Text = "Sat2IP Server";
            // 
            // txtIpAddressDevice
            // 
            this.txtIpAddressDevice.Location = new System.Drawing.Point(110, 205);
            this.txtIpAddressDevice.Name = "txtIpAddressDevice";
            this.txtIpAddressDevice.Size = new System.Drawing.Size(100, 23);
            this.txtIpAddressDevice.TabIndex = 17;
            // 
            // gbType
            // 
            this.gbType.Controls.Add(this.rbDVBC);
            this.gbType.Controls.Add(this.rbDVBS);
            this.gbType.Location = new System.Drawing.Point(26, 12);
            this.gbType.Name = "gbType";
            this.gbType.Size = new System.Drawing.Size(184, 49);
            this.gbType.TabIndex = 19;
            this.gbType.TabStop = false;
            this.gbType.Text = "Type";
            // 
            // rbDVBC
            // 
            this.rbDVBC.AutoSize = true;
            this.rbDVBC.Location = new System.Drawing.Point(99, 16);
            this.rbDVBC.Name = "rbDVBC";
            this.rbDVBC.Size = new System.Drawing.Size(60, 19);
            this.rbDVBC.TabIndex = 1;
            this.rbDVBC.TabStop = true;
            this.rbDVBC.Text = "DVB-C";
            this.rbDVBC.UseVisualStyleBackColor = true;
            // 
            // rbDVBS
            // 
            this.rbDVBS.AutoSize = true;
            this.rbDVBS.Location = new System.Drawing.Point(35, 16);
            this.rbDVBS.Name = "rbDVBS";
            this.rbDVBS.Size = new System.Drawing.Size(58, 19);
            this.rbDVBS.TabIndex = 0;
            this.rbDVBS.TabStop = true;
            this.rbDVBS.Text = "DVB-S";
            this.rbDVBS.UseVisualStyleBackColor = true;
            this.rbDVBS.CheckedChanged += new System.EventHandler(this.rbDVBS_CheckedChanged);
            // 
            // gbLNB
            // 
            this.gbLNB.Controls.Add(this.cbLNB1);
            this.gbLNB.Controls.Add(this.cbLNB2);
            this.gbLNB.Controls.Add(this.cbLNB3);
            this.gbLNB.Controls.Add(this.cbLNB4);
            this.gbLNB.Controls.Add(this.cmbSatellites1);
            this.gbLNB.Controls.Add(this.cmbSatellites4);
            this.gbLNB.Controls.Add(this.cmbSatellites3);
            this.gbLNB.Controls.Add(this.cmbSatellites2);
            this.gbLNB.Location = new System.Drawing.Point(26, 67);
            this.gbLNB.Name = "gbLNB";
            this.gbLNB.Size = new System.Drawing.Size(369, 135);
            this.gbLNB.TabIndex = 20;
            this.gbLNB.TabStop = false;
            this.gbLNB.Text = "LNBs";
            // 
            // FrmConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(564, 381);
            this.Controls.Add(this.gbLNB);
            this.Controls.Add(this.gbType);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtIpAddressDevice);
            this.Controls.Add(this.numTuner);
            this.Controls.Add(this.cbFixedTuner);
            this.Controls.Add(this.lblOscamport);
            this.Controls.Add(this.lblOscam);
            this.Controls.Add(this.txtOscamport);
            this.Controls.Add(this.txtOscamserver);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "FrmConfig";
            this.Text = "Configuration";
            ((System.ComponentModel.ISupportInitialize)(this.numTuner)).EndInit();
            this.gbType.ResumeLayout(false);
            this.gbType.PerformLayout();
            this.gbLNB.ResumeLayout(false);
            this.gbLNB.PerformLayout();
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
        private System.Windows.Forms.TextBox txtOscamserver;
        private System.Windows.Forms.TextBox txtOscamport;
        private System.Windows.Forms.Label lblOscam;
        private System.Windows.Forms.Label lblOscamport;
        private System.Windows.Forms.CheckBox cbFixedTuner;
        private System.Windows.Forms.NumericUpDown numTuner;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtIpAddressDevice;
        private System.Windows.Forms.GroupBox gbType;
        private System.Windows.Forms.RadioButton rbDVBC;
        private System.Windows.Forms.RadioButton rbDVBS;
        private System.Windows.Forms.GroupBox gbLNB;
    }
}