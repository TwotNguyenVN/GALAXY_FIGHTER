namespace GUI
{
    partial class MainMenuForm
    {
        private System.ComponentModel.IContainer components = null;

        // ==== HUD / Info ====
        private System.Windows.Forms.Label lblTitle;
        public System.Windows.Forms.Label lblPlayer;
        public System.Windows.Forms.Label lblCoins;

        // ==== Khu nhập tên + 2 nút ====
        private System.Windows.Forms.TextBox txtPlayerName;
        private System.Windows.Forms.Button btnRename;       // Đổi tên tài khoản hiện tại
        private System.Windows.Forms.Button btnLoginSwitch;  // Đăng nhập / Chuyển tài khoản

        // ==== Actions ====
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Button btnSettings;
        private System.Windows.Forms.Button btnLeaderboard;
        private System.Windows.Forms.Button btnProfile;
        private System.Windows.Forms.Button btnAbout;
        private System.Windows.Forms.Button btnExit;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblPlayer = new System.Windows.Forms.Label();
            this.lblCoins = new System.Windows.Forms.Label();
            this.txtPlayerName = new System.Windows.Forms.TextBox();
            this.btnRename = new System.Windows.Forms.Button();
            this.btnLoginSwitch = new System.Windows.Forms.Button();
            this.btnPlay = new System.Windows.Forms.Button();
            this.btnSettings = new System.Windows.Forms.Button();
            this.btnLeaderboard = new System.Windows.Forms.Button();
            this.btnProfile = new System.Windows.Forms.Button();
            this.btnAbout = new System.Windows.Forms.Button();
            this.btnExit = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 48F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.Gainsboro;
            this.lblTitle.Location = new System.Drawing.Point(194, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(471, 86);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Galaxy Fighter";
            // 
            // lblPlayer
            // 
            this.lblPlayer.AutoSize = true;
            this.lblPlayer.BackColor = System.Drawing.Color.Transparent;
            this.lblPlayer.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblPlayer.ForeColor = System.Drawing.Color.White;
            this.lblPlayer.Location = new System.Drawing.Point(30, 20);
            this.lblPlayer.Name = "lblPlayer";
            this.lblPlayer.Size = new System.Drawing.Size(77, 19);
            this.lblPlayer.TabIndex = 1;
            this.lblPlayer.Text = "Player: N/A";
            // 
            // lblCoins
            // 
            this.lblCoins.AutoSize = true;
            this.lblCoins.BackColor = System.Drawing.Color.Transparent;
            this.lblCoins.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblCoins.ForeColor = System.Drawing.Color.White;
            this.lblCoins.Location = new System.Drawing.Point(30, 45);
            this.lblCoins.Name = "lblCoins";
            this.lblCoins.Size = new System.Drawing.Size(58, 19);
            this.lblCoins.TabIndex = 2;
            this.lblCoins.Text = "Coins: 0";
            // 
            // txtPlayerName
            // 
            this.txtPlayerName.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.txtPlayerName.Location = new System.Drawing.Point(80, 130);
            this.txtPlayerName.Name = "txtPlayerName";
            this.txtPlayerName.Size = new System.Drawing.Size(200, 20);
            this.txtPlayerName.TabIndex = 3;
            // 
            // btnRename
            // 
            this.btnRename.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnRename.Location = new System.Drawing.Point(80, 165);
            this.btnRename.Name = "btnRename";
            this.btnRename.Size = new System.Drawing.Size(95, 30);
            this.btnRename.TabIndex = 4;
            this.btnRename.Text = "Đổi tên";
            this.btnRename.UseVisualStyleBackColor = true;
            this.btnRename.Click += new System.EventHandler(this.btnRename_Click);
            // 
            // btnLoginSwitch
            // 
            this.btnLoginSwitch.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnLoginSwitch.Location = new System.Drawing.Point(185, 165);
            this.btnLoginSwitch.Name = "btnLoginSwitch";
            this.btnLoginSwitch.Size = new System.Drawing.Size(95, 30);
            this.btnLoginSwitch.TabIndex = 5;
            this.btnLoginSwitch.Text = "Đăng nhập";
            this.btnLoginSwitch.UseVisualStyleBackColor = true;
            this.btnLoginSwitch.Click += new System.EventHandler(this.btnLoginSwitch_Click);
            // 
            // btnPlay
            // 
            this.btnPlay.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnPlay.BackColor = System.Drawing.Color.OrangeRed;
            this.btnPlay.FlatAppearance.BorderSize = 0;
            this.btnPlay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlay.Font = new System.Drawing.Font("Segoe UI", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPlay.ForeColor = System.Drawing.Color.White;
            this.btnPlay.Location = new System.Drawing.Point(80, 220);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(200, 50);
            this.btnPlay.TabIndex = 0;
            this.btnPlay.Text = "► Play";
            this.btnPlay.UseVisualStyleBackColor = false;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // btnSettings
            // 
            this.btnSettings.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnSettings.Location = new System.Drawing.Point(80, 280);
            this.btnSettings.Name = "btnSettings";
            this.btnSettings.Size = new System.Drawing.Size(200, 35);
            this.btnSettings.TabIndex = 7;
            this.btnSettings.Text = "Settings";
            this.btnSettings.UseVisualStyleBackColor = true;
            this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
            // 
            // btnLeaderboard
            // 
            this.btnLeaderboard.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnLeaderboard.Location = new System.Drawing.Point(80, 325);
            this.btnLeaderboard.Name = "btnLeaderboard";
            this.btnLeaderboard.Size = new System.Drawing.Size(200, 35);
            this.btnLeaderboard.TabIndex = 8;
            this.btnLeaderboard.Text = "Leaderboard";
            this.btnLeaderboard.UseVisualStyleBackColor = true;
            this.btnLeaderboard.Click += new System.EventHandler(this.btnLeaderboard_Click);
            // 
            // btnProfile
            // 
            this.btnProfile.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnProfile.Location = new System.Drawing.Point(80, 370);
            this.btnProfile.Name = "btnProfile";
            this.btnProfile.Size = new System.Drawing.Size(200, 35);
            this.btnProfile.TabIndex = 9;
            this.btnProfile.Text = "Profile";
            this.btnProfile.UseVisualStyleBackColor = true;
            this.btnProfile.Click += new System.EventHandler(this.btnProfile_Click);
            // 
            // btnAbout
            // 
            this.btnAbout.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.btnAbout.Location = new System.Drawing.Point(80, 415);
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(200, 35);
            this.btnAbout.TabIndex = 10;
            this.btnAbout.Text = "About";
            this.btnAbout.UseVisualStyleBackColor = true;
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            // 
            // btnExit
            // 
            this.btnExit.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnExit.Location = new System.Drawing.Point(740, 480);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(100, 35);
            this.btnExit.TabIndex = 11;
            this.btnExit.Text = "Thoát";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(-23, -46);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(100, 50);
            this.pictureBox1.TabIndex = 12;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            this.pictureBox2.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox2.Image = global::GUI.Properties.Resources.ship_36;
            this.pictureBox2.Location = new System.Drawing.Point(450, 140);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(280, 300);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox2.TabIndex = 13;
            this.pictureBox2.TabStop = false;
            // 
            // MainMenuForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(28)))));
            this.BackgroundImage = global::GUI.Properties.Resources.bg_space_mid;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(860, 540);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblPlayer);
            this.Controls.Add(this.lblCoins);
            this.Controls.Add(this.txtPlayerName);
            this.Controls.Add(this.btnRename);
            this.Controls.Add(this.btnLoginSwitch);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.btnSettings);
            this.Controls.Add(this.btnLeaderboard);
            this.Controls.Add(this.btnProfile);
            this.Controls.Add(this.btnAbout);
            this.Controls.Add(this.btnExit);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainMenuForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Chiến Cơ Huyền Thoại - Main Menu";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
    }
}
