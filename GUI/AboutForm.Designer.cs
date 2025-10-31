namespace GUI
{
    partial class AboutForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.TextBox txtDesc;
        private System.Windows.Forms.GroupBox grpShortcuts;
        private System.Windows.Forms.Label lblShortcuts;
        private System.Windows.Forms.LinkLabel lnkProject;
        private System.Windows.Forms.Button btnOk;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.txtDesc = new System.Windows.Forms.TextBox();
            this.grpShortcuts = new System.Windows.Forms.GroupBox();
            this.lblShortcuts = new System.Windows.Forms.Label();
            this.lnkProject = new System.Windows.Forms.LinkLabel();
            this.btnOk = new System.Windows.Forms.Button();
            this.lblProduct = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(16, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(183, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Giới thiệu ứng dụng";
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(706, 621);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(72, 13);
            this.lblVersion.TabIndex = 2;
            this.lblVersion.Text = "Version: 1.0.0";
            // 
            // txtDesc
            // 
            this.txtDesc.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDesc.Location = new System.Drawing.Point(23, 367);
            this.txtDesc.Multiline = true;
            this.txtDesc.Name = "txtDesc";
            this.txtDesc.ReadOnly = true;
            this.txtDesc.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDesc.Size = new System.Drawing.Size(591, 206);
            this.txtDesc.TabIndex = 4;
            this.txtDesc.Text = resources.GetString("txtDesc.Text");
            // 
            // grpShortcuts
            // 
            this.grpShortcuts.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.grpShortcuts.Location = new System.Drawing.Point(23, 144);
            this.grpShortcuts.Name = "grpShortcuts";
            this.grpShortcuts.Size = new System.Drawing.Size(591, 186);
            this.grpShortcuts.TabIndex = 5;
            this.grpShortcuts.TabStop = false;
            this.grpShortcuts.Text = "Danh sách thành viên";
            // 
            // lblShortcuts
            // 
            this.lblShortcuts.AutoSize = true;
            this.lblShortcuts.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblShortcuts.Location = new System.Drawing.Point(645, 164);
            this.lblShortcuts.Name = "lblShortcuts";
            this.lblShortcuts.Size = new System.Drawing.Size(300, 150);
            this.lblShortcuts.TabIndex = 0;
            this.lblShortcuts.Text = "Trong Game:\r\n- ESC: Pause / Continue\r\n- Chuột trái: Bắn (ấn hoặc giữ)\r\n- Di Chuyể" +
    "n: Bằng chuột\r\n- Space: Để phóng tên lửa\r\n- S : Sử dụng khiêng chắn\r\n";
            // 
            // lnkProject
            // 
            this.lnkProject.AutoSize = true;
            this.lnkProject.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lnkProject.Location = new System.Drawing.Point(34, 621);
            this.lnkProject.Name = "lnkProject";
            this.lnkProject.Size = new System.Drawing.Size(289, 16);
            this.lnkProject.TabIndex = 6;
            this.lnkProject.TabStop = true;
            this.lnkProject.Text = "https://gemini.google.com/share/1779864d1d97";
            this.lnkProject.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkProject_LinkClicked_1);
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(852, 624);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(90, 28);
            this.btnOk.TabIndex = 7;
            this.btnOk.Text = "Đóng";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // lblProduct
            // 
            this.lblProduct.AutoSize = true;
            this.lblProduct.Font = new System.Drawing.Font("Microsoft Tai Le", 36F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProduct.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.lblProduct.Location = new System.Drawing.Point(326, 70);
            this.lblProduct.Name = "lblProduct";
            this.lblProduct.Size = new System.Drawing.Size(355, 61);
            this.lblProduct.TabIndex = 8;
            this.lblProduct.Text = "Galaxy Fighter";
            // 
            // AboutForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(965, 667);
            this.Controls.Add(this.lblProduct);
            this.Controls.Add(this.txtDesc);
            this.Controls.Add(this.lblShortcuts);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.lnkProject);
            this.Controls.Add(this.grpShortcuts);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion
        private System.Windows.Forms.Label lblProduct;
    }
}
