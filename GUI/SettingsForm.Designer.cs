namespace GUI
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitle;

        private System.Windows.Forms.Label lblMissile;
        private System.Windows.Forms.NumericUpDown nudMissile;

        private System.Windows.Forms.Label lblBase;
        private System.Windows.Forms.NumericUpDown nudBase;

        private System.Windows.Forms.Label lblDay7;
        private System.Windows.Forms.NumericUpDown nudDay7;

        private System.Windows.Forms.Button btnReload;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnClose;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblMissile = new System.Windows.Forms.Label();
            this.nudMissile = new System.Windows.Forms.NumericUpDown();
            this.lblBase = new System.Windows.Forms.Label();
            this.nudBase = new System.Windows.Forms.NumericUpDown();
            this.lblDay7 = new System.Windows.Forms.Label();
            this.nudDay7 = new System.Windows.Forms.NumericUpDown();
            this.btnReload = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudMissile)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBase)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDay7)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(18, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(213, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Cấu hình gameplay";
            // 
            // lblMissile
            // 
            this.lblMissile.AutoSize = true;
            this.lblMissile.Location = new System.Drawing.Point(22, 62);
            this.lblMissile.Name = "lblMissile";
            this.lblMissile.Size = new System.Drawing.Size(196, 15);
            this.lblMissile.TabIndex = 1;
            this.lblMissile.Text = "Missile cooldown (ms) (0–60000):";
            // 
            // nudMissile
            // 
            this.nudMissile.Increment = new decimal(new int[] { 50, 0, 0, 0 });
            this.nudMissile.Location = new System.Drawing.Point(25, 80);
            this.nudMissile.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this.nudMissile.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudMissile.Name = "nudMissile";
            this.nudMissile.Size = new System.Drawing.Size(140, 23);
            this.nudMissile.TabIndex = 2;
            this.nudMissile.Value = new decimal(new int[] { 2500, 0, 0, 0 });
            // 
            // lblBase
            // 
            this.lblBase.AutoSize = true;
            this.lblBase.Location = new System.Drawing.Point(22, 118);
            this.lblBase.Name = "lblBase";
            this.lblBase.Size = new System.Drawing.Size(132, 15);
            this.lblBase.TabIndex = 3;
            this.lblBase.Text = "Daily base bonus (0–50):";
            // 
            // nudBase
            // 
            this.nudBase.Location = new System.Drawing.Point(25, 136);
            this.nudBase.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            this.nudBase.Name = "nudBase";
            this.nudBase.Size = new System.Drawing.Size(140, 23);
            this.nudBase.TabIndex = 4;
            this.nudBase.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // lblDay7
            // 
            this.lblDay7.AutoSize = true;
            this.lblDay7.Location = new System.Drawing.Point(22, 173);
            this.lblDay7.Name = "lblDay7";
            this.lblDay7.Size = new System.Drawing.Size(148, 15);
            this.lblDay7.TabIndex = 5;
            this.lblDay7.Text = "Daily day-7 bonus (0–100):";
            // 
            // nudDay7
            // 
            this.nudDay7.Location = new System.Drawing.Point(25, 191);
            this.nudDay7.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.nudDay7.Name = "nudDay7";
            this.nudDay7.Size = new System.Drawing.Size(140, 23);
            this.nudDay7.TabIndex = 6;
            this.nudDay7.Value = new decimal(new int[] { 6, 0, 0, 0 });
            // 
            // btnReload
            // 
            this.btnReload.Location = new System.Drawing.Point(25, 238);
            this.btnReload.Name = "btnReload";
            this.btnReload.Size = new System.Drawing.Size(90, 30);
            this.btnReload.TabIndex = 7;
            this.btnReload.Text = "Load";
            this.btnReload.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(131, 238);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(90, 30);
            this.btnSave.TabIndex = 8;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(237, 238);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(90, 30);
            this.btnClose.TabIndex = 9;
            this.btnClose.Text = "Đóng";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(344, 292);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnReload);
            this.Controls.Add(this.nudDay7);
            this.Controls.Add(this.lblDay7);
            this.Controls.Add(this.nudBase);
            this.Controls.Add(this.lblBase);
            this.Controls.Add(this.nudMissile);
            this.Controls.Add(this.lblMissile);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)(this.nudMissile)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudBase)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDay7)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion
    }
}
