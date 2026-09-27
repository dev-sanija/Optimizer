namespace Optimzer
{
    partial class FrmSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSettings));
            pnlTopHeader = new Panel();
            lblSubtitle = new Label();
            lblMainTitle = new Label();
            pnlContent = new Panel();
            pnlfootersupport = new Panel();
            lblSupportNote = new Label();
            btnBack = new Button();
            btnCompleteRegistration = new Button();
            textBox2 = new TextBox();
            lblConfirmPassword = new Label();
            txtNewPassword = new TextBox();
            lblNewPassword = new Label();
            txtUsername = new TextBox();
            lblUsername = new Label();
            btnManualActivation = new Button();
            btnOpenPaymentPortal = new Button();
            btnCopyInstallId = new Button();
            txtInstallId = new TextBox();
            lblPaymentNote = new Label();
            lblInstallId = new Label();
            pnlPaymentStatus = new Panel();
            lblPaymentStatusTitle = new Label();
            lblPaymentStatusSub = new Label();
            lblnote = new Label();
            pnlTopHeader.SuspendLayout();
            pnlContent.SuspendLayout();
            pnlfootersupport.SuspendLayout();
            pnlPaymentStatus.SuspendLayout();
            SuspendLayout();
            // 
            // pnlTopHeader
            // 
            pnlTopHeader.BackColor = Color.FromArgb(22, 24, 32);
            pnlTopHeader.Controls.Add(lblSubtitle);
            pnlTopHeader.Controls.Add(lblMainTitle);
            pnlTopHeader.Dock = DockStyle.Top;
            pnlTopHeader.Location = new Point(0, 0);
            pnlTopHeader.Name = "pnlTopHeader";
            pnlTopHeader.Size = new Size(847, 113);
            pnlTopHeader.TabIndex = 0;
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.BackColor = Color.Transparent;
            lblSubtitle.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblSubtitle.ForeColor = Color.FromArgb(156, 163, 175);
            lblSubtitle.Location = new Point(12, 59);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(352, 21);
            lblSubtitle.TabIndex = 0;
            lblSubtitle.Text = "Activate for this Windows installation to continue.";
            // 
            // lblMainTitle
            // 
            lblMainTitle.AutoSize = true;
            lblMainTitle.BackColor = Color.Transparent;
            lblMainTitle.Font = new Font("Georgia", 21.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMainTitle.ForeColor = Color.FromArgb(245, 247, 250);
            lblMainTitle.Location = new Point(12, 21);
            lblMainTitle.Name = "lblMainTitle";
            lblMainTitle.Size = new Size(432, 34);
            lblMainTitle.TabIndex = 0;
            lblMainTitle.Text = "Buy Access && Account Setup";
            // 
            // pnlContent
            // 
            pnlContent.BackColor = Color.FromArgb(18, 20, 28);
            pnlContent.BorderStyle = BorderStyle.FixedSingle;
            pnlContent.Controls.Add(lblnote);
            pnlContent.Controls.Add(pnlfootersupport);
            pnlContent.Controls.Add(btnBack);
            pnlContent.Controls.Add(btnCompleteRegistration);
            pnlContent.Controls.Add(textBox2);
            pnlContent.Controls.Add(lblConfirmPassword);
            pnlContent.Controls.Add(txtNewPassword);
            pnlContent.Controls.Add(lblNewPassword);
            pnlContent.Controls.Add(txtUsername);
            pnlContent.Controls.Add(lblUsername);
            pnlContent.Controls.Add(btnManualActivation);
            pnlContent.Controls.Add(btnOpenPaymentPortal);
            pnlContent.Controls.Add(btnCopyInstallId);
            pnlContent.Controls.Add(txtInstallId);
            pnlContent.Controls.Add(lblPaymentNote);
            pnlContent.Controls.Add(lblInstallId);
            pnlContent.Controls.Add(pnlPaymentStatus);
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(0, 113);
            pnlContent.Name = "pnlContent";
            pnlContent.Size = new Size(847, 702);
            pnlContent.TabIndex = 1;
            // 
            // pnlfootersupport
            // 
            pnlfootersupport.Controls.Add(lblSupportNote);
            pnlfootersupport.Dock = DockStyle.Bottom;
            pnlfootersupport.Location = new Point(0, 600);
            pnlfootersupport.Name = "pnlfootersupport";
            pnlfootersupport.Size = new Size(845, 100);
            pnlfootersupport.TabIndex = 11;
            // 
            // lblSupportNote
            // 
            lblSupportNote.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            lblSupportNote.AutoSize = true;
            lblSupportNote.BackColor = Color.Transparent;
            lblSupportNote.Font = new Font("Segoe UI", 10.5F);
            lblSupportNote.ForeColor = Color.FromArgb(145, 150, 160);
            lblSupportNote.Location = new Point(171, 43);
            lblSupportNote.Name = "lblSupportNote";
            lblSupportNote.Size = new Size(533, 19);
            lblSupportNote.TabIndex = 10;
            lblSupportNote.Text = "Use the Install ID above when contacting support about payment or activation issues.";
            // 
            // btnBack
            // 
            btnBack.BackColor = Color.FromArgb(28, 32, 40);
            btnBack.FlatStyle = FlatStyle.Flat;
            btnBack.Font = new Font("Segoe UI", 13F);
            btnBack.ForeColor = Color.FromArgb(230, 230, 230);
            btnBack.Location = new Point(334, 552);
            btnBack.Name = "btnBack";
            btnBack.Size = new Size(166, 40);
            btnBack.TabIndex = 9;
            btnBack.Text = "Back";
            btnBack.UseVisualStyleBackColor = false;
            btnBack.Click += btnBack_Click;
            // 
            // btnCompleteRegistration
            // 
            btnCompleteRegistration.BackColor = Color.FromArgb(28, 32, 40);
            btnCompleteRegistration.FlatStyle = FlatStyle.Flat;
            btnCompleteRegistration.Font = new Font("Segoe UI Semibold", 13F);
            btnCompleteRegistration.ForeColor = Color.FromArgb(230, 230, 230);
            btnCompleteRegistration.Location = new Point(14, 496);
            btnCompleteRegistration.Name = "btnCompleteRegistration";
            btnCompleteRegistration.Size = new Size(815, 50);
            btnCompleteRegistration.TabIndex = 8;
            btnCompleteRegistration.Text = "Complete Registration/Upgrade";
            btnCompleteRegistration.UseVisualStyleBackColor = false;
            btnCompleteRegistration.Click += btnCompleteRegistration_Click;
            // 
            // textBox2
            // 
            textBox2.BackColor = Color.FromArgb(12, 14, 22);
            textBox2.BorderStyle = BorderStyle.FixedSingle;
            textBox2.Cursor = Cursors.IBeam;
            textBox2.Font = new Font("Segoe UI", 12F);
            textBox2.ForeColor = Color.White;
            textBox2.Location = new Point(14, 447);
            textBox2.MaxLength = 20;
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(815, 29);
            textBox2.TabIndex = 7;
            textBox2.UseSystemPasswordChar = true;
            textBox2.TextChanged += textBox2_TextChanged;
            // 
            // lblConfirmPassword
            // 
            lblConfirmPassword.AutoSize = true;
            lblConfirmPassword.Font = new Font("Segoe UI Semibold", 13F);
            lblConfirmPassword.ForeColor = SystemColors.ButtonFace;
            lblConfirmPassword.Location = new Point(14, 419);
            lblConfirmPassword.Name = "lblConfirmPassword";
            lblConfirmPassword.Size = new Size(162, 25);
            lblConfirmPassword.TabIndex = 6;
            lblConfirmPassword.Text = "Confirm Password";
            // 
            // txtNewPassword
            // 
            txtNewPassword.BackColor = Color.FromArgb(12, 14, 22);
            txtNewPassword.BorderStyle = BorderStyle.FixedSingle;
            txtNewPassword.Cursor = Cursors.IBeam;
            txtNewPassword.Font = new Font("Segoe UI", 12F);
            txtNewPassword.ForeColor = Color.White;
            txtNewPassword.Location = new Point(14, 387);
            txtNewPassword.MaxLength = 20;
            txtNewPassword.Name = "txtNewPassword";
            txtNewPassword.Size = new Size(815, 29);
            txtNewPassword.TabIndex = 7;
            txtNewPassword.UseSystemPasswordChar = true;
            txtNewPassword.TextChanged += txtNewPassword_TextChanged;
            // 
            // lblNewPassword
            // 
            lblNewPassword.AutoSize = true;
            lblNewPassword.Font = new Font("Segoe UI Semibold", 13F);
            lblNewPassword.ForeColor = SystemColors.ButtonFace;
            lblNewPassword.Location = new Point(14, 359);
            lblNewPassword.Name = "lblNewPassword";
            lblNewPassword.Size = new Size(133, 25);
            lblNewPassword.TabIndex = 6;
            lblNewPassword.Text = "New Password";
            // 
            // txtUsername
            // 
            txtUsername.BackColor = Color.FromArgb(12, 14, 22);
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            txtUsername.Cursor = Cursors.IBeam;
            txtUsername.Font = new Font("Segoe UI", 12F);
            txtUsername.ForeColor = Color.White;
            txtUsername.Location = new Point(14, 324);
            txtUsername.MaxLength = 20;
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(815, 29);
            txtUsername.TabIndex = 7;
            txtUsername.TextChanged += txtUsername_TextChanged;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Font = new Font("Segoe UI Semibold", 13F);
            lblUsername.ForeColor = SystemColors.ButtonFace;
            lblUsername.Location = new Point(14, 296);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(96, 25);
            lblUsername.TabIndex = 6;
            lblUsername.Text = "Username";
            // 
            // btnManualActivation
            // 
            btnManualActivation.BackColor = Color.FromArgb(68, 126, 232);
            btnManualActivation.FlatStyle = FlatStyle.Flat;
            btnManualActivation.Font = new Font("Segoe UI Semibold", 15F);
            btnManualActivation.ForeColor = Color.White;
            btnManualActivation.Location = new Point(449, 201);
            btnManualActivation.Name = "btnManualActivation";
            btnManualActivation.Size = new Size(380, 50);
            btnManualActivation.TabIndex = 5;
            btnManualActivation.Text = "Manual Activation";
            btnManualActivation.UseVisualStyleBackColor = false;
            btnManualActivation.Click += btnOpenPaymentPortal_Click;
            // 
            // btnOpenPaymentPortal
            // 
            btnOpenPaymentPortal.BackColor = Color.FromArgb(68, 126, 232);
            btnOpenPaymentPortal.Enabled = false;
            btnOpenPaymentPortal.FlatStyle = FlatStyle.Flat;
            btnOpenPaymentPortal.Font = new Font("Segoe UI Semibold", 15F);
            btnOpenPaymentPortal.ForeColor = Color.White;
            btnOpenPaymentPortal.Location = new Point(14, 201);
            btnOpenPaymentPortal.Name = "btnOpenPaymentPortal";
            btnOpenPaymentPortal.Size = new Size(429, 50);
            btnOpenPaymentPortal.TabIndex = 5;
            btnOpenPaymentPortal.Text = "Payment Portal (Coming Soon)";
            btnOpenPaymentPortal.UseVisualStyleBackColor = false;
            btnOpenPaymentPortal.Click += btnOpenPaymentPortal_Click;
            // 
            // btnCopyInstallId
            // 
            btnCopyInstallId.BackColor = Color.FromArgb(28, 36, 52);
            btnCopyInstallId.BackgroundImageLayout = ImageLayout.None;
            btnCopyInstallId.Cursor = Cursors.Hand;
            btnCopyInstallId.FlatStyle = FlatStyle.Flat;
            btnCopyInstallId.Font = new Font("Segoe UI Semibold", 11F);
            btnCopyInstallId.ForeColor = Color.White;
            btnCopyInstallId.Image = Properties.Resources.copy__2_;
            btnCopyInstallId.Location = new Point(670, 161);
            btnCopyInstallId.Name = "btnCopyInstallId";
            btnCopyInstallId.Size = new Size(34, 29);
            btnCopyInstallId.TabIndex = 4;
            btnCopyInstallId.UseVisualStyleBackColor = false;
            btnCopyInstallId.Click += btnCopyInstallId_Click;
            // 
            // txtInstallId
            // 
            txtInstallId.BackColor = Color.FromArgb(12, 14, 22);
            txtInstallId.BorderStyle = BorderStyle.FixedSingle;
            txtInstallId.Enabled = false;
            txtInstallId.Font = new Font("Segoe UI", 12F);
            txtInstallId.ForeColor = Color.Gainsboro;
            txtInstallId.Location = new Point(14, 162);
            txtInstallId.Name = "txtInstallId";
            txtInstallId.Size = new Size(690, 29);
            txtInstallId.TabIndex = 3;
            txtInstallId.TextChanged += txtInstallId_TextChanged;
            // 
            // lblPaymentNote
            // 
            lblPaymentNote.AutoSize = true;
            lblPaymentNote.BackColor = Color.Transparent;
            lblPaymentNote.Font = new Font("Segoe UI", 10F);
            lblPaymentNote.ForeColor = Color.FromArgb(170, 175, 185);
            lblPaymentNote.Location = new Point(14, 256);
            lblPaymentNote.Name = "lblPaymentNote";
            lblPaymentNote.Size = new Size(609, 19);
            lblPaymentNote.TabIndex = 2;
            lblPaymentNote.Text = "The payment gateway has temporarily disabled. Please Press Continue in First web screen You get.";
            // 
            // lblInstallId
            // 
            lblInstallId.AutoSize = true;
            lblInstallId.BackColor = Color.Transparent;
            lblInstallId.Font = new Font("Segoe UI Semibold", 14F);
            lblInstallId.ForeColor = SystemColors.ButtonFace;
            lblInstallId.Location = new Point(11, 130);
            lblInstallId.Name = "lblInstallId";
            lblInstallId.Size = new Size(250, 25);
            lblInstallId.TabIndex = 2;
            lblInstallId.Text = "Install ID / Fingerprint Hash";
            // 
            // pnlPaymentStatus
            // 
            pnlPaymentStatus.BackColor = Color.FromArgb(46, 36, 24);
            pnlPaymentStatus.Controls.Add(lblPaymentStatusTitle);
            pnlPaymentStatus.Controls.Add(lblPaymentStatusSub);
            pnlPaymentStatus.Location = new Point(14, 18);
            pnlPaymentStatus.Name = "pnlPaymentStatus";
            pnlPaymentStatus.Size = new Size(815, 105);
            pnlPaymentStatus.TabIndex = 1;
            // 
            // lblPaymentStatusTitle
            // 
            lblPaymentStatusTitle.AutoSize = true;
            lblPaymentStatusTitle.BackColor = Color.Transparent;
            lblPaymentStatusTitle.Font = new Font("Georgia", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPaymentStatusTitle.ForeColor = Color.FromArgb(192, 192, 0);
            lblPaymentStatusTitle.Location = new Point(26, 23);
            lblPaymentStatusTitle.Name = "lblPaymentStatusTitle";
            lblPaymentStatusTitle.Size = new Size(241, 18);
            lblPaymentStatusTitle.TabIndex = 0;
            lblPaymentStatusTitle.Text = "⚠ Payment Not Verified Yet";
            // 
            // lblPaymentStatusSub
            // 
            lblPaymentStatusSub.AutoSize = true;
            lblPaymentStatusSub.BackColor = Color.Transparent;
            lblPaymentStatusSub.Font = new Font("Georgia", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPaymentStatusSub.ForeColor = Color.FromArgb(192, 192, 0);
            lblPaymentStatusSub.Location = new Point(49, 59);
            lblPaymentStatusSub.Name = "lblPaymentStatusSub";
            lblPaymentStatusSub.Size = new Size(349, 15);
            lblPaymentStatusSub.TabIndex = 0;
            lblPaymentStatusSub.Text = "This device is authorized for one Windows installation only.";
            // 
            // lblnote
            // 
            lblnote.AutoSize = true;
            lblnote.BackColor = Color.Transparent;
            lblnote.Font = new Font("Segoe UI", 10F);
            lblnote.ForeColor = Color.FromArgb(255, 128, 128);
            lblnote.Location = new Point(14, 275);
            lblnote.Name = "lblnote";
            lblnote.Size = new Size(706, 19);
            lblnote.TabIndex = 12;
            lblnote.Text = "Manual verification may take 2 hours to 3 days. Please contact us after purchase if you need immediate activation.";
            // 
            // FrmSettings
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(15, 17, 21);
            ClientSize = new Size(847, 815);
            Controls.Add(pnlContent);
            Controls.Add(pnlTopHeader);
            Font = new Font("Segoe UI", 10F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MaximumSize = new Size(863, 854);
            MinimizeBox = false;
            MinimumSize = new Size(863, 854);
            Name = "FrmSettings";
            StartPosition = FormStartPosition.CenterParent;
            Text = "SxS Optimizer";
            pnlTopHeader.ResumeLayout(false);
            pnlTopHeader.PerformLayout();
            pnlContent.ResumeLayout(false);
            pnlContent.PerformLayout();
            pnlfootersupport.ResumeLayout(false);
            pnlfootersupport.PerformLayout();
            pnlPaymentStatus.ResumeLayout(false);
            pnlPaymentStatus.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel pnlTopHeader;
        private Label lblSubtitle;
        private Label lblMainTitle;
        private Panel pnlContent;
        private Label lblPaymentStatusSub;
        private Label lblPaymentStatusTitle;
        private Panel pnlPaymentStatus;
        private Label lblInstallId;
        private Button btnCopyInstallId;
        private TextBox txtInstallId;
        private Button btnOpenPaymentPortal;
        private Label lblPaymentNote;
        private TextBox txtUsername;
        private Label lblUsername;
        private TextBox textBox2;
        private Label lblConfirmPassword;
        private TextBox txtNewPassword;
        private Label lblNewPassword;
        private Button btnBack;
        private Button btnCompleteRegistration;
        private Panel pnlfootersupport;
        private Label lblSupportNote;
        private Button btnManualActivation;
        private Label lblnote;
    }
}