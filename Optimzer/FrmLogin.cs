using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Optimzer.Core;
using Optimzer.Models;
using Optimzer.Services;
using WinFormsTimer = System.Windows.Forms.Timer;
using System.Windows.Interop;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Optimzer
{
    public partial class FrmLogin : Form
    {
        private readonly ApiService _apiService;
        private readonly DeviceFingerprintService _fingerprintService;
        private readonly WinFormsTimer _internetTimer;

        private string _installHash = string.Empty;
        private string _displayInstallId = string.Empty;

        private bool _hasInternet;
        private bool _isCheckingInternet;
        private bool _isNavigating;
        private bool _isLoginInProgress;
        private bool _isRegisterInProgress;
        private bool _isFormClosing;

        private LicenseInfo _lastKnownLicense = new LicenseInfo();
        private DateTime _lastSuccessfulStateCheckUtc = DateTime.MinValue;
        private CancellationTokenSource? _onlineCheckCts;

        private static readonly TimeSpan InstallStateRefreshWindow = TimeSpan.FromSeconds(30);
        private const int InternetTimerIntervalMs = 15000;
        private const int ConnectivityProbeTimeoutMs = 2500;

        private const string TutorialUrl = "https://youtu.be/kZAGf4c9w0o";

        public FrmLogin()
        {
            InitializeComponent();

            ApplyModernStyle();
            MakeButtonsModern();
            StyleTextBoxes();
            AddFocusEffect();
            AddLoginButtonHoverEffect();

            _apiService = new ApiService();
            _fingerprintService = new DeviceFingerprintService();

            _internetTimer = new WinFormsTimer
            {
                Interval = InternetTimerIntervalMs
            };
            _internetTimer.Tick += InternetTimer_Tick;

            ConfigureForm();
            ConfigureInstallIdentity();
            UpdateLoginButtonState();
        }

        private void ConfigureForm()
        {
            AcceptButton = btnLogin;
            CancelButton = btnExit;

            txtPassword.UseSystemPasswordChar = true;

            SetInternetCheckingState();
            SetRegisterButtonDefaultText();
        }

        private void ConfigureInstallIdentity()
        {
            _installHash = _fingerprintService.GetFullHashedFingerprint();
            _displayInstallId = _fingerprintService.GetDisplayInstallId(_installHash);

            lblDeviceStatus.Text = $"● Install ID: {_displayInstallId}";
            lblDeviceStatus.ForeColor = Color.FromArgb(52, 211, 153);
        }

        private void AddLoginButtonHoverEffect()
        {
            btnLogin.MouseEnter += (_, _) =>
            {
                if (btnLogin.Enabled)
                    btnLogin.BackColor = Color.FromArgb(37, 99, 235);
            };

            btnLogin.MouseLeave += (_, _) =>
            {
                btnLogin.BackColor = Color.FromArgb(59, 130, 246);
            };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (_isFormClosing)
                return;

            Program.BeginUiThreadUpdateCheck(this);

            _internetTimer.Start();
            _ = RefreshOnlineStateAsync(forceInstallStateRefresh: true);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _isFormClosing = true;
            CancelPendingOnlineCheck();
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                _internetTimer.Stop();
                _internetTimer.Tick -= InternetTimer_Tick;
                _internetTimer.Dispose();
            }
            catch
            {
            }

            CancelPendingOnlineCheck();

            base.OnFormClosed(e);
        }

        private async void InternetTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                await RefreshOnlineStateAsync(forceInstallStateRefresh: false);
            }
            catch
            {
                if (!_isFormClosing && !IsDisposed)
                {
                    _hasInternet = false;
                    SetInternetOfflineState();
                    SetRegisterButtonDefaultText();
                    UpdateLoginButtonState();
                }
            }
        }

        private async Task RefreshOnlineStateAsync(bool forceInstallStateRefresh)
        {
            if (_isFormClosing || IsDisposed || Disposing)
                return;

            if (_isCheckingInternet || _isLoginInProgress || _isRegisterInProgress || _isNavigating)
                return;

            _isCheckingInternet = true;

            CancelPendingOnlineCheck();
            _onlineCheckCts = new CancellationTokenSource(ConnectivityProbeTimeoutMs);

            try
            {
                SetInternetCheckingState();

                bool isOnline = await _apiService.HasInternetAsync(_onlineCheckCts.Token);
                _hasInternet = isOnline;

                if (!_hasInternet)
                {
                    SetInternetOfflineState();
                    SetRegisterButtonDefaultText();
                    UpdateLoginButtonState();
                    return;
                }

                SetInternetOnlineState();

                bool shouldRefreshInstallState =
                    forceInstallStateRefresh ||
                    _lastSuccessfulStateCheckUtc == DateTime.MinValue ||
                    (DateTime.UtcNow - _lastSuccessfulStateCheckUtc) >= InstallStateRefreshWindow;

                if (!shouldRefreshInstallState)
                {
                    ApplyInstallStateToUi(_lastKnownLicense);
                    UpdateLoginButtonState();
                    return;
                }

                LicenseInfo state = await _apiService.GetInstallStateAsync(_installHash, _displayInstallId);
                _lastKnownLicense = state?.Clone() ?? new LicenseInfo();
                _lastSuccessfulStateCheckUtc = DateTime.UtcNow;

                ApplyInstallStateToUi(_lastKnownLicense);
                UpdateLoginButtonState();
            }
            catch (OperationCanceledException)
            {
                _hasInternet = false;
                SetInternetOfflineState();
                SetRegisterButtonDefaultText();
                UpdateLoginButtonState();
            }
            catch
            {
                _hasInternet = false;
                SetInternetOfflineState();
                SetRegisterButtonDefaultText();
                UpdateLoginButtonState();
            }
            finally
            {
                _isCheckingInternet = false;
                _onlineCheckCts?.Dispose();
                _onlineCheckCts = null;
            }
        }

        private void CancelPendingOnlineCheck()
        {
            try
            {
                _onlineCheckCts?.Cancel();
            }
            catch
            {
            }
        }

        private void ApplyInstallStateToUi(LicenseInfo? state)
        {
            if (state == null)
            {
                SetRegisterButtonDefaultText();
                return;
            }

            if (state.PaymentVerified && state.AccountConfigured && state.IsActive)
            {
                btnRegister.Text = "Manage Account";

                if (string.IsNullOrWhiteSpace(txtUsername.Text) && !string.IsNullOrWhiteSpace(state.Username))
                    txtUsername.Text = state.Username;
            }
            else if (state.PaymentVerified && !state.AccountConfigured && state.IsActive)
            {
                btnRegister.Text = "Complete Setup";

                if (string.IsNullOrWhiteSpace(txtUsername.Text) && !string.IsNullOrWhiteSpace(state.Username))
                    txtUsername.Text = state.Username;
            }
            else
            {
                SetRegisterButtonDefaultText();
            }
        }

        private void UpdateLoginButtonState()
        {
            bool hasCredentials =
                !string.IsNullOrWhiteSpace(txtUsername.Text) &&
                !string.IsNullOrWhiteSpace(txtPassword.Text);

            btnLogin.Enabled = _hasInternet && hasCredentials && !_isLoginInProgress && !_isRegisterInProgress;
            btnRegister.Enabled = _hasInternet && !_isLoginInProgress && !_isRegisterInProgress;
            btnExit.Enabled = !_isLoginInProgress && !_isRegisterInProgress;
        }

        private bool TryGetLoginCredentials(out string username, out string password)
        {
            username = txtUsername.Text?.Trim() ?? string.Empty;
            password = txtPassword.Text ?? string.Empty;

            if (!_hasInternet)
            {
                ShowWarning(
                    "Internet Required",
                    "Internet is required for login and registration.");
                return false;
            }

            if (username.Length == 0)
            {
                ShowWarning(
                    "Username Required",
                    "Please enter your username.");

                txtUsername.Focus();
                return false;
            }

            // Password is a raw secret value. Check literal length, not whitespace semantics.
            if (password.Length == 0)
            {
                ShowWarning(
                    "Password Required",
                    "Please enter your password.");

                txtPassword.Focus();
                return false;
            }

            return true;
        }

        private void OpenDashboard()
        {
            if (_isNavigating || _isFormClosing)
                return;

            _isNavigating = true;

            try
            {
                _internetTimer.Stop();

                Hide();

                var dashboard = new DashboardWPF();

                // Set WinForms window as owner of WPF window
                var helper = new WindowInteropHelper(dashboard);
                helper.Owner = this.Handle;

                dashboard.ShowDialog();

                if (_isFormClosing || IsDisposed)
                    return;

                _isFormClosing = true;
                Close();
                return;
            }
            finally
            {
                if (!_isFormClosing && !IsDisposed)
                {
                    _internetTimer.Start();
                    UpdateLoginButtonState();
                    _ = RefreshOnlineStateAsync(forceInstallStateRefresh: false);
                }

                _isNavigating = false;
            }
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            if (_isLoginInProgress || _isRegisterInProgress)
                return;

            if (!TryGetLoginCredentials(out string username, out string password))
                return;

            _isLoginInProgress = true;

            _internetTimer.Stop();
            CancelPendingOnlineCheck();

            Cursor = Cursors.WaitCursor;
            btnLogin.Enabled = false;
            btnRegister.Enabled = false;
            btnExit.Enabled = false;

            lblInternetStatus.Text = "● Internet: Verifying login.";
            lblInternetStatus.ForeColor = Color.FromArgb(245, 158, 11);

            try
            {
                LoginRequest request = new LoginRequest
                {
                    Username = username,
                    Password = password,
                    InstallHash = _installHash,
                    DisplayInstallId = _displayInstallId
                };

                Result<LoginResponse> result = await _apiService.LoginAsync(request);

                if (!result.Success)
                {
                    if (result.Message == "Login request timed out.")
                    {
                        ShowWarning(
                            "Login Timeout",
                            "Login verification took too long. Please try again.");
                    }
                    else
                    {
                        ShowWarning(
                            "Login Failed",
                            result.Message);
                    }

                    txtPassword.Focus();
                    txtPassword.SelectAll();
                    return;
                }

                if (result.Data == null || !result.Data.Allowed)
                {
                    ShowWarning(
                        "Login Failed",
                        "The login service denied access for this installation.");

                    txtPassword.Focus();
                    txtPassword.SelectAll();
                    return;
                }

                AppSession.SetAuthenticated(result.Data);
                _lastKnownLicense = result.Data.LicenseInfo?.Clone() ?? new LicenseInfo();
                _lastSuccessfulStateCheckUtc = DateTime.UtcNow;

                OpenDashboard();
            }
            catch (Exception ex)
            {
                ShowError(
                    "Login Error",
                    "An unexpected error occurred while trying to log in.\n\n" + ex.Message);
            }
            finally
            {
                _isLoginInProgress = false;
                Cursor = Cursors.Default;

                if (!_isFormClosing && !IsDisposed)
                {
                    _internetTimer.Start();

                    if (_hasInternet)
                        SetInternetOnlineState();
                    else
                        SetInternetOfflineState();

                    UpdateLoginButtonState();
                    _ = RefreshOnlineStateAsync(forceInstallStateRefresh: false);
                }
            }
        }

        private async void btnRegister_Click(object sender, EventArgs e)
        {
            if (_isRegisterInProgress || _isLoginInProgress || _isNavigating)
                return;

            if (!_hasInternet)
            {
                ShowWarning(
                    "Internet Required",
                    "Internet is required to continue.");
                return;
            }

            _isRegisterInProgress = true;
            _isNavigating = true;
            Cursor = Cursors.WaitCursor;
            UpdateLoginButtonState();

            try
            {
                _internetTimer.Stop();
                Hide();

                using (FrmSettings settings = new FrmSettings(_apiService, _fingerprintService))
                {
                    if (settings.ShowDialog() == DialogResult.OK)
                    {
                        if (!string.IsNullOrWhiteSpace(settings.RegisteredUsername))
                            txtUsername.Text = settings.RegisteredUsername;

                        txtPassword.Clear();
                        txtPassword.Focus();

                        _lastSuccessfulStateCheckUtc = DateTime.MinValue;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(
                    "Registration Error",
                    "An unexpected error occurred while opening account setup.\n\n" + ex.Message);
            }
            finally
            {
                if (!_isFormClosing && !IsDisposed)
                {
                    Show();
                    Activate();
                    _internetTimer.Start();
                    _ = RefreshOnlineStateAsync(forceInstallStateRefresh: true);
                }

                _isRegisterInProgress = false;
                _isNavigating = false;
                Cursor = Cursors.Default;
                UpdateLoginButtonState();
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void txtUsername_TextChanged(object sender, EventArgs e)
        {
            UpdateLoginButtonState();
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            UpdateLoginButtonState();
        }

        private void SetInternetCheckingState()
        {
            lblInternetStatus.Text = "● Internet: Checking...";
            lblInternetStatus.ForeColor = Color.FromArgb(245, 158, 11);
        }

        private void SetInternetOnlineState()
        {
            lblInternetStatus.Text = "● Internet: Online";
            lblInternetStatus.ForeColor = Color.FromArgb(52, 211, 153);
        }

        private void SetInternetOfflineState()
        {
            lblInternetStatus.Text = "● Internet: Offline";
            lblInternetStatus.ForeColor = Color.FromArgb(239, 68, 68);
        }

        private void SetRegisterButtonDefaultText()
        {
            btnRegister.Text = "Register / Buy Access";
        }

        private void ShowWarning(string title, string message)
        {
            if (_isFormClosing || IsDisposed)
                return;

            MessageBox.Show(
                this,
                message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private void ShowError(string title, string message)
        {
            if (_isFormClosing || IsDisposed)
                return;

            MessageBox.Show(
                this,
                message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void btntutorial_Click(object sender, EventArgs e)
        {
            OpenTutorialVideo();
        }

        private void OpenTutorialVideo()
        {
            try
            {
                // Best path: let Windows open the HTTPS link using the default browser.
                Process.Start(new ProcessStartInfo
                {
                    FileName = TutorialUrl,
                    UseShellExecute = true
                });
                return;
            }
            catch
            {
                // Continue to fallbacks.
            }

            try
            {
                // Fallback: ask Explorer/Windows shell again.
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = TutorialUrl,
                    UseShellExecute = true
                });
                return;
            }
            catch
            {
                // Continue to browser-path fallback.
            }

            string[] possibleBrowsers =
            {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),

        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),

        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Mozilla Firefox", "firefox.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Mozilla Firefox", "firefox.exe"),

        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Opera", "opera.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Opera GX", "opera.exe"),

        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware", "Brave-Browser", "Application", "brave.exe")
    };

            string? browserPath = possibleBrowsers.FirstOrDefault(File.Exists);

            if (!string.IsNullOrWhiteSpace(browserPath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = browserPath,
                        Arguments = TutorialUrl,
                        UseShellExecute = true
                    });
                    return;
                }
                catch
                {
                    // Final fallback below.
                }
            }

            var result = MessageBox.Show(
                "No web browser is currently available to open the tutorial video.\r\n\r\n" +
                "Would you like to open Windows Default Apps settings now?",
                "Browser Not Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:defaultapps",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    MessageBox.Show(
                        "Please set a default web browser in Windows Settings and then try again.",
                        "Default Browser Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }
    }
}