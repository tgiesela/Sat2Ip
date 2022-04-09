namespace Sat2IpGui
{
    partial class frmFilter
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbDVBBouquets = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbFastscanBouquets = new System.Windows.Forms.ComboBox();
            this.cmbFrequency = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbSatellites = new System.Windows.Forms.ComboBox();
            this.cbData = new System.Windows.Forms.CheckBox();
            this.cbTV = new System.Windows.Forms.CheckBox();
            this.cbRadio = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.cmbProviders = new System.Windows.Forms.ComboBox();
            this.cbFTA = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(379, 194);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(88, 27);
            this.btnCancel.TabIndex = 13;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(475, 194);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(88, 27);
            this.btnOk.TabIndex = 12;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 140);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 15);
            this.label2.TabIndex = 11;
            this.label2.Text = "DVB bouquet";
            // 
            // cmbDVBBouquets
            // 
            this.cmbDVBBouquets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDVBBouquets.FormattingEnabled = true;
            this.cmbDVBBouquets.Location = new System.Drawing.Point(120, 137);
            this.cmbDVBBouquets.Name = "cmbDVBBouquets";
            this.cmbDVBBouquets.Size = new System.Drawing.Size(314, 23);
            this.cmbDVBBouquets.TabIndex = 10;
            this.cmbDVBBouquets.SelectedIndexChanged += new System.EventHandler(this.cmbDVBBouquets_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 111);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 15);
            this.label1.TabIndex = 9;
            this.label1.Text = "Fastscan bouquet";
            // 
            // cmbFastscanBouquets
            // 
            this.cmbFastscanBouquets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFastscanBouquets.FormattingEnabled = true;
            this.cmbFastscanBouquets.Location = new System.Drawing.Point(120, 108);
            this.cmbFastscanBouquets.Name = "cmbFastscanBouquets";
            this.cmbFastscanBouquets.Size = new System.Drawing.Size(314, 23);
            this.cmbFastscanBouquets.TabIndex = 8;
            this.cmbFastscanBouquets.SelectedIndexChanged += new System.EventHandler(this.cmbFastscanBouquets_SelectedIndexChanged);
            // 
            // cmbFrequency
            // 
            this.cmbFrequency.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFrequency.FormattingEnabled = true;
            this.cmbFrequency.Location = new System.Drawing.Point(120, 47);
            this.cmbFrequency.Name = "cmbFrequency";
            this.cmbFrequency.Size = new System.Drawing.Size(248, 23);
            this.cmbFrequency.TabIndex = 14;
            this.cmbFrequency.SelectedIndexChanged += new System.EventHandler(this.cmbFrequency_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 50);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(62, 15);
            this.label3.TabIndex = 15;
            this.label3.Text = "Frequency";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 21);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 15);
            this.label4.TabIndex = 17;
            this.label4.Text = "Satellite";
            // 
            // cmbSatellites
            // 
            this.cmbSatellites.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSatellites.FormattingEnabled = true;
            this.cmbSatellites.Location = new System.Drawing.Point(120, 18);
            this.cmbSatellites.Name = "cmbSatellites";
            this.cmbSatellites.Size = new System.Drawing.Size(166, 23);
            this.cmbSatellites.TabIndex = 16;
            this.cmbSatellites.SelectedIndexChanged += new System.EventHandler(this.cmbSatellites_SelectedIndexChanged);
            // 
            // cbData
            // 
            this.cbData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbData.AutoSize = true;
            this.cbData.Location = new System.Drawing.Point(419, 22);
            this.cbData.Name = "cbData";
            this.cbData.Size = new System.Drawing.Size(50, 19);
            this.cbData.TabIndex = 20;
            this.cbData.Text = "Data";
            this.cbData.UseVisualStyleBackColor = true;
            // 
            // cbTV
            // 
            this.cbTV.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbTV.AutoSize = true;
            this.cbTV.Location = new System.Drawing.Point(374, 22);
            this.cbTV.Name = "cbTV";
            this.cbTV.Size = new System.Drawing.Size(39, 19);
            this.cbTV.TabIndex = 19;
            this.cbTV.Text = "TV";
            this.cbTV.UseVisualStyleBackColor = true;
            // 
            // cbRadio
            // 
            this.cbRadio.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbRadio.AutoSize = true;
            this.cbRadio.Location = new System.Drawing.Point(312, 22);
            this.cbRadio.Name = "cbRadio";
            this.cbRadio.Size = new System.Drawing.Size(56, 19);
            this.cbRadio.TabIndex = 18;
            this.cbRadio.Text = "Radio";
            this.cbRadio.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 79);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(51, 15);
            this.label5.TabIndex = 22;
            this.label5.Text = "Provider";
            // 
            // cmbProviders
            // 
            this.cmbProviders.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProviders.FormattingEnabled = true;
            this.cmbProviders.Location = new System.Drawing.Point(120, 76);
            this.cmbProviders.Name = "cmbProviders";
            this.cmbProviders.Size = new System.Drawing.Size(248, 23);
            this.cmbProviders.TabIndex = 21;
            this.cmbProviders.SelectedIndexChanged += new System.EventHandler(this.cmbProviders_SelectedIndexChanged);
            // 
            // cbFTA
            // 
            this.cbFTA.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cbFTA.AutoSize = true;
            this.cbFTA.Location = new System.Drawing.Point(475, 22);
            this.cbFTA.Name = "cbFTA";
            this.cbFTA.Size = new System.Drawing.Size(85, 19);
            this.cbFTA.TabIndex = 23;
            this.cbFTA.Text = "Free-To-Air";
            this.cbFTA.UseVisualStyleBackColor = true;
            // 
            // frmFilter
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(576, 238);
            this.Controls.Add(this.cbFTA);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cmbProviders);
            this.Controls.Add(this.cbData);
            this.Controls.Add(this.cbTV);
            this.Controls.Add(this.cbRadio);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cmbSatellites);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmbFrequency);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbDVBBouquets);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbFastscanBouquets);
            this.Name = "frmFilter";
            this.Text = "frmFilter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbDVBBouquets;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbFastscanBouquets;
        private System.Windows.Forms.ComboBox cmbFrequency;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cmbSatellites;
        private System.Windows.Forms.CheckBox cbData;
        private System.Windows.Forms.CheckBox cbTV;
        private System.Windows.Forms.CheckBox cbRadio;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cmbProviders;
        private System.Windows.Forms.CheckBox cbFTA;
    }
}