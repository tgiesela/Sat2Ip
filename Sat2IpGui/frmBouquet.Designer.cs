namespace Sat2IpGui
{
    partial class frmBouquet
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
            this.cmbFastscanBouquets = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbDVBBouquets = new System.Windows.Forms.ComboBox();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cmbFastscanBouquets
            // 
            this.cmbFastscanBouquets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFastscanBouquets.FormattingEnabled = true;
            this.cmbFastscanBouquets.Location = new System.Drawing.Point(171, 21);
            this.cmbFastscanBouquets.Name = "cmbFastscanBouquets";
            this.cmbFastscanBouquets.Size = new System.Drawing.Size(314, 23);
            this.cmbFastscanBouquets.TabIndex = 0;
            this.cmbFastscanBouquets.SelectedIndexChanged += new System.EventHandler(this.cmbFastscanBouquets_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(22, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Fastscan bouquet";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(22, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "DVB bouquet";
            // 
            // cmbDVBBouquets
            // 
            this.cmbDVBBouquets.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDVBBouquets.FormattingEnabled = true;
            this.cmbDVBBouquets.Location = new System.Drawing.Point(171, 50);
            this.cmbDVBBouquets.Name = "cmbDVBBouquets";
            this.cmbDVBBouquets.Size = new System.Drawing.Size(314, 23);
            this.cmbDVBBouquets.TabIndex = 2;
            this.cmbDVBBouquets.SelectedIndexChanged += new System.EventHandler(this.cmbDVBBouquets_SelectedIndexChanged);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(303, 100);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(88, 27);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(397, 100);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(88, 27);
            this.btnOk.TabIndex = 6;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // frmBouquet
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(513, 150);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbDVBBouquets);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbFastscanBouquets);
            this.Name = "frmBouquet";
            this.Text = "frmBouquet";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbFastscanBouquets;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbDVBBouquets;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
    }
}