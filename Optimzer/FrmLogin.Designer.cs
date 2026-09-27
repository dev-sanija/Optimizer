namespace Optimzer
{
    partial class FrmLogin
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmLogin));
            pnlLeft = new Panel();
            lblAppSubtitle = new Label();
            lblAppName = new Label();
            picLogo = new PictureBox();
            pnlRight = new Panel();
            pnlLoginCard = new Panel();
            btntutorial = new Button();
            lblFooterNote = new Label();
            lblDeviceStatus = new Label();
            lblInternetStatus = new Label();
            btnExit = new Button();
            btnRegister = new Button();
            btnLogin = new Button();
            txtPassword = new TextBox();
            txtUsername = new TextBox();
            lblPassword = new Label();
            lblUsername = new Label();
            lblWelcomeSubtitle = new Label();
            lblWelcomeTitle = new Label();
            pnlLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            pnlRight.SuspendLayout();
            pnlLoginCard.SuspendLayout();
            SuspendLayout();
            // 
            // pnlLeft
            // 
            pnlLeft.BackgroundImage = Properties.Resources.robin_heemstra_8hXxNoinzLc_unsplash;
            pnlLeft.Controls.Add(lblAppSubtitle);
            pnlLeft.Controls.Add(lblAppName);
            pnlLeft.Controls.Add(picLogo);
            pnlLeft.Dock = DockStyle.Left;
            pnlLeft.Location = new Point(0, 0);
            pnlLeft.Name = "pnlLeft";
            pnlLeft.Size = new Size(400, 681);
            pnlLeft.TabIndex = 0;
            // 
            // lblAppSubtitle
            // 
            lblAppSubtitle.AutoSize = true;
            lblAppSubtitle.BackColor = Color.Transparent;
            lblAppSubtitle.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblAppSubtitle.Location = new Point(97, 195);
            lblAppSubtitle.Name = "lblAppSubtitle";
            lblAppSubtitle.Size = new Size(224, 21);
            lblAppSubtitle.TabIndex = 2;
            lblAppSubtitle.Text = "Premium access to low-end PC";
            // 
            // lblAppName
            // 
            lblAppName.AutoSize = true;
            lblAppName.BackColor = Color.Transparent;
            lblAppName.Font = new Font("Georgia", 24.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAppName.Location = new Point(82, 157);
            lblAppName.Name = "lblAppName";
            lblAppName.Size = new Size(259, 38);
            lblAppName.TabIndex = 1;
            lblAppName.Text = "SxS Optimizer";
            // 
            // picLogo
            // 
            picLogo.BackColor = Color.Transparent;
            picLogo.Image = Properties.Resources.gear;
            picLogo.Location = new Point(161, 69);
            picLogo.Name = "picLogo";
            picLogo.Size = new Size(80, 80);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.TabIndex = 0;
            picLogo.TabStop = false;
            // 
            // pnlRight
            // 
            pnlRight.BackColor = Color.FromArgb(18, 20, 28);
            pnlRight.Controls.Add(pnlLoginCard);
            pnlRight.Controls.Add(lblWelcomeSubtitle);
            pnlRight.Controls.Add(lblWelcomeTitle);
            pnlRight.Dock = DockStyle.Fill;
            pnlRight.Location = new Point(400, 0);
            pnlRight.Name = "pnlRight";
            pnlRight.Size = new Size(764, 681);
            pnlRight.TabIndex = 1;
            // 
            // pnlLoginCard
            // 
            pnlLoginCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlLoginCard.BackColor = Color.FromArgb(22, 25, 33);
            pnlLoginCard.BorderStyle = BorderStyle.FixedSingle;
            pnlLoginCard.Controls.Add(btntutorial);
            pnlLoginCard.Controls.Add(lblFooterNote);
            pnlLoginCard.Controls.Add(lblDeviceStatus);
            pnlLoginCard.Controls.Add(lblInternetStatus);
            pnlLoginCard.Controls.Add(btnExit);
            pnlLoginCard.Controls.Add(btnRegister);
            pnlLoginCard.Controls.Add(btnLogin);
            pnlLoginCard.Controls.Add(txtPassword);
            pnlLoginCard.Controls.Add(txtUsername);
            pnlLoginCard.Controls.Add(lblPassword);
            pnlLoginCard.Controls.Add(lblUsername);
            pnlLoginCard.Location = new Point(39, 133);
            pnlLoginCard.Margin = new Padding(0);
            pnlLoginCard.Name = "pnlLoginCard";
            pnlLoginCard.Size = new Size(690, 470);
            pnlLoginCard.TabIndex = 2;
            // 
            // btntutorial
            // 
            btntutorial.BackColor = Color.FromArgb(22, 25, 33);
            btntutorial.BackgroundImage = Properties.Resources.youtube;
            btntutorial.BackgroundImageLayout = ImageLayout.Stretch;
            btntutorial.Cursor = Cursors.Hand;
            btntutorial.FlatStyle = FlatStyle.Flat;
            this.btntutorial.FlatAppearance.BorderSize = 0;
            btntutorial.UseVisualStyleBackColor = false;
            btntutorial.ForeColor = Color.Transparent;
            btntutorial.Location = new Point(399, 351);
            btntutorial.Margin = new Padding(0);
            btntutorial.Name = "btntutorial";
            btntutorial.Size = new Size(41, 42);
            btntutorial.TabIndex = 3;
            btntutorial.UseVisualStyleBackColor = false;
            btntutorial.Click += btntutorial_Click;
            // 
            // lblFooterNote
            // 
            lblFooterNote.AutoSize = true;
            lblFooterNote.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblFooterNote.ForeColor = Color.FromArgb(125, 135, 150);
            lblFooterNote.Location = new Point(58, 416);
            lblFooterNote.Name = "lblFooterNote";
            lblFooterNote.Size = new Size(303, 17);
            lblFooterNote.TabIndex = 3;
            lblFooterNote.Text = "Your device fingerprint will be verified during login";
            // 
            // lblDeviceStatus
            // 
            lblDeviceStatus.AutoSize = true;
            lblDeviceStatus.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDeviceStatus.ForeColor = Color.FromArgb(34, 197, 94);
            lblDeviceStatus.Location = new Point(58, 376);
            lblDeviceStatus.Name = "lblDeviceStatus";
            lblDeviceStatus.Size = new Size(101, 17);
            lblDeviceStatus.TabIndex = 3;
            lblDeviceStatus.Text = "● Device: Ready";
            // 
            // lblInternetStatus
            // 
            lblInternetStatus.AutoSize = true;
            lblInternetStatus.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblInternetStatus.ForeColor = Color.FromArgb(34, 197, 94);
            lblInternetStatus.Location = new Point(58, 351);
            lblInternetStatus.Name = "lblInternetStatus";
            lblInternetStatus.Size = new Size(132, 17);
            lblInternetStatus.TabIndex = 3;
            lblInternetStatus.Text = "● Internet: Checking...";
            // 
            // btnExit
            // 
            btnExit.BackColor = Color.FromArgb(28, 32, 40);
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.Font = new Font("Segoe UI Semibold", 14F);
            btnExit.ForeColor = Color.Gainsboro;
            btnExit.Location = new Point(448, 351);
            btnExit.Name = "btnExit";
            btnExit.Size = new Size(190, 42);
            btnExit.TabIndex = 2;
            btnExit.Text = "Exit";
            btnExit.UseVisualStyleBackColor = false;
            btnExit.Click += btnExit_Click;
            // 
            // btnRegister
            // 
            btnRegister.BackColor = Color.FromArgb(31, 41, 55);
            btnRegister.FlatStyle = FlatStyle.Flat;
            btnRegister.Font = new Font("Segoe UI Semibold", 14F);
            btnRegister.ForeColor = SystemColors.ButtonFace;
            btnRegister.Location = new Point(58, 277);
            btnRegister.Name = "btnRegister";
            btnRegister.Size = new Size(580, 48);
            btnRegister.TabIndex = 2;
            btnRegister.Text = "Register / Buy Access";
            btnRegister.UseVisualStyleBackColor = false;
            btnRegister.Click += btnRegister_Click;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = Color.FromArgb(59, 130, 246);
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.Font = new Font("Segoe UI Semibold", 14F);
            btnLogin.Location = new Point(58, 200);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(580, 48);
            btnLogin.TabIndex = 2;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += btnLogin_Click;
            // 
            // txtPassword
            // 
            txtPassword.BackColor = Color.FromArgb(15, 17, 24);
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.Cursor = Cursors.IBeam;
            txtPassword.Font = new Font("Segoe UI", 13F);
            txtPassword.ForeColor = SystemColors.ButtonFace;
            txtPassword.Location = new Point(58, 139);
            txtPassword.MaxLength = 128;
            txtPassword.Name = "txtPassword";
            txtPassword.Size = new Size(580, 31);
            txtPassword.TabIndex = 1;
            txtPassword.TextAlign = HorizontalAlignment.Center;
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.TextChanged += txtPassword_TextChanged;
            // 
            // txtUsername
            // 
            txtUsername.BackColor = Color.FromArgb(15, 17, 24);
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            txtUsername.Cursor = Cursors.IBeam;
            txtUsername.Font = new Font("Segoe UI", 13F);
            txtUsername.ForeColor = SystemColors.ButtonFace;
            txtUsername.Location = new Point(58, 60);
            txtUsername.MaxLength = 32;
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(580, 31);
            txtUsername.TabIndex = 1;
            txtUsername.TextAlign = HorizontalAlignment.Center;
            txtUsername.TextChanged += txtUsername_TextChanged;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Font = new Font("Segoe UI Semibold", 15F);
            lblPassword.ForeColor = Color.FromArgb(248, 250, 252);
            lblPassword.Location = new Point(58, 106);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(97, 28);
            lblPassword.TabIndex = 0;
            lblPassword.Text = "Password";
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI Semibold", 15F);
            lblUsername.ForeColor = Color.FromArgb(248, 250, 252);
            lblUsername.Location = new Point(58, 23);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(104, 28);
            lblUsername.TabIndex = 0;
            lblUsername.Text = "Username";
            // 
            // lblWelcomeSubtitle
            // 
            lblWelcomeSubtitle.AutoSize = true;
            lblWelcomeSubtitle.Font = new Font("Segoe UI", 12F);
            lblWelcomeSubtitle.ForeColor = Color.FromArgb(148, 163, 184);
            lblWelcomeSubtitle.Location = new Point(26, 80);
            lblWelcomeSubtitle.Name = "lblWelcomeSubtitle";
            lblWelcomeSubtitle.Size = new Size(320, 21);
            lblWelcomeSubtitle.TabIndex = 1;
            lblWelcomeSubtitle.Text = "Secure access to premium optimization tools";
            // 
            // lblWelcomeTitle
            // 
            lblWelcomeTitle.AutoSize = true;
            lblWelcomeTitle.Font = new Font("Georgia", 27F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblWelcomeTitle.ForeColor = Color.FromArgb(243, 244, 246);
            lblWelcomeTitle.Location = new Point(23, 37);
            lblWelcomeTitle.Name = "lblWelcomeTitle";
            lblWelcomeTitle.Size = new Size(291, 41);
            lblWelcomeTitle.TabIndex = 0;
            lblWelcomeTitle.Text = "Welcome Back";
            // 
            // FrmLogin
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 17, 21);
            ClientSize = new Size(1164, 681);
            Controls.Add(pnlRight);
            Controls.Add(pnlLeft);
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ForeColor = Color.White;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new Size(1180, 720);
            MinimumSize = new Size(1180, 720);
            Name = "FrmLogin";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SxS Optimizer";
            pnlLeft.ResumeLayout(false);
            pnlLeft.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            pnlRight.ResumeLayout(false);
            pnlRight.PerformLayout();
            pnlLoginCard.ResumeLayout(false);
            pnlLoginCard.PerformLayout();
            ResumeLayout(false);
        }

        private void ApplyModernStyle()
        {
            // Remove borders
            btnLogin.FlatAppearance.BorderSize = 0;
            btnRegister.FlatAppearance.BorderSize = 0;
            btnExit.FlatAppearance.BorderSize = 0;

            // Smooth hover colors
            btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(37, 99, 235);
            btnLogin.FlatAppearance.MouseDownBackColor = Color.FromArgb(29, 78, 216);

            btnRegister.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 65, 81);
            btnRegister.FlatAppearance.MouseDownBackColor = Color.FromArgb(31, 41, 55);

            btnExit.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 55, 72);
        }

        private void MakeButtonsModern()
        {
            btnLogin.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, btnLogin.Width, btnLogin.Height, 10, 10));

            btnRegister.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, btnRegister.Width, btnRegister.Height, 10, 10));

            btnExit.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, btnExit.Width, btnExit.Height, 10, 10));
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(
            int left, int top, int right, int bottom,
            int width, int height);

        private void StyleTextBoxes()
        {
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.BorderStyle = BorderStyle.FixedSingle;

            txtUsername.BackColor = Color.FromArgb(13, 17, 23);
            txtPassword.BackColor = Color.FromArgb(13, 17, 23);

            txtUsername.ForeColor = Color.White;
            txtPassword.ForeColor = Color.White;
        }

        private void AddFocusEffect()
        {
            txtUsername.Enter += (s, e) =>
                txtUsername.BackColor = Color.FromArgb(20, 25, 35);

            txtUsername.Leave += (s, e) =>
                txtUsername.BackColor = Color.FromArgb(13, 17, 23);

            txtPassword.Enter += (s, e) =>
                txtPassword.BackColor = Color.FromArgb(20, 25, 35);

            txtPassword.Leave += (s, e) =>
                txtPassword.BackColor = Color.FromArgb(13, 17, 23);
        }


        #endregion

        private Panel pnlLeft;
        private PictureBox picLogo;
        private Panel pnlRight;
        private Label lblAppSubtitle;
        private Label lblAppName;
        private Label lblWelcomeSubtitle;
        private Label lblWelcomeTitle;
        private Panel pnlLoginCard;
        private TextBox txtPassword;
        private TextBox txtUsername;
        private Label lblPassword;
        private Label lblUsername;
        private Button btnRegister;
        private Button btnLogin;
        private Label lblDeviceStatus;
        private Label lblInternetStatus;
        private Button btnExit;
        private Label lblFooterNote;
        private Button btntutorial;
    }
}
