using Optimzer.Core;
using Optimzer.Models;
using Optimzer.Services;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Optimzer
{
    public partial class FrmSettings : Form
    {
        private enum SettingsMode
        {
            PaymentRequired = 0,
            PaymentVerifiedPendingSetup = 1,
            PaymentVerifiedAccountExists = 2
        }

        private enum ExternalLaunchTarget
        {
            PaymentPortal = 0,
            ManualActivationWebsite = 1
        }

        private const string ManualActivationWebsiteUrl = "https://sanija-sathnindu.w3spaces.com/SxS-Optimizer/SxS-Optimizer.html";

        private readonly ApiService _apiService;
        private readonly DeviceFingerprintService _fingerprintService;

        private readonly Color _primaryButtonColor = Color.FromArgb(68, 126, 232);
        private readonly Color _disabledButtonColor = Color.FromArgb(45, 50, 60);
        private readonly Color _warningPanelColor = Color.FromArgb(46, 36, 24);
        private readonly Color _warningTextColor = Color.FromArgb(245, 200, 96);
        private readonly Color _successPanelColor = Color.FromArgb(24, 46, 36);
        private readonly Color _successTextColor = Color.FromArgb(52, 211, 153);

        private SettingsMode _mode;
        private bool _busy;
        private bool _isFormClosing;
        private bool _isExternalOpenInProgress;

        private string _installHash = string.Empty;
        private string _displayInstallId = string.Empty;

        public string RegisteredUsername { get; private set; } = string.Empty;

        public FrmSettings()
            : this(new ApiService(), new DeviceFingerprintService())
        {
        }

        public FrmSettings(ApiService apiService, DeviceFingerprintService fingerprintService)
        {
            InitializeComponent();

            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _fingerprintService = fingerprintService ?? throw new ArgumentNullException(nameof(fingerprintService));

            txtInstallId.ReadOnly = true;
            txtNewPassword.UseSystemPasswordChar = true;
            textBox2.UseSystemPasswordChar = true;

            BindRuntimeButtonEvents();
            UpdateActionState();
        }

        private void BindRuntimeButtonEvents()
        {
            btnOpenPaymentPortal.Click -= btnOpenPaymentPortal_Click;
            btnOpenPaymentPortal.Click += btnOpenPaymentPortal_Click;

            btnManualActivation.Click -= btnManualActivation_Click;
            btnManualActivation.Click += btnManualActivation_Click;
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            try
            {
                await LoadStateAsync();
            }
            catch (Exception ex)
            {
                ShowError("Load Failed", ex.Message);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _isFormClosing = true;
            base.OnFormClosing(e);
        }

        private async Task LoadStateAsync()
        {
            if (_busy || _isFormClosing || IsDisposed || Disposing)
                return;

            _busy = true;
            UpdateActionState();

            try
            {
                EnsureInstallIdentityLoaded();

                LicenseInfo info = await _apiService.GetInstallStateAsync(_installHash, _displayInstallId);
                ApplyLicenseInfo(info);
            }
            finally
            {
                _busy = false;
                UpdateActionState();
            }
        }

        private void EnsureInstallIdentityLoaded()
        {
            if (string.IsNullOrEmpty(_installHash))
                _installHash = _fingerprintService.GetFullHashedFingerprint();

            if (string.IsNullOrEmpty(_displayInstallId))
                _displayInstallId = _fingerprintService.GetDisplayInstallId(_installHash);

            txtInstallId.Text = _displayInstallId;
        }

        private void ApplyLicenseInfo(LicenseInfo state)
        {
            if (state == null)
            {
                _mode = SettingsMode.PaymentRequired;
            }
            else if (state.PaymentVerified && state.AccountConfigured && state.IsActive)
            {
                _mode = SettingsMode.PaymentVerifiedAccountExists;
            }
            else if (state.PaymentVerified)
            {
                _mode = SettingsMode.PaymentVerifiedPendingSetup;
            }
            else
            {
                _mode = SettingsMode.PaymentRequired;
            }

            if (!string.IsNullOrWhiteSpace(state?.Username))
                txtUsername.Text = state.Username;

            ApplyModeUi();
            UpdateActionState();
        }

        private void ApplyModeUi()
        {
            if (lblSupportNote != null)
                lblSupportNote.Text = "Use the Install ID above when contacting support about payment or activation issues.";

            switch (_mode)
            {
                case SettingsMode.PaymentRequired:
                    if (lblMainTitle != null) lblMainTitle.Text = "Buy Access & Account Setup";
                    if (lblSubtitle != null) lblSubtitle.Text = "Complete payment to activate this Windows installation.";

                    if (pnlPaymentStatus != null) pnlPaymentStatus.BackColor = _warningPanelColor;
                    if (lblPaymentStatusTitle != null)
                    {
                        lblPaymentStatusTitle.Text = "⚠ Payment Not Verified Yet";
                        lblPaymentStatusTitle.ForeColor = _warningTextColor;
                    }
                    if (lblPaymentStatusSub != null)
                    {
                        lblPaymentStatusSub.Text = "This device is authorized for one Windows installation only.";
                        lblPaymentStatusSub.ForeColor = Color.FromArgb(235, 215, 160);
                    }
                    if (lblPaymentNote != null)
                        lblPaymentNote.Text = "Complete payment first, then return here after verification.";

                    btnOpenPaymentPortal.Visible = true;
                    btnManualActivation.Visible = true;
                    break;

                case SettingsMode.PaymentVerifiedPendingSetup:
                    if (lblMainTitle != null) lblMainTitle.Text = "Complete Account Setup";
                    if (lblSubtitle != null) lblSubtitle.Text = "Payment verified. Create your username and password to activate access.";

                    if (pnlPaymentStatus != null) pnlPaymentStatus.BackColor = _successPanelColor;
                    if (lblPaymentStatusTitle != null)
                    {
                        lblPaymentStatusTitle.Text = "✔ Payment Verified Successfully";
                        lblPaymentStatusTitle.ForeColor = _successTextColor;
                    }
                    if (lblPaymentStatusSub != null)
                    {
                        lblPaymentStatusSub.Text = "This Windows installation is now ready for account setup.";
                        lblPaymentStatusSub.ForeColor = Color.FromArgb(190, 230, 205);
                    }
                    if (lblPaymentNote != null)
                        lblPaymentNote.Text = "Payment already verified for this Windows installation.";

                    btnOpenPaymentPortal.Visible = false;
                    btnManualActivation.Visible = false;
                    break;

                case SettingsMode.PaymentVerifiedAccountExists:
                    if (lblMainTitle != null) lblMainTitle.Text = "Update Account Access";
                    if (lblSubtitle != null) lblSubtitle.Text = "Payment verified. You can update your username or password for this installation.";

                    if (pnlPaymentStatus != null) pnlPaymentStatus.BackColor = _successPanelColor;
                    if (lblPaymentStatusTitle != null)
                    {
                        lblPaymentStatusTitle.Text = "✔ Payment Already Verified";
                        lblPaymentStatusTitle.ForeColor = _successTextColor;
                    }
                    if (lblPaymentStatusSub != null)
                    {
                        lblPaymentStatusSub.Text = "Existing account details can be updated without making another payment.";
                        lblPaymentStatusSub.ForeColor = Color.FromArgb(190, 230, 205);
                    }
                    if (lblPaymentNote != null)
                        lblPaymentNote.Text = "You do not need to pay again for this Windows installation.";

                    btnOpenPaymentPortal.Visible = false;
                    btnManualActivation.Visible = false;
                    break;
            }

            txtInstallId.Text = _displayInstallId;
        }

        private void UpdateActionState()
        {
            bool paymentVerified = _mode != SettingsMode.PaymentRequired;
            bool canSave =
                paymentVerified &&
                !string.IsNullOrWhiteSpace(txtUsername.Text) &&
                !string.IsNullOrWhiteSpace(txtNewPassword.Text) &&
                !string.IsNullOrWhiteSpace(textBox2.Text) &&
                txtNewPassword.Text == textBox2.Text &&
                !_busy &&
                !_isExternalOpenInProgress;

            btnCompleteRegistration.Enabled = canSave;
            btnCompleteRegistration.BackColor = canSave ? _primaryButtonColor : _disabledButtonColor;
            btnBack.Enabled = !_busy && !_isExternalOpenInProgress;
            btnCopyInstallId.Enabled = !_busy && !_isExternalOpenInProgress;

            btnOpenPaymentPortal.Enabled = false;
            btnOpenPaymentPortal.BackColor = _disabledButtonColor;

            btnManualActivation.Enabled =
                btnManualActivation.Visible &&
                !_busy &&
                !_isExternalOpenInProgress;
        }

        private async void btnOpenPaymentPortal_Click(object? sender, EventArgs e)
        {
            await OpenExternalPageAsync(ExternalLaunchTarget.PaymentPortal);
        }

        private async void btnManualActivation_Click(object? sender, EventArgs e)
        {
            await OpenExternalPageAsync(ExternalLaunchTarget.ManualActivationWebsite);
        }

        private async Task OpenExternalPageAsync(ExternalLaunchTarget target)
        {
            if (_busy || _isExternalOpenInProgress || _isFormClosing || IsDisposed || Disposing)
                return;

            if (target == ExternalLaunchTarget.PaymentPortal)
                return;

            _isExternalOpenInProgress = true;
            UpdateActionState();

            try
            {
                EnsureInstallIdentityLoaded();

                string url = ManualActivationWebsiteUrl + "?installId=" + Uri.EscapeDataString(_displayInstallId);

                Process? process = Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true,
                    ErrorDialog = false
                });

                process?.Dispose();
            }
            catch (Exception ex)
            {
                ShowError("Error", ex.Message);
            }
            finally
            {
                _isExternalOpenInProgress = false;
                UpdateActionState();
            }

            await Task.CompletedTask;
        }

        private bool ValidateInputs()
        {
            if (_mode == SettingsMode.PaymentRequired)
            {
                ShowWarning("Payment Required", "Payment is not verified yet. Please complete payment first.");
                return false;
            }

            string username = txtUsername.Text.Trim();
            string password = txtNewPassword.Text;
            string confirmPassword = textBox2.Text;

            if (!IsValidUsername(username))
            {
                ShowWarning("Invalid Username", "Username must be 4-32 characters and contain only letters, numbers, '.', '_' or '-'.");
                txtUsername.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowWarning("Password Required", "Please enter a password.");
                txtNewPassword.Focus();
                return false;
            }

            if (password.Length < 8)
            {
                ShowWarning("Weak Password", "Password must be at least 8 characters long.");
                txtNewPassword.Focus();
                return false;
            }

            if (password.Length > 128)
            {
                ShowWarning("Invalid Password", "Password is too long.");
                txtNewPassword.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                ShowWarning("Confirm Password", "Please confirm your password.");
                textBox2.Focus();
                return false;
            }

            if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                ShowWarning("Password Mismatch", "Password and confirm password do not match.");
                textBox2.Focus();
                return false;
            }

            return true;
        }

        private static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            if (username.Length < 4 || username.Length > 32)
                return false;

            foreach (char ch in username)
            {
                if (char.IsLetterOrDigit(ch))
                    continue;

                if (ch == '.' || ch == '_' || ch == '-')
                    continue;

                return false;
            }

            return true;
        }

        private async void btnCompleteRegistration_Click(object? sender, EventArgs e)
        {
            if (_busy)
                return;

            if (!ValidateInputs())
                return;

            _busy = true;
            UpdateActionState();

            try
            {
                EnsureInstallIdentityLoaded();

                Result<LicenseInfo> result = await _apiService.CompleteOrUpdateAccountAsync(
                    _installHash,
                    _displayInstallId,
                    txtUsername.Text.Trim(),
                    txtNewPassword.Text);

                if (!result.Success)
                {
                    ShowWarning("Failed", result.Message);
                    return;
                }

                RegisteredUsername = txtUsername.Text.Trim();

                ShowInfo("Success", "Account saved.");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ShowError("Error", ex.Message);
            }
            finally
            {
                _busy = false;
                UpdateActionState();
            }
        }

        private void btnBack_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnCopyInstallId_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txtInstallId.Text))
                    Clipboard.SetText(txtInstallId.Text);
            }
            catch
            {
            }
        }

        private void txtInstallId_TextChanged(object? sender, EventArgs e)
        {
        }

        private void txtUsername_TextChanged(object? sender, EventArgs e)
        {
            UpdateActionState();
        }

        private void txtNewPassword_TextChanged(object? sender, EventArgs e)
        {
            UpdateActionState();
        }

        private void textBox2_TextChanged(object? sender, EventArgs e)
        {
            UpdateActionState();
        }

        private void ShowWarning(string title, string message)
        {
            if (_isFormClosing || IsDisposed)
                return;

            MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ShowError(string title, string message)
        {
            if (_isFormClosing || IsDisposed)
                return;

            MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowInfo(string title, string message)
        {
            if (_isFormClosing || IsDisposed)
                return;

            MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}