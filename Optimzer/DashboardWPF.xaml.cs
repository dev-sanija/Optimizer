
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using Optimzer.Services;

using WinFormsApplication = System.Windows.Forms.Application;

using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfClipboard = System.Windows.Clipboard;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfApplication = System.Windows.Application;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;

namespace Optimzer
{
    public partial class DashboardWPF : Window
    {
        private const string BlueStacksConfigPath = @"C:\ProgramData\BlueStacks_nxt\bluestacks.conf";
        private const string MsiAppPlayerConfigPath = @"C:\ProgramData\BlueStacks_msi5\bluestacks.conf";
        private const string GameLoopInstallDir = @"C:\Program Files\TxGameAssistant";
        private const string GameLoopTempPrimary = @"C:\Temp";
        private const string GameLoopTempSecondary = @"D:\Temp";
        private const string LogoRelativePath = @"Resources\gear.ico";

        private readonly Dictionary<string, FrameworkElement> _pages;
        private readonly Dictionary<string, WpfButton> _navButtons;
        private readonly WpfBrush _defaultNavBackground;
        private readonly WpfBrush _defaultNavBorder;
        private readonly WpfBrush _activeNavBackground;
        private readonly WpfBrush _activeNavBorder;
        private readonly ApiService _apiService;
        private readonly StringBuilder _logBuilder = new StringBuilder();
        private readonly StringBuilder _tweakActionLogBuilder = new StringBuilder();
        private readonly Dictionary<string, TweakDefinition> _tweakLookup = new Dictionary<string, TweakDefinition>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, RegistryToggleBackup> _registryBackups = new Dictionary<string, RegistryToggleBackup>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _profilePrimaryPathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _profileSecondaryPathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private List<TweakDefinition> _allTweaks = new List<TweakDefinition>();
        private string _mainScriptUrl = string.Empty;
        private bool _useCustomScriptSource;
        private bool _uiReady;
        private bool _isShuttingDown;
        private string _selectedPreset = "Universal";
        private string _selectedFilter = "All";
        private string _detectedFocus = "Universal";

        [DllImport("psapi.dll")]
        static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        static extern bool SetSystemFileCacheSize(IntPtr MinimumFileCacheSize, IntPtr MaximumFileCacheSize, int Flags);

        [DllImport("ntdll.dll")]
        static extern int NtSetSystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint TimeEndPeriod(uint uMilliseconds);
        // ------------------------------
        public DashboardWPF()
        {
            InitializeComponent();

            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }

            _apiService = new ApiService();

            _pages = new Dictionary<string, FrameworkElement>
            {
                { "Dashboard", PageDashboard },
                { "Runner", PageRunner },
                { "Tweaks", PageTweaks },
                { "Apps", PageApps },
                { "Logs", PageLogs },
                { "Settings", PageSettings }
            };

            _navButtons = new Dictionary<string, WpfButton>
            {
                { "Dashboard", BtnNavDashboard },
                { "Runner", BtnNavRunner },
                { "Tweaks", BtnNavTweaks },
                { "Apps", BtnNavApps },
                { "Logs", BtnNavLogs },
                { "Settings", BtnNavSettings }
            };

            _defaultNavBackground = WpfBrushes.Transparent;
            _defaultNavBorder = WpfBrushes.Transparent;
            _activeNavBackground = (WpfBrush)FindResource("ActiveNavBrush");
            _activeNavBorder = (WpfBrush)FindResource("AccentBrush");

            Loaded += DashboardWPF_Loaded;
            Closing += DashboardWPF_Closing;
            Closed += DashboardWPF_Closed;

            NavigateTo("Dashboard");
        }

        private async void DashboardWPF_Loaded(object? sender, RoutedEventArgs e)
        {
            LoadPortableIcon();
            InitializePresetSelector();
            InitializeProfileFooter();
            InitializeTweaks();
            AppendLog("Dashboard initialized.");

            _uiReady = true;
            UpdateRunnerSourceUi();

            await RefreshDashboardStateAsync().ConfigureAwait(true);
            RenderTweakCards();
        }

        private void DashboardWPF_Closing(object? sender, CancelEventArgs e)
        {
            if (_isShuttingDown)
                return;

            _isShuttingDown = true;
            DisposeRuntimeResources();
        }

        private void DashboardWPF_Closed(object? sender, EventArgs e)
        {
            try
            {
                // Only cleanup this window, DO NOT shut down app
                Loaded -= DashboardWPF_Loaded;
                Closing -= DashboardWPF_Closing;
                Closed -= DashboardWPF_Closed;
            }
            catch { }
        }

        private void DisposeRuntimeResources()
        {
            try
            {
                Loaded -= DashboardWPF_Loaded;
                Closing -= DashboardWPF_Closing;
                Closed -= DashboardWPF_Closed;
            }
            catch { }

            try
            {
                ImgSidebarLogo.Source = null;
                Icon = null;
            }
            catch { }

            try
            {
                TxtRunnerOutput.Clear();
                TxtLogs.Clear();
                TxtDashboardPreviewLog.Clear();
                TxtRunnerCommand.Clear();
                TxtTweakActionLog.Clear();
            }
            catch { }

            try
            {
                _logBuilder.Clear();
                _tweakActionLogBuilder.Clear();
                _tweakLookup.Clear();
                _registryBackups.Clear();
                _allTweaks.Clear();
                _pages.Clear();
                _navButtons.Clear();
            }
            catch { }

            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch { }
        }

        private void InitializePresetSelector()
        {
            CmbTweakPreset.SelectedIndex = 0;
            HighlightFilterButton(BtnFilterAll);
        }

        private void InitializeProfileFooter()
        {
            CmbProfileFps.SelectedIndex = 3;
            ChkProfileHighPriority.IsChecked = true;
            ChkProfileRoot.IsChecked = true;
            ChkProfileReadOnly.IsChecked = true;
            UpdateProfileFooter();
        }

        private void InitializeTweaks()
        {
            _allTweaks = BuildTweakDefinitions();
            _tweakLookup.Clear();

            foreach (TweakDefinition tweak in _allTweaks)
            {
                _tweakLookup[tweak.Id] = tweak;
            }
        }

        private List<TweakDefinition> BuildTweakDefinitions()
        {
            List<TweakDefinition> list = new List<TweakDefinition>();

            list.Add(ActionTweak("uni.temp", "Universal", "Temp Clean", "Clean common temporary files without restarting Explorer.", "Recommended", "Live", "Safe", "run_temp_clean"));
            list.Add(ActionTweak("uni.deep", "Universal", "Deep Temp Clean", "Clean temp, prefetch, delivery cache and safe transient folders.", "Recommended", "Live", "Safe", "run_deep_temp_clean"));
            list.Add(ActionTweak("uni.shader", "Universal", "Shader Cache Clean", "Clean DirectX and vendor shader caches.", "Recommended", "Game restart", "Safe", "run_shader_cache_clean"));
            list.Add(ActionTweak("uni.dns", "Universal", "DNS Flush", "Flush DNS resolver cache.", "Recommended", "Live", "Safe", "run_dns_flush"));
            list.Add(ActionTweak("uni.ram", "Universal", "RAM Clean", "Trim process working sets without restarting Explorer.", "Recommended", "Live", "Safe", "run_ram_trim"));
            list.Add(ActionTweak("uni.graphics", "Universal", "Open Graphics Settings", "Open advanced graphics settings for GPU preference tuning.", "Recommended", "Live", "Safe", "open_graphics_settings"));
            list.Add(ActionTweak("uni.startup", "Universal", "Open Startup Apps", "Open startup apps settings for cleanup.", "Recommended", "Live", "Safe", "open_startup_apps"));
            list.Add(ActionTweak("uni.gamemode", "Universal", "Open Game Mode", "Open Windows gaming settings and Game Mode controls.", "Recommended", "Live", "Safe", "open_game_mode"));
            list.Add(ActionTweak("uni.overlays", "Universal", "Kill Common Overlays", "Stop common overlay processes that may inject into games.", "Recommended", "Game restart", "Safe", "kill_overlays"));
            list.Add(ActionTweak("uni.highperf", "Universal", "Set High Performance Plan", "Switch to a high performance power plan.", "Recommended", "Live", "Safe", "set_high_perf_plan"));
            list.Add(ActionTweak("uni.balanced", "Universal", "Set Balanced Plan", "Return to a balanced power plan.", "Advanced", "Live", "Safe", "set_balanced_plan"));
            list.Add(ToggleTweak("uni.gamedvr", "Universal", "Disable Game DVR", "Toggle Game DVR related capture settings.", "Recommended", "PC restart", "Aggressive", "toggle_game_dvr", ToggleKind.GameDvr));
            list.Add(ToggleTweak("uni.hags", "Universal", "Toggle HAGS", "Toggle Hardware-Accelerated GPU Scheduling.", "Advanced", "PC restart", "Experimental", "toggle_hags", ToggleKind.Hags));
            list.Add(ActionTweak("uni.restore", "Universal", "Create Restore Point", "Create a restore point before aggressive changes.", "Advanced", "Live", "Safe", "create_restore_point"));

            AddEmulatorPreset(list, "BlueStacks", "bluestacks_only");
            AddEmulatorPreset(list, "MSI", "msi_only");

            list.Add(ActionTweak("gl.detect", "GameLoop", "Detect GameLoop", "Detect GameLoop install and temp locations.", "Recommended", "Live", "Safe", "detect_gameloop"));
            list.Add(ActionTweak("gl.openinstall", "GameLoop", "Open Install Folder", "Open GameLoop installation folder.", "Recommended", "Live", "Safe", "open_gameloop_install"));
            list.Add(ActionTweak("gl.opentemp", "GameLoop", "Open Temp Folder", "Open GameLoop temp folder if present.", "Recommended", "Live", "Safe", "open_gameloop_temp"));
            list.Add(ActionTweak("gl.priority", "GameLoop", "Boost Emulator Priority", "Set GameLoop emulator processes to high priority.", "Recommended", "Live", "Safe", "set_gameloop_priority"));
            list.Add(ActionTweak("gl.temp", "GameLoop", "Temp Clean", "Run temp clean for a lighter GameLoop session.", "Recommended", "Live", "Safe", "run_temp_clean"));
            list.Add(ActionTweak("gl.deep", "GameLoop", "Deep Temp Clean", "Run deeper cleanup before launching GameLoop.", "Recommended", "Live", "Safe", "run_deep_temp_clean"));
            list.Add(ActionTweak("gl.overlays", "GameLoop", "Kill Common Overlays", "Stop common overlays before launching GameLoop.", "Recommended", "Game restart", "Safe", "kill_overlays"));
            list.Add(ActionTweak("gl.graphics", "GameLoop", "Open Graphics Settings", "Open Windows graphics settings for GameLoop testing.", "Recommended", "Live", "Safe", "open_graphics_settings"));
            list.Add(ActionTweak("gl.highperf", "GameLoop", "Set High Performance Plan", "Switch to a high performance power plan before launch.", "Recommended", "Live", "Safe", "set_high_perf_plan"));
            list.Add(NoteTweak("gl.note", "GameLoop", "Config Safety Note", "GameLoop uses a different local structure than BlueStacks/MSI, so this preset keeps actions to detection, folders and safe launch support.", "Experimental", "Manual", "Experimental"));
            list.Add(ActionTweak("gl.dns", "GameLoop", "DNS Flush", "Flush DNS before reconnect testing.", "Advanced", "Live", "Safe", "run_dns_flush"));

            AddPcPreset(list, "CS2", "cs2.exe", "CS2: Test autoexec, launch options, overlays and fullscreen behavior one by one.", @"steam\userdata", true);
            AddPcPreset(list, "Valorant", "VALORANT-Win64-Shipping.exe", "Valorant: Test Raw Input Buffer, overlays and startup clutter. Keep Vanguard-related security items manual.", @"VALORANT\Saved\Config", false);
            AddPcPreset(list, "Fortnite", "FortniteClient-Win64-Shipping.exe", "Fortnite: Performance Mode and render path changes happen in-game and need a game restart.", @"FortniteGame\Saved\Config\WindowsClient", false);
            list.Add(NoteTweak("fort.note", "Fortnite", "Performance Mode Note", "Fortnite Performance Mode is an in-game setting and needs a game restart after change.", "Recommended", "Game restart", "Safe"));
            AddPcPreset(list, "GTA V", "GTA5.exe", "GTA V: Test settings.xml carefully, keep overlays light and validate GPU preference.", @"Documents\Rockstar Games\GTA V", false);
            AddPcPreset(list, "Roblox PC", "RobloxPlayerBeta.exe", "Roblox: Use the built-in Maximum Framerate setting in the client settings menu.", @"AppData\Local\Roblox", false);
            list.Add(NoteTweak("rbx.note", "Roblox PC", "Max Framerate Note", "Modern Roblox includes a built-in Maximum Framerate setting in the client settings menu.", "Recommended", "Live", "Safe"));

            return list;
        }

        private void AddEmulatorPreset(List<TweakDefinition> list, string preset, string actionPrefix)
        {
            string lower = preset.Equals("MSI", StringComparison.OrdinalIgnoreCase) ? "msi" : "bluestacks";
            list.Add(ActionTweak($"{lower}.detect", preset, "Detect Config", $"Detect the {preset} config and related state.", "Recommended", "Live", "Safe", $"detect_{actionPrefix}"));
            list.Add(ActionTweak($"{lower}.openconfig", preset, "Open Config File", $"Open the main {preset} config file location.", "Recommended", "Live", "Safe", $"open_{actionPrefix}_config"));
            list.Add(ActionTweak($"{lower}.openfolder", preset, "Open Config Folder", $"Open the {preset} config folder.", "Recommended", "Live", "Safe", $"open_{actionPrefix}_folder"));
            list.Add(ActionTweak($"{lower}.priority", preset, "Boost HD-Player Priority", "Set HD-Player.exe to high priority when running.", "Recommended", "Live", "Safe", "set_hd_priority"));
            list.Add(ActionTweak($"{lower}.apply", preset, "Apply Profile", "Apply the footer profile using FPS, root, priority and read-only settings.", "Recommended", "Restart emulator", "Safe", $"apply_{actionPrefix}_profile"));
            list.Add(ToggleTweak($"{lower}.readonly", preset, "Set Read-Only", "Force the config file into read-only mode.", "Recommended", "Restart emulator", "Safe", $"toggle_{actionPrefix}_readonly", ToggleKind.EmulatorReadOnly));
            list.Add(ToggleTweak($"{lower}.highfps", preset, "Enable High FPS Flag", "Write a high FPS flag into the emulator config.", "Recommended", "Restart emulator", "Safe", $"toggle_{actionPrefix}_highfps", ToggleKind.EmulatorHighFps));
            list.Add(ToggleTweak($"{lower}.root", preset, "Enable Root Access Flag", "Write root-related flags into the emulator config.", "Advanced", "Restart emulator", "Aggressive", $"toggle_{actionPrefix}_root", ToggleKind.EmulatorRoot));
            list.Add(ActionTweak($"{lower}.unlock", preset, "Unlock Config", "Remove read-only from the config file before edits.", "Recommended", "Live", "Safe", $"unlock_{actionPrefix}_config"));
            list.Add(ActionTweak($"{lower}.graphics", preset, "Open Graphics Settings", "Open Windows graphics settings for emulator tuning.", "Recommended", "Live", "Safe", "open_graphics_settings"));
            list.Add(ActionTweak($"{lower}.temp", preset, "Temp Clean", "Run temp clean before launching the emulator.", "Recommended", "Live", "Safe", "run_temp_clean"));
            list.Add(ActionTweak($"{lower}.power", preset, "High Performance Plan", "Switch to a high performance power plan before a session.", "Recommended", "Live", "Safe", "set_high_perf_plan"));
        }

        private void AddPcPreset(List<TweakDefinition> list, string preset, string exeName, string guidanceText, string openHint, bool includeAutoexec)
        {
            string key = preset.ToLowerInvariant().Replace(" ", string.Empty);
            list.Add(ActionTweak($"{key}.priority", preset, "Set High Priority", $"Set {exeName} to high priority when running.", "Recommended", "Live", "Safe", $"priority|{exeName}"));
            list.Add(ActionTweak($"{key}.graphics", preset, "Open Graphics Settings", "Open Windows graphics settings to set GPU preference.", "Recommended", "Live", "Safe", "open_graphics_settings"));
            list.Add(ActionTweak($"{key}.overlays", preset, "Kill Common Overlays", "Stop common overlay processes before launch.", "Recommended", "Game restart", "Safe", "kill_overlays"));
            list.Add(ActionTweak($"{key}.temp", preset, "Temp Clean", "Clean temp folders before a session.", "Recommended", "Live", "Safe", "run_temp_clean"));
            list.Add(ActionTweak($"{key}.shader", preset, "Shader Cache Clean", "Clean shader caches before retesting stutter.", "Recommended", "Game restart", "Safe", "run_shader_cache_clean"));
            list.Add(ActionTweak($"{key}.startup", preset, "Open Startup Apps", "Open startup apps settings to reduce clutter.", "Recommended", "Live", "Safe", "open_startup_apps"));
            list.Add(ActionTweak($"{key}.gaming", preset, "Open Gaming Settings", "Open Windows gaming settings.", "Recommended", "Live", "Safe", "open_game_mode"));
            list.Add(ActionTweak($"{key}.power", preset, "High Performance Plan", "Switch to a high performance power plan.", "Recommended", "Live", "Safe", "set_high_perf_plan"));
            list.Add(ToggleTweak($"{key}.hags", preset, "Toggle HAGS", "Toggle Hardware-Accelerated GPU Scheduling for test passes.", "Advanced", "PC restart", "Experimental", "toggle_hags", ToggleKind.Hags));
            list.Add(ActionTweak($"{key}.guidance", preset, "Copy Guidance Note", guidanceText, "Recommended", "Live", "Safe", $"copy_note|{preset}"));
            list.Add(ActionTweak($"{key}.openhint", preset, "Open Helper Path", $"Open the most relevant config or helper folder for {preset} if found.", "Recommended", "Live", "Safe", $"open_helper|{preset}|{openHint}"));
            if (includeAutoexec)
            {
                list.Add(ActionTweak($"{key}.autoexec", preset, "Copy Autoexec Template", "Copy a simple autoexec template for testing.", "Advanced", "Live", "Safe", "copy_cs2_autoexec"));
                list.Add(ToggleTweak($"{key}.fullscreenopt", preset, "Disable Fullscreen Optimizations", "Toggle fullscreen optimizations on the executable when the path is known.", "Advanced", "Game restart", "Aggressive", $"toggle_fso|{exeName}", ToggleKind.FullscreenOptimizations));
            }
            else
            {
                list.Add(ToggleTweak($"{key}.gamedvr", preset, "Disable Game DVR", "Toggle Game DVR related capture settings while testing.", "Advanced", "PC restart", "Aggressive", "toggle_game_dvr", ToggleKind.GameDvr));
            }
        }

        private TweakDefinition ActionTweak(string id, string preset, string title, string description, string filter, string restartBadge, string riskBadge, string actionId)
        {
            return new TweakDefinition
            {
                Id = id,
                Preset = preset,
                Title = title,
                Description = description,
                Filter = filter,
                RestartBadge = restartBadge,
                RiskBadge = riskBadge,
                CardKind = TweakCardKind.Action,
                ActionId = actionId
            };
        }

        private TweakDefinition ToggleTweak(string id, string preset, string title, string description, string filter, string restartBadge, string riskBadge, string actionId, ToggleKind toggleKind)
        {
            return new TweakDefinition
            {
                Id = id,
                Preset = preset,
                Title = title,
                Description = description,
                Filter = filter,
                RestartBadge = restartBadge,
                RiskBadge = riskBadge,
                CardKind = TweakCardKind.Toggle,
                ActionId = actionId,
                ToggleKind = toggleKind
            };
        }

        private void BtnOpenAtlas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var atlasWin = new AtlasOSWindow
                {
                    Owner = this
                };

                atlasWin.Closed += (s, args) =>
                {
                    this.Show();
                    this.Activate();
                    AppendLog("Returned to Dashboard.");
                };

                
                atlasWin.Show();

                AppendLog("Atlas OS window opened.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }


        private TweakDefinition NoteTweak(string id, string preset, string title, string description, string filter, string restartBadge, string riskBadge)
        {
            return new TweakDefinition
            {
                Id = id,
                Preset = preset,
                Title = title,
                Description = description,
                Filter = filter,
                RestartBadge = restartBadge,
                RiskBadge = riskBadge,
                CardKind = TweakCardKind.Note
            };
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WpfButton button && button.Tag is string pageKey)
            {
                NavigateTo(pageKey);
            }
        }

        private void QuickNavigate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WpfButton button && button.Tag is string pageKey)
            {
                NavigateTo(pageKey);
                AppendLog("Navigated to " + pageKey + ".");
            }
        }

        private void NavigateTo(string pageKey)
        {
            foreach (FrameworkElement page in _pages.Values)
            {
                page.Visibility = Visibility.Collapsed;
            }

            if (_pages.TryGetValue(pageKey, out FrameworkElement? activePage))
            {
                activePage.Visibility = Visibility.Visible;
            }

            foreach (KeyValuePair<string, WpfButton> item in _navButtons)
            {
                item.Value.Background = _defaultNavBackground;
                item.Value.BorderBrush = _defaultNavBorder;
            }

            if (_navButtons.TryGetValue(pageKey, out WpfButton? activeButton))
            {
                activeButton.Background = _activeNavBackground;
                activeButton.BorderBrush = _activeNavBorder;
            }

            switch (pageKey)
            {
                case "Dashboard":
                    TxtPageTitle.Text = "Dashboard";
                    TxtPageSubtitle.Text = "Quick actions for low-end gaming PCs, MSI and BlueStacks users.";
                    break;
                case "Runner":
                    TxtPageTitle.Text = "Script Runner";
                    TxtPageSubtitle.Text = "Hidden optimizer source plus optional custom script import.";
                    break;
                case "Tweaks":
                    TxtPageTitle.Text = "Tweaks";
                    TxtPageSubtitle.Text = "Preset-based dynamic tweak cards with filters and live status.";
                    break;
                case "Apps":
                    TxtPageTitle.Text = "Apps";
                    TxtPageSubtitle.Text = "Category-based app queue with simple bundles.";
                    break;
                case "Logs":
                    TxtPageTitle.Text = "Logs";
                    TxtPageSubtitle.Text = "Readable activity stream and exportable history.";
                    break;
                case "Settings":
                    TxtPageTitle.Text = "Settings";
                    TxtPageSubtitle.Text = "Interface toggles and source control.";
                    break;
            }
        }

        private async Task RefreshDashboardStateAsync()
        {
            UpdateDeviceSummary();
            UpdateEnvironmentSummary();
            await LoadMainScriptUrlAsync().ConfigureAwait(true);
            await UpdateInternetStateAsync().ConfigureAwait(true);
            UpdateProfileFooter();
        }

        private async Task LoadMainScriptUrlAsync()
        {
            object result = await _apiService.GetMainScriptUrlAsync().ConfigureAwait(true);
            bool isSuccess = GetResultSucceeded(result);
            string url = GetResultStringValue(result);
            string message = GetResultMessage(result);

            if (isSuccess && !string.IsNullOrWhiteSpace(url))
            {
                _mainScriptUrl = url.Trim();
                TxtScriptStatus.Text = "Internal optimizer source ready";
                AppendRunnerOutput("Optimizer source refreshed.");
            }
            else
            {
                _mainScriptUrl = string.Empty;
                TxtScriptStatus.Text = "Internal optimizer source unavailable";
                AppendRunnerOutput(string.IsNullOrWhiteSpace(message) ? "Optimizer source is unavailable." : message);
                AppendLog(string.IsNullOrWhiteSpace(message) ? "Optimizer source is unavailable." : message);
            }

            UpdateRunnerSourceUi();
        }

        private async Task UpdateInternetStateAsync()
        {
            bool hasInternet = await _apiService.HasInternetAsync().ConfigureAwait(true);
            TxtInternet.Text = hasInternet ? "Online" : "Offline";
            TxtInternetChip.Text = hasInternet ? "Internet: Online" : "Internet: Offline";
            TxtInternetChip.Foreground = CreateBrush(hasInternet ? "#4ADE80" : "#FCA5A5");
        }

        private void UpdateRunnerSourceUi()
        {
            string modeText = _useCustomScriptSource ? "Custom Source" : "Optimizer Source";
            string optimizerStatus = string.IsNullOrWhiteSpace(_mainScriptUrl) ? "Unavailable" : "Ready";
            string preview = _useCustomScriptSource
                ? BuildCommandPreview(TxtRunnerCustomScriptUrl.Text?.Trim() ?? string.Empty)
                : string.IsNullOrWhiteSpace(_mainScriptUrl) ? "Optimizer source unavailable." : "irm '<internal optimizer source>' | iex";

            TxtRunnerSourceMode.Text = modeText;
            TxtRunnerOptimizerStatus.Text = optimizerStatus;
            TxtSettingsSourceMode.Text = modeText;
            TxtSettingsOptimizerStatus.Text = optimizerStatus;
            TxtRunnerCommand.Text = preview;
        }

        private string BuildCommandPreview(string url)
        {
            return string.IsNullOrWhiteSpace(url) ? "No custom script URL selected." : $"irm '{url}' | iex";
        }

        private string? GetActiveScriptUrl()
        {
            if (_useCustomScriptSource)
            {
                string custom = TxtRunnerCustomScriptUrl.Text?.Trim() ?? string.Empty;
                return IsValidHttpUrl(custom) ? custom : null;
            }

            return string.IsNullOrWhiteSpace(_mainScriptUrl) ? null : _mainScriptUrl;
        }

        private static bool IsValidHttpUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) || uri == null)
                return false;

            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        private async void BtnRefreshOverview_Click(object sender, RoutedEventArgs e)
        {
            AppendLog("Refreshing dashboard state...");
            await RefreshDashboardStateAsync().ConfigureAwait(true);
            RenderTweakCards();
            AppendLog("Dashboard state refreshed.");
        }

        private async void BtnRunnerRefreshOptimizer_Click(object sender, RoutedEventArgs e)
        {
            AppendLog("Refreshing optimizer source...");
            await LoadMainScriptUrlAsync().ConfigureAwait(true);
            AppendLog("Optimizer source refresh completed.");
        }

        private void BtnRunnerUseOptimizerSource_Click(object sender, RoutedEventArgs e)
        {
            _useCustomScriptSource = false;
            UpdateRunnerSourceUi();
            AppendLog("Runner switched to internal optimizer source.");
        }

        private void BtnRunnerUseCustomSource_Click(object sender, RoutedEventArgs e)
        {
            string customUrl = TxtRunnerCustomScriptUrl.Text?.Trim() ?? string.Empty;
            if (!IsValidHttpUrl(customUrl))
            {
                AppendLog("Custom script URL is invalid.");
                return;
            }

            _useCustomScriptSource = true;
            UpdateRunnerSourceUi();
            AppendLog("Runner switched to custom script source.");
        }

        private void BtnRunnerClearCustom_Click(object sender, RoutedEventArgs e)
        {
            TxtRunnerCustomScriptUrl.Text = string.Empty;
            _useCustomScriptSource = false;
            UpdateRunnerSourceUi();
            AppendLog("Custom script source cleared. Optimizer source restored.");
        }

        private async void BtnRunMainScript_Click(object sender, RoutedEventArgs e)
        {
            if (!_useCustomScriptSource && string.IsNullOrWhiteSpace(_mainScriptUrl))
            {
                await LoadMainScriptUrlAsync().ConfigureAwait(true);
            }

            string? activeUrl = GetActiveScriptUrl();
            if (string.IsNullOrWhiteSpace(activeUrl))
            {
                AppendLog("No runnable script source is selected.");
                return;
            }

            string command = $"irm '{activeUrl}' | iex";
            bool launched = LaunchPowerShellCommand(command, false, true);

            if (launched)
            {
                AppendRunnerOutput(_useCustomScriptSource ? "Custom script launched." : "Optimizer script launched.");
                AppendLog(_useCustomScriptSource ? "Custom script launch requested." : "Optimizer script launch requested.");
            }
            else
            {
                AppendRunnerOutput("Script launch was cancelled or failed.");
                AppendLog("Script launch failed.");
            }
        }

        private void BtnCopyRunnerCommand_Click(object sender, RoutedEventArgs e)
        {
            if (_useCustomScriptSource)
            {
                string customUrl = TxtRunnerCustomScriptUrl.Text?.Trim() ?? string.Empty;
                if (!IsValidHttpUrl(customUrl))
                {
                    AppendLog("No valid custom command is available to copy.");
                    return;
                }

                WpfClipboard.SetText($"irm '{customUrl}' | iex");
                AppendLog("Custom runner command copied to clipboard.");
                return;
            }

            AppendLog("Internal optimizer source is hidden and cannot be copied.");
        }

        private void UpdateDeviceSummary()
        {
            string osName = GetOsDisplayName();
            string cpuName = GetCpuNameFromRegistry();
            string ramText = GetTotalRamDisplay();
            string archText = $"{(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")} / {Environment.ProcessorCount} logical cores";
            bool isAdmin = IsAdministrator();
            string versionText = GetAppVersion();

            TxtOsName.Text = osName;
            TxtCpu.Text = cpuName;
            TxtRam.Text = ramText;
            TxtArch.Text = archText;
            TxtMachine.Text = Environment.MachineName;
            TxtUser.Text = Environment.UserName;
            TxtAdminStatus.Text = isAdmin ? "Yes" : "No";
            TxtAdminChip.Text = isAdmin ? "Admin: Yes" : "Admin: No";
            TxtSidebarDevice.Text = Environment.MachineName;
            TxtSidebarVersion.Text = versionText;

            if (osName.Contains("Windows 11", StringComparison.OrdinalIgnoreCase))
            {
                TxtSupportHint.Text = "Windows 11 detected. Keep shell tweaks modern and conservative.";
            }
            else if (osName.Contains("Windows 10", StringComparison.OrdinalIgnoreCase))
            {
                TxtSupportHint.Text = "Windows 10 detected. Prefer classic-compatible tweaks and careful service changes.";
            }
            else
            {
                TxtSupportHint.Text = "Unknown Windows edition. Review tweak impact before applying.";
            }
        }

        private void UpdateEnvironmentSummary()
        {
            bool blueExists = File.Exists(BlueStacksConfigPath);
            bool msiExists = File.Exists(MsiAppPlayerConfigPath);
            bool gameloopInstalled = Directory.Exists(GameLoopInstallDir);

            TxtBlueStacksConfigPath.Text = blueExists ? BlueStacksConfigPath : "Not found";
            TxtMsiConfigPath.Text = msiExists ? MsiAppPlayerConfigPath : "Not found";
            TxtGameLoopState.Text = gameloopInstalled ? GameLoopInstallDir : "Not found";
            TxtEmulatorStatus.Text = (blueExists || msiExists) ? "BlueStacks / MSI config detected" : gameloopInstalled ? "GameLoop detected" : "No supported config found";
            TxtConfigReadOnlyState.Text = GetReadOnlySummary(blueExists, msiExists);
            TxtSelectedPresetStatus.Text = $"Dashboard FPS {GetSelectedProfileFps()} / Priority {(ChkProfileHighPriority.IsChecked == true ? "On" : "Off")}";
            TxtRunningProcesses.Text = BuildRunningProcessSummary();
            _detectedFocus = DetectBestFocus();
            TxtDetectedTarget.Text = _detectedFocus;
            TxtSidebarDetectedFocus.Text = _detectedFocus;
            TxtTweakDetectedTarget.Text = _detectedFocus;
            TxtTweakRunningNow.Text = TxtRunningProcesses.Text;
            TxtTweakRecommendedProfile.Text = _detectedFocus;
            TxtTweakNotice.Text = BuildNoticeText(_selectedPreset);
        }

        private string BuildRunningProcessSummary()
        {
            List<string> hits = new List<string>();
            string[] interesting =
            {
                "HD-Player",
                "AndroidEmulatorEx",
                "AndroidEmulatorEn",
                "AndroidEmulator",
                "cs2",
                "VALORANT-Win64-Shipping",
                "FortniteClient-Win64-Shipping",
                "GTA5",
                "RobloxPlayerBeta"
            };

            foreach (string name in interesting)
            {
                int count = Process.GetProcessesByName(name).Length;
                if (count > 0)
                {
                    hits.Add($"{name} ({count})");
                }
            }

            return hits.Count == 0 ? "No tracked processes running" : string.Join(", ", hits);
        }

        private string DetectBestFocus()
        {
            if (Process.GetProcessesByName("cs2").Length > 0)
                return "CS2";
            if (Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
                return "Valorant";
            if (Process.GetProcessesByName("FortniteClient-Win64-Shipping").Length > 0)
                return "Fortnite";
            if (Process.GetProcessesByName("GTA5").Length > 0)
                return "GTA V";
            if (Process.GetProcessesByName("RobloxPlayerBeta").Length > 0)
                return "Roblox PC";
            if (Process.GetProcessesByName("HD-Player").Length > 0)
                return File.Exists(MsiAppPlayerConfigPath) ? "MSI" : "BlueStacks";
            if (Directory.Exists(GameLoopInstallDir) || Process.GetProcessesByName("AndroidEmulatorEx").Length > 0 || Process.GetProcessesByName("AndroidEmulatorEn").Length > 0)
                return "GameLoop";
            return "Universal";
        }

        private string BuildNoticeText(string preset)
        {
            switch (preset)
            {
                case "BlueStacks":
                case "MSI":
                    return "Root and FPS flags are written first, then read-only is applied if selected.";
                case "GameLoop":
                    return "GameLoop is handled as detection, folders and safe launch support only.";
                case "Fortnite":
                    return "Performance Mode is changed in-game and needs a game restart.";
                case "Roblox PC":
                    return "Use Roblox's built-in Maximum Framerate setting in client settings.";
                default:
                    return "Cards show live, restart-game, restart-PC or manual badges per tweak.";
            }
        }

        private void LoadPortableIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogoRelativePath);
                if (!File.Exists(iconPath))
                    return;

                Uri iconUri = new Uri(iconPath, UriKind.Absolute);
                BitmapFrame frame = BitmapFrame.Create(iconUri);
                Icon = frame;
                ImgSidebarLogo.Source = frame;
                ImgSidebarLogo.Visibility = Visibility.Visible;
                TxtLogoFallback.Visibility = Visibility.Collapsed;
            }
            catch
            {
            }
        }

        private void CmbTweakPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_uiReady)
                return;

            _selectedPreset = GetSelectedComboText(CmbTweakPreset, "Universal");
            UpdateProfileFooter();
            RenderTweakCards();
        }

        private void FilterChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not WpfButton button || button.Tag is not string filter)
                return;

            _selectedFilter = filter;
            HighlightFilterButton(button);
            RenderTweakCards();
        }

        private void HighlightFilterButton(WpfButton active)
        {
            WpfButton[] buttons = { BtnFilterAll, BtnFilterRecommended, BtnFilterAdvanced, BtnFilterExperimental };
            foreach (WpfButton button in buttons)
            {
                button.Background = CreateBrush("#122036");
                button.BorderBrush = (WpfBrush)FindResource("StrokeBrush");
            }

            active.Background = _activeNavBackground;
            active.BorderBrush = _activeNavBorder;
        }

        private void RenderTweakCards()
        {
            if (TweakCardHost == null)
                return;

            TweakCardHost.Children.Clear();

            IEnumerable<TweakDefinition> source = _allTweaks.Where(t => t.Preset.Equals(_selectedPreset, StringComparison.OrdinalIgnoreCase));
            if (!_selectedFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                source = source.Where(t => t.Filter.Equals(_selectedFilter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (TweakDefinition tweak in source)
            {
                TweakCardHost.Children.Add(BuildTweakCard(tweak));
            }

            TxtTweakPresetFocus.Text = _selectedPreset;
            TxtTweakNotice.Text = BuildNoticeText(_selectedPreset);
        }

        private UIElement BuildTweakCard(TweakDefinition tweak)
        {
            Border card = new Border
            {
                Width = 340,
                Margin = new Thickness(0, 0, 14, 14),
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(16),
                BorderBrush = (WpfBrush)FindResource("StrokeBrush"),
                BorderThickness = new Thickness(1),
                Background = (WpfBrush)FindResource("SurfaceBrush2")
            };

            StackPanel root = new StackPanel();

            root.Children.Add(new TextBlock
            {
                Text = tweak.Title,
                Foreground = (WpfBrush)FindResource("TextBrush"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6)
            });

            root.Children.Add(new TextBlock
            {
                Text = tweak.Description,
                Foreground = (WpfBrush)FindResource("MutedTextBrush"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            WrapPanel badges = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            badges.Children.Add(CreateBadge(tweak.Filter, GetFilterBrushKey(tweak.Filter)));
            badges.Children.Add(CreateBadge(tweak.RestartBadge, "ChipBrush"));
            badges.Children.Add(CreateBadge(tweak.RiskBadge, GetRiskBrushKey(tweak.RiskBadge)));
            root.Children.Add(badges);

            switch (tweak.CardKind)
            {
                case TweakCardKind.Action:
                    WpfButton actionButton = new WpfButton
                    {
                        Content = "Run",
                        Tag = tweak.Id,
                        Style = (Style)FindResource("PrimaryButtonStyle"),
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    actionButton.Click += TweakActionButton_Click;
                    root.Children.Add(actionButton);
                    break;

                case TweakCardKind.Toggle:
                    bool isChecked = QueryToggleState(tweak);
                    WpfCheckBox toggle = new WpfCheckBox
                    {
                        Content = isChecked ? "Enabled" : "Disabled",
                        Tag = tweak.Id,
                        IsChecked = isChecked,
                        Style = (Style)FindResource("PlainCheckStyle")
                    };
                    toggle.Checked += TweakToggle_Changed;
                    toggle.Unchecked += TweakToggle_Changed;
                    root.Children.Add(toggle);
                    break;

                case TweakCardKind.Note:
                    root.Children.Add(new TextBlock
                    {
                        Text = "Guidance only",
                        Foreground = CreateBrush("#93C5FD"),
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 4, 0, 0)
                    });
                    break;
            }

            card.Child = root;
            return card;
        }

        private Border CreateBadge(string text, string brushKey)
        {
            return new Border
            {
                Background = (WpfBrush)FindResource(brushKey),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 8, 6),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = (WpfBrush)FindResource("TextBrush"),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold
                }
            };
        }

        private string GetFilterBrushKey(string filter)
        {
            return filter switch
            {
                "Recommended" => "RecommendedBrush",
                "Advanced" => "AdvancedBrush",
                "Experimental" => "ExperimentalBrush",
                _ => "ChipBrush"
            };
        }

        private string GetRiskBrushKey(string risk)
        {
            return risk switch
            {
                "Safe" => "RecommendedBrush",
                "Aggressive" => "AdvancedBrush",
                "Experimental" => "ExperimentalBrush",
                _ => "ChipBrush"
            };
        }

        private void TweakActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not WpfButton button || button.Tag is not string tweakId || !_tweakLookup.TryGetValue(tweakId, out TweakDefinition? tweak))
                return;

            ExecuteTweakAction(tweak);
        }

        private void TweakToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is not WpfCheckBox checkBox || checkBox.Tag is not string tweakId || !_tweakLookup.TryGetValue(tweakId, out TweakDefinition? tweak))
                return;

            bool desired = checkBox.IsChecked == true;
            ApplyToggleTweak(tweak, desired);
            checkBox.Content = desired ? "Enabled" : "Disabled";
        }

        private void UpdateProfileFooter()
        {
            TxtProfilePresetTitle.Text = _selectedPreset + " Profile";
            TxtProfilePresetDescription.Text = BuildProfileDescription(_selectedPreset);
            TxtProfileTarget.Text = BuildProfileTarget(_selectedPreset);
            TxtTweakPresetFocus.Text = _selectedPreset;

            bool emulatorPreset = IsEmulatorPreset(_selectedPreset);
            ChkProfileRoot.Visibility = emulatorPreset ? Visibility.Visible : Visibility.Collapsed;
            ChkProfileReadOnly.Visibility = emulatorPreset ? Visibility.Visible : Visibility.Collapsed;

            ChkProfileHighPriority.Content = emulatorPreset ? "Boost HD-Player priority" : "Boost game priority";
            BtnProfileApply.Content = emulatorPreset ? "Apply Emulator Profile" : "Apply Profile";

            PopulateProfilePaths();
        }

        private string BuildProfileDescription(string preset)
        {
            return preset switch
            {
                "BlueStacks" => "Apply FPS, root, high priority and read-only behavior to the BlueStacks config.",
                "MSI" => "Apply FPS, root, high priority and read-only behavior to the MSI App Player config.",
                "GameLoop" => "Use safe GameLoop helper actions. Config writing is not forced here.",
                "CS2" => "Use quick profile apply for process priority, power plan and helper paths.",
                "Valorant" => "Use live priority apply and helper paths while keeping security-sensitive items manual.",
                "Fortnite" => "Use live priority apply and in-game render mode guidance.",
                "GTA V" => "Use live priority apply and helper paths for settings and cleanup.",
                "Roblox PC" => "Use live priority apply and built-in framerate guidance.",
                _ => "Universal profile controls are mainly for cleanup, power and quick settings actions."
            };
        }

        private string BuildProfileTarget(string preset)
        {
            return preset switch
            {
                "BlueStacks" => BlueStacksConfigPath,
                "MSI" => MsiAppPlayerConfigPath,
                "GameLoop" => Directory.Exists(GameLoopInstallDir) ? GameLoopInstallDir : GameLoopTempPrimary,
                "CS2" => "cs2.exe",
                "Valorant" => "VALORANT-Win64-Shipping.exe",
                "Fortnite" => "FortniteClient-Win64-Shipping.exe",
                "GTA V" => "GTA5.exe",
                "Roblox PC" => "RobloxPlayerBeta.exe",
                _ => "Universal Windows quick actions"
            };
        }

        private void PopulateProfilePaths()
        {
            _profilePrimaryPathMap.Clear();
            _profileSecondaryPathMap.Clear();

            _profilePrimaryPathMap["BlueStacks"] = BlueStacksConfigPath;
            _profileSecondaryPathMap["BlueStacks"] = Path.GetDirectoryName(BlueStacksConfigPath) ?? string.Empty;
            _profilePrimaryPathMap["MSI"] = MsiAppPlayerConfigPath;
            _profileSecondaryPathMap["MSI"] = Path.GetDirectoryName(MsiAppPlayerConfigPath) ?? string.Empty;
            _profilePrimaryPathMap["GameLoop"] = GameLoopInstallDir;
            _profileSecondaryPathMap["GameLoop"] = Directory.Exists(GameLoopTempPrimary) ? GameLoopTempPrimary : GameLoopTempSecondary;
            _profilePrimaryPathMap["CS2"] = BuildSteamCfgFolder();
            _profileSecondaryPathMap["CS2"] = "steam://open/settings";
            _profilePrimaryPathMap["Valorant"] = BuildFolderFromLocalAppData(@"VALORANT\Saved\Config");
            _profileSecondaryPathMap["Valorant"] = "ms-settings:gaming";
            _profilePrimaryPathMap["Fortnite"] = BuildFolderFromLocalAppData(@"FortniteGame\Saved\Config\WindowsClient");
            _profileSecondaryPathMap["Fortnite"] = "ms-settings:display-advancedgraphics";
            _profilePrimaryPathMap["GTA V"] = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Rockstar Games\GTA V");
            _profileSecondaryPathMap["GTA V"] = "ms-settings:display-advancedgraphics";
            _profilePrimaryPathMap["Roblox PC"] = BuildFolderFromLocalAppData(@"Roblox");
            _profileSecondaryPathMap["Roblox PC"] = "ms-settings:display-advancedgraphics";
            _profilePrimaryPathMap["Universal"] = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _profileSecondaryPathMap["Universal"] = "ms-settings:gaming";
        }

        private void BtnProfileApply_Click(object sender, RoutedEventArgs e)
        {
            if (IsEmulatorPreset(_selectedPreset))
            {
                ApplySelectedFooterEmulatorProfile();
                return;
            }

            switch (_selectedPreset)
            {
                case "CS2":
                    ApplyPriorityForProcessName("cs2.exe");
                    SetHighPerformancePowerPlan();
                    break;
                case "Valorant":
                    ApplyPriorityForProcessName("VALORANT-Win64-Shipping.exe");
                    SetHighPerformancePowerPlan();
                    break;
                case "Fortnite":
                    ApplyPriorityForProcessName("FortniteClient-Win64-Shipping.exe");
                    SetHighPerformancePowerPlan();
                    break;
                case "GTA V":
                    ApplyPriorityForProcessName("GTA5.exe");
                    SetHighPerformancePowerPlan();
                    break;
                case "Roblox PC":
                    ApplyPriorityForProcessName("RobloxPlayerBeta.exe");
                    SetHighPerformancePowerPlan();
                    break;
                default:
                    SetHighPerformancePowerPlan();
                    break;
            }

            UpdateEnvironmentSummary();
            AppendTweakAction($"Applied profile footer for {_selectedPreset}.");
        }

        private void BtnProfileOpenPrimary_Click(object sender, RoutedEventArgs e)
        {
            if (_profilePrimaryPathMap.TryGetValue(_selectedPreset, out string? value) && !string.IsNullOrWhiteSpace(value))
            {
                OpenPathOrUri(value);
            }
        }

        private void BtnProfileOpenSecondary_Click(object sender, RoutedEventArgs e)
        {
            if (_profileSecondaryPathMap.TryGetValue(_selectedPreset, out string? value) && !string.IsNullOrWhiteSpace(value))
            {
                OpenPathOrUri(value);
            }
        }

        private void ExecuteTweakAction(TweakDefinition tweak)
        {
            string action = tweak.ActionId;

            switch (action)
            {
                case "run_temp_clean":
                    RunTempClean(false);
                    break;
                case "run_deep_temp_clean":
                    RunTempClean(true);
                    break;
                case "run_shader_cache_clean":
                    RunShaderCacheClean();
                    break;
                case "run_dns_flush":
                    RunDnsFlush();
                    break;
                case "run_ram_trim":
                    RunRamTrim();
                    break;
                case "open_graphics_settings":
                    OpenGraphicsSettings();
                    break;
                case "open_startup_apps":
                    OpenStartupApps();
                    break;
                case "open_game_mode":
                    OpenGameModeSettings();
                    break;
                case "kill_overlays":
                    KillCommonOverlays();
                    break;
                case "set_high_perf_plan":
                    SetHighPerformancePowerPlan();
                    break;
                case "set_balanced_plan":
                    SetBalancedPowerPlan();
                    break;
                case "create_restore_point":
                    CreateRestorePoint();
                    break;
                case "open_bluestacks_only_config":
                    OpenProfilePrimaryConfig(BlueStacksConfigPath);
                    break;
                case "open_msi_only_config":
                    OpenProfilePrimaryConfig(MsiAppPlayerConfigPath);
                    break;
                case "open_bluestacks_only_folder":
                    OpenProfilePrimaryConfigFolder(BlueStacksConfigPath);
                    break;
                case "open_msi_only_folder":
                    OpenProfilePrimaryConfigFolder(MsiAppPlayerConfigPath);
                    break;
                case "detect_bluestacks_only":
                case "detect_msi_only":
                    UpdateEnvironmentSummary();
                    AppendTweakAction($"Detected state refreshed for {_selectedPreset}.");
                    break;
                case "set_hd_priority":
                    SetHdPlayerPriority();
                    break;
                case "apply_bluestacks_only_profile":
                case "apply_msi_only_profile":
                    ApplySelectedFooterEmulatorProfile();
                    break;
                case "unlock_bluestacks_only_config":
                    UnlockConfig(BlueStacksConfigPath, "BlueStacks config unlocked.");
                    break;
                case "unlock_msi_only_config":
                    UnlockConfig(MsiAppPlayerConfigPath, "MSI config unlocked.");
                    break;
                case "detect_gameloop":
                    UpdateEnvironmentSummary();
                    AppendTweakAction("GameLoop detection refreshed.");
                    break;
                case "open_gameloop_install":
                    OpenPathOrUri(GameLoopInstallDir);
                    break;
                case "open_gameloop_temp":
                    OpenPathOrUri(Directory.Exists(GameLoopTempPrimary) ? GameLoopTempPrimary : GameLoopTempSecondary);
                    break;
                case "set_gameloop_priority":
                    SetPriorityForAny(new[] { "AndroidEmulatorEx.exe", "AndroidEmulatorEn.exe", "AndroidEmulator.exe", "aow_exe.exe" }, "GameLoop emulator priority applied.");
                    break;
                case "copy_cs2_autoexec":
                    CopyCs2AutoexecTemplate();
                    break;
                default:
                    if (action.StartsWith("priority|", StringComparison.OrdinalIgnoreCase))
                    {
                        ApplyPriorityForProcessName(action.Substring("priority|".Length));
                    }
                    else if (action.StartsWith("copy_note|", StringComparison.OrdinalIgnoreCase))
                    {
                        CopyPresetGuidanceNote(action.Substring("copy_note|".Length));
                    }
                    else if (action.StartsWith("open_helper|", StringComparison.OrdinalIgnoreCase))
                    {
                        OpenHelperPath(action);
                    }
                    else
                    {
                        AppendTweakAction($"No action handler was found for {tweak.Title}.");
                    }
                    break;
            }

            UpdateEnvironmentSummary();
        }

        private void ApplyToggleTweak(TweakDefinition tweak, bool desired)
        {
            switch (tweak.ToggleKind)
            {
                case ToggleKind.GameDvr:
                    SetGameDvr(desired);
                    break;
                case ToggleKind.Hags:
                    SetHags(desired);
                    break;
                case ToggleKind.EmulatorReadOnly:
                    SetSelectedPresetReadOnly(desired);
                    break;
                case ToggleKind.EmulatorHighFps:
                    SetSelectedEmulatorFlag("enable_high_fps", desired ? "1" : "0");
                    break;
                case ToggleKind.EmulatorRoot:
                    SetSelectedEmulatorFlag("enable_root_access", desired ? "1" : "0");
                    break;
                case ToggleKind.FullscreenOptimizations:
                    SetFullscreenOptimizationNote(tweak, desired);
                    break;
            }

            UpdateEnvironmentSummary();
        }

        private bool QueryToggleState(TweakDefinition tweak)
        {
            return tweak.ToggleKind switch
            {
                ToggleKind.GameDvr => QueryGameDvrDisabled(),
                ToggleKind.Hags => QueryHagsEnabled(),
                ToggleKind.EmulatorReadOnly => QuerySelectedPresetReadOnly(),
                ToggleKind.EmulatorHighFps => QuerySelectedEmulatorConfigContains("enable_high_fps", "1"),
                ToggleKind.EmulatorRoot => QuerySelectedEmulatorConfigContains("enable_root_access", "1") || QuerySelectedEmulatorConfigContains("bst.feature.rooting", "1"),
                _ => false
            };
        }

        private void BtnApplyEmulator240_Click(object sender, RoutedEventArgs e)
        {
            string[] configPaths =
            {
        BlueStacksConfigPath,
        MsiAppPlayerConfigPath
    };

            bool success = false;

            try
            {
                foreach (string path in configPaths)
                {
                    if (!File.Exists(path))
                        continue;

                    // Remove ReadOnly if enabled
                    FileAttributes attr = File.GetAttributes(path);
                    if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
                    }

                    string[] lines = File.ReadAllLines(path);

                    // Detect all instance names dynamically
                    HashSet<string> instances = new HashSet<string>();

                    foreach (string line in lines)
                    {
                        Match match = Regex.Match(line, @"bst\.instance\.([^.]+)\.");
                        if (match.Success)
                        {
                            instances.Add(match.Groups[1].Value);
                        }
                    }

                    List<string> updatedLines = new List<string>();

                    foreach (string line in lines)
                    {
                        string newLine = line;

                        // Global values
                        if (line.StartsWith("bst.feature.rooting="))
                            newLine = "bst.feature.rooting=\"1\"";

                        else if (line.StartsWith("bst.mim.max_fps="))
                            newLine = "bst.mim.max_fps=\"240\"";

                        // Dynamic instance values
                        foreach (string instance in instances)
                        {
                            if (line.StartsWith($"bst.instance.{instance}.enable_root_access="))
                                newLine = $"bst.instance.{instance}.enable_root_access=\"1\"";

                            else if (line.StartsWith($"bst.instance.{instance}.max_fps="))
                                newLine = $"bst.instance.{instance}.max_fps=\"240\"";

                            else if (line.StartsWith($"bst.instance.{instance}.enable_vsync="))
                                newLine = $"bst.instance.{instance}.enable_vsync=\"0\"";

                            else if (line.StartsWith($"bst.instance.{instance}.enable_high_fps="))
                                newLine = $"bst.instance.{instance}.enable_high_fps=\"1\"";

                            else if (line.StartsWith($"bst.instance.{instance}.enable_fps_display="))
                                newLine = $"bst.instance.{instance}.enable_fps_display=\"1\"";
                        }

                        updatedLines.Add(newLine);
                    }

                    // Add missing values if not found
                    if (!updatedLines.Any(x => x.StartsWith("bst.feature.rooting=")))
                        updatedLines.Add("bst.feature.rooting=\"1\"");

                    if (!updatedLines.Any(x => x.StartsWith("bst.mim.max_fps=")))
                        updatedLines.Add("bst.mim.max_fps=\"240\"");

                    foreach (string instance in instances)
                    {
                        if (!updatedLines.Any(x => x.StartsWith($"bst.instance.{instance}.enable_root_access=")))
                            updatedLines.Add($"bst.instance.{instance}.enable_root_access=\"1\"");

                        if (!updatedLines.Any(x => x.StartsWith($"bst.instance.{instance}.max_fps=")))
                            updatedLines.Add($"bst.instance.{instance}.max_fps=\"240\"");

                        if (!updatedLines.Any(x => x.StartsWith($"bst.instance.{instance}.enable_vsync=")))
                            updatedLines.Add($"bst.instance.{instance}.enable_vsync=\"0\"");

                        if (!updatedLines.Any(x => x.StartsWith($"bst.instance.{instance}.enable_high_fps=")))
                            updatedLines.Add($"bst.instance.{instance}.enable_high_fps=\"1\"");

                        if (!updatedLines.Any(x => x.StartsWith($"bst.instance.{instance}.enable_fps_display=")))
                            updatedLines.Add($"bst.instance.{instance}.enable_fps_display=\"1\"");
                    }

                    File.WriteAllLines(path, updatedLines);

                    // Set back to ReadOnly
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);

                    success = true;
                }

                if (success)
                {
                    AppendLog("240 FPS Emulator Profile Applied Successfully.");

                    System.Windows.MessageBox.Show(
                        "240 FPS Config Applied Successfully!\n\nAll detected instances were optimized and locked.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "BlueStacks / MSI App Player config file not found.",
                        "Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Error: " + ex.Message,
                    "Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ApplyAdvancedEmulatorTweaks(string filePath)
        {
            try
            {
                // 1. File Usage Check: Prevent crash if emulator is open
                try
                {
                    using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    { stream.Close(); }
                }
                catch (IOException)
                {
                    AppendLog($"ERROR: {Path.GetFileName(filePath)} is in use. Please close the emulator first.");
                    return;
                }

                // 2. Remove Read-Only attribute
                FileAttributes attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
                }

                // 3. Process Content
                string[] lines = File.ReadAllLines(filePath);
                bool mimFound = false;
                bool rootFeatureFound = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    // Global Keys
                    if (lines[i].StartsWith("bst.mim.max_fps=")) { lines[i] = "bst.mim.max_fps=\"240\""; mimFound = true; }
                    if (lines[i].StartsWith("bst.feature.rooting=")) { lines[i] = "bst.feature.rooting=\"1\""; rootFeatureFound = true; }

                    // Instance Keys (Regex matches any instance like Pie64, Nougat64, etc.)
                    if (lines[i].Contains(".max_fps=")) lines[i] = Regex.Replace(lines[i], @"max_fps="".*?""", "max_fps=\"240\"");
                    if (lines[i].Contains(".enable_fps_display=")) lines[i] = Regex.Replace(lines[i], @"enable_fps_display="".*?""", "enable_fps_display=\"1\"");
                    if (lines[i].Contains(".enable_root_access=")) lines[i] = Regex.Replace(lines[i], @"enable_root_access="".*?""", "enable_root_access=\"1\"");
                }

                // 4. Fallback: If global keys didn't exist in the file, add them
                List<string> finalLines = new List<string>(lines);
                if (!mimFound) finalLines.Add("bst.mim.max_fps=\"240\"");
                if (!rootFeatureFound) finalLines.Add("bst.feature.rooting=\"1\"");

                File.WriteAllLines(filePath, finalLines);

                // 5. Re-enable Read-Only automatically
                File.SetAttributes(filePath, FileAttributes.ReadOnly);

                AppendLog($"Successfully patched and locked {Path.GetFileName(filePath)}");
            }
            catch (UnauthorizedAccessException)
            {
                AppendLog("CRITICAL: Access Denied. Run as Administrator.");
                System.Windows.MessageBox.Show(
                    "CRITICAL: Access Denied. Run as Administrator.",
                    "Access Denied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error); 
            }
            catch (Exception ex)
            {
                AppendLog($"Error patching {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        private void BtnApplySelectedEmulatorProfile_Click(object sender, RoutedEventArgs e)
        {
            ApplySelectedFooterEmulatorProfile();
        }

 
        private void BtnHdHighPriority_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetHdPlayerPriority();
                MakeTimerPermanent();
                SetPermanentGamePriority();

                // Extra Registry Force
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(
                    @"SYSTEM\CurrentControlSet\Control\PriorityControl"))
                {
                    key.SetValue("Win32PrioritySeparation", 38, RegistryValueKind.DWord);
                }

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\HD-Player.exe\PerfOptions"))
                {
                    key.SetValue("CpuPriorityClass", 3, RegistryValueKind.DWord);
                    key.SetValue("IoPriority", 2, RegistryValueKind.DWord);
                }

                foreach (Process p in Process.GetProcessesByName("HD-Player"))
                {
                    try
                    {
                        p.PriorityClass = ProcessPriorityClass.High;
                        p.ProcessorAffinity = (IntPtr)0xFFC;
                    }
                    catch { }
                }

                   System.Windows.MessageBox.Show(
                    "Kernel-Level High Priority Applied Successfully!\n\nEmulator permanently forced to max performance.",
                    "Optimization Applied",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error applying priority: {ex.Message}",
                    "Registry Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private void BtnOpenBlueStacksConfig_Click(object sender, RoutedEventArgs e)
        {
            OpenPathInExplorer(BlueStacksConfigPath);
        }

        private void BtnOpenMsiConfig_Click(object sender, RoutedEventArgs e)
        {
            OpenPathInExplorer(MsiAppPlayerConfigPath);
        }


        private void BtnUnlockEmulatorConfigs_Click(object sender, RoutedEventArgs e)
        {
            UnlockConfig(BlueStacksConfigPath, "BlueStacks config unlocked.");
            UnlockConfig(MsiAppPlayerConfigPath, "MSI config unlocked.");
            UpdateEnvironmentSummary();
        }

        private void BtnRelockEmulatorConfigs_Click(object sender, RoutedEventArgs e)
        {
            SetReadOnlyState(BlueStacksConfigPath, true, "BlueStacks config set to read-only.");
            SetReadOnlyState(MsiAppPlayerConfigPath, true, "MSI config set to read-only.");
            UpdateEnvironmentSummary();
        }

        private void BtnTempClean_Click(object sender, RoutedEventArgs e)
        {
            RunTempClean(false);
        }

        private void BtnDeepTempClean_Click(object sender, RoutedEventArgs e)
        {
            RunTempClean(true);
        }

        private void BtnShaderCacheClean_Click(object sender, RoutedEventArgs e)
        {
            RunShaderCacheClean();
        }

        private void BtnRamTrim_Click(object sender, RoutedEventArgs e)
        {
            RunRamTrim();
        }

        private void BtnDnsFlush_Click(object sender, RoutedEventArgs e)
        {
            RunDnsFlush();
        }

        private void BtnKillOverlays_Click(object sender, RoutedEventArgs e)
        {
            KillCommonOverlays();
        }

        private void BtnOpenGraphicsSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenGraphicsSettings();
        }

        private void BtnDisableStartupApps_Click(object sender, RoutedEventArgs e)
        {
            DisableStartupApps();
        }

        private void DisableStartupApps()
        {
            try
            {
                string[] runKeys =
                {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"
        };

                foreach (string keyPath in runKeys)
                {
                    using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, true))
                    {
                        if (key != null)
                        {
                            foreach (string valueName in key.GetValueNames())
                            {
                                try
                                {
                                    key.DeleteValue(valueName, false);
                                }
                                catch { }
                            }
                        }
                    }

                    string currentUserPath = keyPath.Replace("SOFTWARE\\", "Software\\");

                    using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(currentUserPath, true))
                    {
                        if (key != null)
                        {
                            foreach (string valueName in key.GetValueNames())
                            {
                                try
                                {
                                    key.DeleteValue(valueName, false);
                                }
                                catch { }
                            }
                        }
                    }
                }

                AppendLog("Startup apps disabled successfully.");
                AppendTweakAction("Startup apps disabled.");

                System.Windows.MessageBox.Show(
                    "Startup apps have been disabled successfully.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppendLog("Failed to disable startup apps.");
                AppendTweakAction("Failed to disable startup apps.");

                System.Windows.MessageBox.Show(
                    "Error disabling startup apps:\n" + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnDisableDiffender_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult warning = System.Windows.MessageBox.Show(
                "Disabling Windows Defender can expose your PC to malware, ransomware, and other threats.\n\nDo you want to continue?",
                "Security Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (warning != MessageBoxResult.Yes)
                return;

            MessageBoxResult tamper = System.Windows.MessageBox.Show(
                "Before continuing:\n\n" +
                "1. Open Windows Security\n" +
                "2. Go to Virus & threat protection\n" +
                "3. Open Manage settings\n" +
                "4. Turn OFF Tamper Protection\n\n" +
                "Have you turned Tamper Protection OFF?",
                "Action Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (tamper != MessageBoxResult.Yes)
            {
                AppendLog("Defender disable cancelled. Tamper Protection still enabled.");

                System.Windows.MessageBox.Show(
                    "Operation cancelled.\nTamper Protection must be OFF first.",
                    "Cancelled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                DisableDefenderPermanently();

                AppendLog("Windows Defender registry disable applied.");

                MessageBoxResult restart = System.Windows.MessageBox.Show(
                    "Windows Defender settings were changed successfully.\n\nA restart is required.\n\nRestart now?",
                    "Restart Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (restart == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "-r -t 0",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                }
                else
                {
                    AppendLog("Restart postponed by user.");
                }
            }
            catch (Exception ex)
            {
                AppendLog("Failed to disable Windows Defender.");

                System.Windows.MessageBox.Show(
                    "Error:\n" + ex.Message,
                    "Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DisableDefenderPermanently()
        {
            // These keys require Admin privileges
            string policyPath = @"SOFTWARE\Policies\Microsoft\Windows Defender";

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(policyPath))
            {
                key.SetValue("DisableAntiSpyware", 1, RegistryValueKind.DWord);
                key.SetValue("DisableAntiVirus", 1, RegistryValueKind.DWord);
            }

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(policyPath + @"\Real-Time Protection"))
            {
                key.SetValue("DisableRealtimeMonitoring", 1, RegistryValueKind.DWord);
            }

            AppendLog("Registry values for Defender set to 'Disabled'.");
        }

        private void BtnEnableDiffender_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show(
                "This will re-enable Windows Defender protection.\n\nDo you want to proceed?",
                "Restore Security",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                EnableDefenderRegistry();

                AppendLog("Windows Defender re-enabled in Registry.");

                MessageBoxResult restart = System.Windows.MessageBox.Show(
                    "Defender has been re-enabled!\n\nYou MUST restart for Windows to restart the security services.\n\nRestart now?",
                    "Restart Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (restart == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "shutdown.exe",
                        Arguments = "-r -t 0",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    });
                }
            }
            catch (Exception ex)
            {
                AppendLog("Failed to enable Defender.");
                System.Windows.MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableDefenderRegistry()
        {
            string policyPath = @"SOFTWARE\Policies\Microsoft\Windows Defender";

            // We use OpenSubKey with 'true' to allow writing/deleting
            using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(policyPath, true))
            {
                if (key != null)
                {
                    // Deleting the values returns Defender to its default "On" state
                    try { key.DeleteValue("DisableAntiSpyware"); } catch { }
                    try { key.DeleteValue("DisableAntiVirus"); } catch { }

                    // Delete the Real-Time subkey entirely
                    try { key.DeleteSubKeyTree("Real-Time Protection"); } catch { }
                }
            }

            AppendLog("Registry policies cleared. Defender is now active.");
        }

        private void ApplySelectedFooterEmulatorProfile()
        {
            if (!IsEmulatorPreset(_selectedPreset))
            {
                AppendTweakAction("The footer profile apply is emulator-specific for BlueStacks and MSI.");
                return;
            }

            int fps = GetSelectedProfileFps();
            bool enableRoot = ChkProfileRoot.IsChecked == true;
            bool setReadOnly = ChkProfileReadOnly.IsChecked == true;
            string targetPath = _selectedPreset.Equals("MSI", StringComparison.OrdinalIgnoreCase) ? MsiAppPlayerConfigPath : BlueStacksConfigPath;

            if (TryApplyEmulatorConfig(targetPath, fps, true, enableRoot, setReadOnly, out string message))
            {
                AppendRunnerOutput($"{_selectedPreset} profile applied. FPS={fps}, Root={enableRoot}, ReadOnly={setReadOnly}.");
                AppendLog(message);
                AppendTweakAction(message);
            }
            else
            {
                AppendLog(message);
                AppendTweakAction(message);
            }

            if (ChkProfileHighPriority.IsChecked == true)
            {
                SetHdPlayerPriority();
            }

            UpdateEnvironmentSummary();
        }

        private bool TryApplyEmulatorConfig(string configPath, int maxFps, bool enableHighFps, bool enableRoot, bool setReadOnly, out string message)
        {
            if (!File.Exists(configPath))
            {
                message = $"{configPath} not found.";
                return false;
            }

            try
            {
                FileAttributes originalAttributes = File.GetAttributes(configPath);
                bool originallyReadOnly = (originalAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;

                if (originallyReadOnly)
                {
                    File.SetAttributes(configPath, originalAttributes & ~FileAttributes.ReadOnly);
                }

                List<string> lines = File.ReadAllLines(configPath).ToList();
                string backupPath = configPath + ".bak";
                File.Copy(configPath, backupPath, true);

                Regex instanceRegex = new Regex(@"^bst\.instance\.(?<name>[^.]+)\.", RegexOptions.IgnoreCase);
                HashSet<string> instanceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    Match match = instanceRegex.Match(line);
                    if (match.Success)
                    {
                        instanceNames.Add(match.Groups["name"].Value);
                    }
                }

                foreach (string instance in instanceNames)
                {
                    UpdateOrAddQuotedKey(lines, $"bst.instance.{instance}.max_fps", maxFps.ToString());
                    UpdateOrAddQuotedKey(lines, $"bst.instance.{instance}.enable_high_fps", enableHighFps ? "1" : "0");
                    UpdateOrAddQuotedKey(lines, $"bst.instance.{instance}.enable_root_access", enableRoot ? "1" : "0");
                }

                UpdateOrAddQuotedKey(lines, "bst.mim.max_fps", maxFps.ToString());
                UpdateOrAddQuotedKey(lines, "bst.feature.rooting", enableRoot ? "1" : "0");

                File.WriteAllLines(configPath, lines, new UTF8Encoding(false));
                SetReadOnlyState(configPath, setReadOnly, null);

                message = $"Updated {Path.GetFileName(configPath)} ({instanceNames.Count} instance(s)) and applied FPS={maxFps}, Root={enableRoot}, ReadOnly={setReadOnly}.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Failed to update {configPath}: {ex.Message}";
                return false;
            }
        }

        private static void UpdateOrAddQuotedKey(List<string> lines, string key, string value)
        {
            string replacement = $"{key}=\"{value}\"";

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].TrimStart().StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                {
                    lines[i] = replacement;
                    return;
                }
            }

            lines.Add(replacement);
        }

        private void RunTempClean(bool deep)
        {
            List<string> targets = new List<string>
            {
                Path.GetTempPath(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp")
            };

            if (deep)
            {
                targets.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"));
                targets.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch"));
                targets.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\DeliveryOptimization\Cache"));
            }

            CleanupResult result = CleanTargets(targets, true);
            string label = deep ? "Deep temp clean" : "Temp clean";
            AppendLog($"{label} finished. Deleted {result.DeletedFiles} file(s), {result.DeletedDirectories} folder(s). Skipped {result.SkippedItems}.");
            AppendTweakAction($"{label} finished.");
        }

        private void RunShaderCacheClean()
        {
            List<string> targets = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"NVIDIA\DXCache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"NVIDIA Corporation\NV_Cache"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"AMD\DxCache")
            };

            CleanupResult result = CleanTargets(targets, true);
            AppendLog($"Shader cache clean finished. Deleted {result.DeletedFiles} file(s), {result.DeletedDirectories} folder(s). Skipped {result.SkippedItems}.");
            AppendTweakAction("Shader cache clean finished.");
        }

        private void MakeTimerPermanent()
        {
            try
            {
                // --- Part 1: Registry Tweaks (Same as before) ---
                const string subKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(subKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                        key.SetValue("NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord);
                        key.SetValue("NoLazyFlush", 1, RegistryValueKind.DWord);
                        AppendLog("Kernel Registry Flags Applied.");
                    }
                }

                // --- Part 2: Using NtSetSystemInformation ---
                // Class 100 is often used for SystemTimeAdjustment
                int systemInformationClass = 100;

                // We need to pass a 5ms or 10ms value (50000 or 100000 units)
                // We allocate memory for an integer to pass to the API
                int timerValue = 50000;
                IntPtr pData = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(pData, timerValue);

                try
                {
                    // This sends the data to the Windows Kernel
                    int result = NtSetSystemInformation(systemInformationClass, pData, sizeof(int));

                    if (result == 0)
                        AppendLog("NtSetSystemInformation: Kernel Timer adjusted to 5ms.");
                    else
                        AppendLog($"NtSetSystemInformation returned status: {result}");
                }
                finally
                {
                    // CRITICAL: Always free the memory you allocated!
                    Marshal.FreeHGlobal(pData);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Error: {ex.Message}");
            }
        }

        private void SetPermanentGamePriority()
        {
            try
            {
                const string path = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games";
                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(path, true))
                {
                    if (key != null)
                    {
                        // GPU Priority 6 is the highest for games
                        key.SetValue("GPU Priority", 6, RegistryValueKind.DWord);
                        // Priority 4 ensures the scheduler picks this task over background OS tasks
                        key.SetValue("Priority", 4, RegistryValueKind.DWord);
                        // Force Scheduling Category to Medium
                        key.SetValue("Scheduling Category", "Medium", RegistryValueKind.String);
                        // SFIO (Special File I/O) Priority Medium ensures textures load faster from disk
                        key.SetValue("SFIO Priority", "Medium", RegistryValueKind.String);

                        AppendLog("Permanent GPU/CPU 'High' Category locked for Emulators.");
                    }
                    else
                    {
                        AppendLog("Error: Games Task path not found in registry.");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Priority Registry Error: {ex.Message}");
            }
        }


        private void RunDnsFlush()
        {
            bool ok = RunExecutable("ipconfig.exe", "/flushdns", true, true);
            AppendLog(ok ? "DNS flush completed." : "DNS flush failed or was cancelled.");
            AppendTweakAction(ok ? "DNS flush completed." : "DNS flush failed.");
        }

        private void RunRamTrim()
        {
            int trimmed = 0;
            AppendLog("Starting Deep RAM Clean...");

            try
            {
                // 1. Process Working Set Trim
                foreach (Process process in Process.GetProcesses())
                {
                    try
                    {
                        // Skip System, Idle, and the Optimizer itself
                        if (process.Id <= 4 || process.Id == Process.GetCurrentProcess().Id || process.HasExited)
                            continue;

                        if (EmptyWorkingSet(process.Handle))
                            trimmed++;
                    }
                    catch { /* Protection check: System processes will skip here */ }
                }

                // 2. Empty System Working Set (System Cache)
                SetSystemFileCacheSize((IntPtr)(-1), (IntPtr)(-1), 0);

                // 3. Empty Standby List & Modified Page List (Premium/RAMMap style)
                // 4 = Standby List, 5 = Modified Page List
                int[] commands = { 4, 5 };
                foreach (int cmd in commands)
                {
                    IntPtr commandPtr = Marshal.AllocHGlobal(sizeof(int));
                    Marshal.WriteInt32(commandPtr, cmd);
                    try
                    {
                        // 80 = SystemMemoryListInformation
                        NtSetSystemInformation(80, commandPtr, sizeof(int));
                    }
                    catch { /* Fail silently if non-admin */ }
                    finally { Marshal.FreeHGlobal(commandPtr); }
                }

                AppendLog($"RAM clean finished. {trimmed} processes trimmed and System Cache purged.");
                AppendTweakAction("Deep RAM clean completed.");
            }
            catch (Exception ex)
            {
                AppendLog($"RAM Clean Error: {ex.Message}");
            }
        }

        private void OpenGraphicsSettings()
        {
            bool opened = OpenUri("ms-settings:display-advancedgraphics");
            AppendLog(opened ? "Opened graphics settings." : "Failed to open graphics settings.");
            AppendTweakAction(opened ? "Opened graphics settings." : "Failed to open graphics settings.");
        }

        private void OpenStartupApps()
        {
            bool opened = OpenUri("ms-settings:startupapps");
            AppendLog(opened ? "Opened startup apps." : "Failed to open startup apps.");
            AppendTweakAction(opened ? "Opened startup apps." : "Failed to open startup apps.");
        }

        private void OpenGameModeSettings()
        {
            bool opened = OpenUri("ms-settings:gaming");
            AppendLog(opened ? "Opened gaming settings." : "Failed to open gaming settings.");
            AppendTweakAction(opened ? "Opened gaming settings." : "Failed to open gaming settings.");
        }

        private void KillCommonOverlays()
        {
            string[] processNames = { "Discord", "GameBar", "GameBarFTServer", "NVIDIA Share", "RadeonSoftware", "Overwolf", "steamwebhelper" };
            int killed = 0;

            foreach (string processName in processNames)
            {
                foreach (Process process in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        process.Kill();
                        killed++;
                    }
                    catch
                    {
                    }
                }
            }

            AppendLog($"Overlay cleanup finished. Closed {killed} process(es).");
            AppendTweakAction("Overlay cleanup finished.");
        }

        private void SetHighPerformancePowerPlan()
        {
            bool ok = RunExecutable("powercfg.exe", "/setactive SCHEME_MIN", true, true);
            AppendLog(ok ? "High performance power plan requested." : "Failed to switch to high performance plan.");
            AppendTweakAction(ok ? "High performance power plan requested." : "Power plan switch failed.");
        }

        private void SetBalancedPowerPlan()
        {
            bool ok = RunExecutable("powercfg.exe", "/setactive SCHEME_BALANCED", true, true);
            AppendLog(ok ? "Balanced power plan requested." : "Failed to switch to balanced plan.");
            AppendTweakAction(ok ? "Balanced power plan requested." : "Balanced power plan switch failed.");
        }

        private void CreateRestorePoint()
        {
            string command = "Checkpoint-Computer -Description 'SxS Optimizer Restore Point' -RestorePointType 'MODIFY_SETTINGS'";
            bool ok = LaunchPowerShellCommand(command, false, true);
            AppendLog(ok ? "Restore point request launched." : "Failed to launch restore point request.");
            AppendTweakAction(ok ? "Restore point request launched." : "Restore point request failed.");
        }
        private void SetHdPlayerPriority()
        {
            try
            {
                // Target: Image File Execution Options (IFEO)
                // This tells Windows to automatically apply settings whenever HD-Player.exe starts.
                string exeName = "HD-Player.exe";
                string path = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\{exeName}\PerfOptions";

                using (RegistryKey? key = Registry.LocalMachine.CreateSubKey(path))
                {
                    if (key != null)
                    {
                        // Value 3 = High Priority Class
                        // This is the direct kernel-level flag for High Priority.
                        key.SetValue("CpuPriorityClass", 3, RegistryValueKind.DWord);

                        // Value 2 = High I/O Priority
                        // Ensures the emulator's disk requests are handled first.
                        key.SetValue("IoPriority", 2, RegistryValueKind.DWord);

                        AppendLog("Permanent HD-Player priority locked to Value 3 (High).");
                        AppendTweakAction("Kernel-level IFEO priority injection successful.");
                    }
                    else
                    {
                        AppendLog("Error: Could not create or open Registry Key.");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                AppendLog("Access Denied: Please run as Administrator.");
            }
            catch (Exception ex)
            {
                AppendLog($"Registry Error: {ex.Message}");
            }
        }

        private void SetPriorityForAny(IEnumerable<string> fileNames, string successMessage)
        {
            int updated = 0;
            foreach (string fileName in fileNames)
            {
                string processName = Path.GetFileNameWithoutExtension(fileName);
                foreach (Process process in Process.GetProcessesByName(processName))
                {
                    try
                    {
                        process.PriorityClass = ProcessPriorityClass.High;
                        updated++;
                    }
                    catch
                    {
                    }
                }
            }

            AppendLog(updated > 0 ? successMessage : "No matching running process was updated.");
            AppendTweakAction(updated > 0 ? successMessage : "No matching running process was updated.");
        }

        private void ApplyPriorityForProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                AppendTweakAction("No process name was provided.");
                return;
            }

            SetPriorityForAny(new[] { processName }, $"Applied high priority for {processName}.");
        }

        private void OpenProfilePrimaryConfig(string path)
        {
            OpenPathInExplorer(path);
        }

        private void OpenProfilePrimaryConfigFolder(string path)
        {
            string? folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(folder))
            {
                OpenPathOrUri(folder);
            }
        }

        private void UnlockConfig(string configPath, string? successMessage)
        {
            if (!File.Exists(configPath))
            {
                AppendLog($"Config not found: {configPath}");
                return;
            }

            try
            {
                FileAttributes attributes = File.GetAttributes(configPath);
                File.SetAttributes(configPath, attributes & ~FileAttributes.ReadOnly);
                if (!string.IsNullOrWhiteSpace(successMessage))
                {
                    AppendLog(successMessage);
                    AppendTweakAction(successMessage);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to unlock config: {ex.Message}");
            }
        }

        private void SetReadOnlyState(string configPath, bool readOnly, string? logMessage)
        {
            if (!File.Exists(configPath))
                return;

            try
            {
                FileAttributes attributes = File.GetAttributes(configPath);
                if (readOnly)
                    File.SetAttributes(configPath, attributes | FileAttributes.ReadOnly);
                else
                    File.SetAttributes(configPath, attributes & ~FileAttributes.ReadOnly);

                if (!string.IsNullOrWhiteSpace(logMessage))
                {
                    AppendLog(logMessage);
                    AppendTweakAction(logMessage);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to set read-only state: {ex.Message}");
            }
        }

        private void SetSelectedPresetReadOnly(bool readOnly)
        {
            string? config = GetCurrentEmulatorConfigPath();
            if (string.IsNullOrWhiteSpace(config))
            {
                AppendTweakAction("Selected preset does not use a writable config file.");
                return;
            }

            SetReadOnlyState(config, readOnly, readOnly ? $"{_selectedPreset} config set to read-only." : $"{_selectedPreset} config unlocked.");
        }

        private string? GetCurrentEmulatorConfigPath()
        {
            return _selectedPreset switch
            {
                "BlueStacks" => BlueStacksConfigPath,
                "MSI" => MsiAppPlayerConfigPath,
                _ => null
            };
        }

        private bool QuerySelectedPresetReadOnly()
        {
            string? path = GetCurrentEmulatorConfigPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            return (File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
        }

        private void SetSelectedEmulatorFlag(string keyFragment, string value)
        {
            string? configPath = GetCurrentEmulatorConfigPath();
            if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
            {
                AppendTweakAction("Selected emulator config was not found.");
                return;
            }

            try
            {
                UnlockConfig(configPath, null);
                List<string> lines = File.ReadAllLines(configPath).ToList();
                Regex instanceRegex = new Regex(@"^bst\.instance\.(?<name>[^.]+)\.", RegexOptions.IgnoreCase);
                HashSet<string> instanceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (string rawLine in lines)
                {
                    Match match = instanceRegex.Match(rawLine.Trim());
                    if (match.Success)
                        instanceNames.Add(match.Groups["name"].Value);
                }

                foreach (string instance in instanceNames)
                {
                    UpdateOrAddQuotedKey(lines, $"bst.instance.{instance}.{keyFragment}", value);
                }

                if (keyFragment.Equals("enable_root_access", StringComparison.OrdinalIgnoreCase))
                    UpdateOrAddQuotedKey(lines, "bst.feature.rooting", value);

                File.WriteAllLines(configPath, lines, new UTF8Encoding(false));
                if (ChkProfileReadOnly.IsChecked == true)
                    SetReadOnlyState(configPath, true, null);

                AppendTweakAction($"Updated {_selectedPreset} flag {keyFragment} to {value}.");
                AppendLog($"Updated {_selectedPreset} flag {keyFragment} to {value}.");
            }
            catch (Exception ex)
            {
                AppendLog($"Failed to update {_selectedPreset} flag: {ex.Message}");
            }
        }

        private bool QuerySelectedEmulatorConfigContains(string keyFragment, string value)
        {
            string? configPath = GetCurrentEmulatorConfigPath();
            if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
                return false;

            try
            {
                string text = File.ReadAllText(configPath);
                return text.IndexOf(keyFragment + "=\"" + value + "\"", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private void SetFullscreenOptimizationNote(TweakDefinition tweak, bool desired)
        {
            AppendLog($"Fullscreen optimization toggle for {tweak.Preset} is stored as a workflow note. Apply through the executable properties dialog when the game path is known.");
            AppendTweakAction($"Fullscreen optimization note updated for {tweak.Preset}: {(desired ? "Enabled" : "Disabled")}.");
        }

        private void SetGameDvr(bool disable)
        {
            try
            {
                BackupRegistryValue("game_dvr_appcapture", RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled");
                BackupRegistryValue("game_dvr_history", RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled");

                using RegistryKey? key1 = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore");
                using RegistryKey? key2 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR");
                key1?.SetValue("GameDVR_Enabled", disable ? 0 : 1, RegistryValueKind.DWord);
                key2?.SetValue("AppCaptureEnabled", disable ? 0 : 1, RegistryValueKind.DWord);
                AppendLog(disable ? "Game DVR disabled." : "Game DVR enabled.");
                AppendTweakAction(disable ? "Game DVR disabled." : "Game DVR enabled.");
            }
            catch (Exception ex)
            {
                AppendLog("Failed to toggle Game DVR: " + ex.Message);
            }
        }

        private bool QueryGameDvrDisabled()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore");
                object? value = key?.GetValue("GameDVR_Enabled", 1);
                return Convert.ToInt32(value) == 0;
            }
            catch
            {
                return false;
            }
        }

        private void SetHags(bool enable)
        {
            try
            {
                BackupRegistryValue("hags_hwschmode", RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode");
                using RegistryKey? key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers");
                key?.SetValue("HwSchMode", enable ? 2 : 1, RegistryValueKind.DWord);
                AppendLog(enable ? "HAGS enabled. Restart Windows to apply." : "HAGS disabled. Restart Windows to apply.");
                AppendTweakAction(enable ? "HAGS enabled." : "HAGS disabled.");
            }
            catch (Exception ex)
            {
                AppendLog("Failed to toggle HAGS: " + ex.Message);
            }
        }

        private bool QueryHagsEnabled()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers");
                object? value = key?.GetValue("HwSchMode", 1);
                return Convert.ToInt32(value) == 2;
            }
            catch
            {
                return false;
            }
        }

        private void BackupRegistryValue(string backupId, RegistryHive hive, string subKeyPath, string valueName)
        {
            if (_registryBackups.ContainsKey(backupId))
                return;

            try
            {
                RegistryKey baseKey = hive == RegistryHive.LocalMachine ? Registry.LocalMachine : Registry.CurrentUser;
                using RegistryKey? key = baseKey.OpenSubKey(subKeyPath);
                object? original = key?.GetValue(valueName);
                RegistryValueKind kind = key?.GetValueKind(valueName) ?? RegistryValueKind.DWord;
                _registryBackups[backupId] = new RegistryToggleBackup { BackupId = backupId, OriginalValue = original, ValueKind = kind };
            }
            catch
            {
                _registryBackups[backupId] = new RegistryToggleBackup { BackupId = backupId, OriginalValue = null, ValueKind = RegistryValueKind.DWord };
            }
        }

        private void CopyCs2AutoexecTemplate()
        {
            string template = "fps_max 0\nrate 786432\ncl_updaterate 128\ncl_cmdrate 128\n";
            WpfClipboard.SetText(template);
            AppendLog("CS2 autoexec template copied to clipboard.");
            AppendTweakAction("CS2 autoexec template copied.");
        }

        private void CopyPresetGuidanceNote(string preset)
        {
            string note = preset switch
            {
                "CS2" => "CS2: Test autoexec, launch options, overlays and fullscreen behavior one by one.",
                "Valorant" => "Valorant: Test Raw Input Buffer, overlays and startup clutter. Keep Vanguard-related security items manual.",
                "Fortnite" => "Fortnite: Performance Mode and render path changes happen in-game and need a game restart.",
                "GTA V" => "GTA V: Test settings.xml carefully, keep overlays light and validate GPU preference.",
                "Roblox PC" => "Roblox: Use the built-in Maximum Framerate setting in the client settings menu.",
                _ => preset + ": No special guidance note."
            };

            WpfClipboard.SetText(note);
            AppendLog($"{preset} guidance note copied to clipboard.");
            AppendTweakAction($"{preset} guidance note copied.");
        }

        private void OpenHelperPath(string action)
        {
            string[] parts = action.Split('|');
            if (parts.Length < 3)
                return;

            string preset = parts[1];
            string relative = parts[2];

            string target = preset switch
            {
                "CS2" => BuildSteamCfgFolder(),
                "Valorant" => BuildFolderFromLocalAppData(relative),
                "Fortnite" => BuildFolderFromLocalAppData(relative),
                "GTA V" => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Rockstar Games\GTA V"),
                "Roblox PC" => BuildFolderFromLocalAppData(@"Roblox"),
                _ => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };

            OpenPathOrUri(target);
        }

        private string BuildSteamCfgFolder()
        {
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string baseSteam = Path.Combine(programFilesX86, @"Steam\userdata");
            return Directory.Exists(baseSteam) ? baseSteam : programFilesX86;
        }

        private string BuildFolderFromLocalAppData(string relative)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), relative);
        }

        private void CmbPcGamePreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void BtnApplyPcGamePriority_Click(object sender, RoutedEventArgs e)
        {
            ApplyPriorityForProcessName(TxtProfileTarget.Text);
        }

        private void BtnOpenWindowsSecurity_Click(object sender, RoutedEventArgs e)
        {
            bool opened = OpenUri("windowsdefender:");
            AppendLog(opened ? "Opened Windows Security." : "Failed to open Windows Security.");
        }

        private void BtnOpenSecuritySettings_Click(object sender, RoutedEventArgs e)
        {
            bool opened = OpenUri("ms-settings:windowsdefender");
            AppendLog(opened ? "Opened Windows security settings." : "Failed to open Windows security settings.");
        }

        private void BtnSetBootTimeout_Click(object sender, RoutedEventArgs e)
        {
            bool ok = RunExecutable("bcdedit.exe", "/timeout 10", true, true);
            AppendLog(ok ? "Requested boot timeout set to 10 seconds." : "Boot timeout command failed or was cancelled.");
        }

        private void BtnResetBootLimits_Click(object sender, RoutedEventArgs e)
        {
            bool one = RunExecutable("bcdedit.exe", "/deletevalue {default} numproc", true, true);
            bool two = RunExecutable("bcdedit.exe", "/deletevalue {default} truncatememory", true, true);
            AppendLog(one || two ? "Requested removal of boot limits for processor count and maximum memory." : "Boot limit reset failed or was cancelled.");
        }

        private void BtnQueueAppCategories_Click(object sender, RoutedEventArgs e)
        {
            List<string> categories = GetSelectedAppCategories();
            TxtAppQueueSummary.Text = categories.Count == 0 ? "No categories selected." : "Queued categories: " + string.Join(", ", categories);
            AppendLog(categories.Count == 0 ? "No app categories selected." : "Queued app categories: " + string.Join(", ", categories));
        }

        private void BtnClearAppCategories_Click(object sender, RoutedEventArgs e)
        {
            SetAppCategorySelection(false, false, false, false, false, false);
            TxtAppQueueSummary.Text = "No categories selected.";
            AppendLog("Cleared app category selection.");
        }

        private void BtnQueueEssentialBundle_Click(object sender, RoutedEventArgs e)
        {
            SetAppCategorySelection(true, true, false, true, false, false);
            BtnQueueAppCategories_Click(sender, e);
        }

        private void BtnQueueDeveloperBundle_Click(object sender, RoutedEventArgs e)
        {
            SetAppCategorySelection(true, false, true, true, false, false);
            BtnQueueAppCategories_Click(sender, e);
        }

        private void BtnQueueGamingBundle_Click(object sender, RoutedEventArgs e)
        {
            SetAppCategorySelection(false, true, false, true, false, true);
            BtnQueueAppCategories_Click(sender, e);
        }

        private void SetAppCategorySelection(bool browsers, bool communication, bool development, bool utilities, bool media, bool gaming)
        {
            ChkAppsBrowsers.IsChecked = browsers;
            ChkAppsCommunication.IsChecked = communication;
            ChkAppsDevelopment.IsChecked = development;
            ChkAppsUtilities.IsChecked = utilities;
            ChkAppsMedia.IsChecked = media;
            ChkAppsGaming.IsChecked = gaming;
        }

        private List<string> GetSelectedAppCategories()
        {
            List<string> categories = new List<string>();
            if (ChkAppsBrowsers.IsChecked == true) categories.Add("Browsers");
            if (ChkAppsCommunication.IsChecked == true) categories.Add("Communication");
            if (ChkAppsDevelopment.IsChecked == true) categories.Add("Development");
            if (ChkAppsUtilities.IsChecked == true) categories.Add("Utilities");
            if (ChkAppsMedia.IsChecked == true) categories.Add("Media");
            if (ChkAppsGaming.IsChecked == true) categories.Add("Gaming Support");
            return categories;
        }

        private void BtnLogsCopy_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtLogs.Text))
                return;

            WpfClipboard.SetText(TxtLogs.Text);
            AppendLog("Logs copied to clipboard.");
        }

        private void BtnLogsExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string filePath = Path.Combine(desktop, $"SxS_Optimizer_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(filePath, TxtLogs.Text ?? string.Empty, Encoding.UTF8);
                AppendLog("Logs exported to desktop.");
            }
            catch (Exception ex)
            {
                AppendLog("Failed to export logs: " + ex.Message);
            }
        }

        private void BtnLogsClear_Click(object sender, RoutedEventArgs e)
        {
            _logBuilder.Clear();
            _tweakActionLogBuilder.Clear();
            PushLogsToUi();
            AppendLog("Logs cleared.");
        }

        private bool LaunchPowerShellCommand(string command, bool noExit, bool requireElevation)
        {
            try
            {
                string arguments = noExit
                    ? $"-NoExit -NoProfile -ExecutionPolicy Bypass -Command \"{command}\""
                    : $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = arguments,
                    UseShellExecute = true
                };

                if (requireElevation && !IsAdministrator())
                {
                    startInfo.Verb = "runas";
                }

                return Process.Start(startInfo) != null;
            }
            catch
            {
                return false;
            }
        }

        private bool RunExecutable(string fileName, string arguments, bool requireElevation, bool waitForExit)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true
                };

                if (requireElevation && !IsAdministrator())
                {
                    startInfo.Verb = "runas";
                }

                Process? process = Process.Start(startInfo);
                if (process == null)
                    return false;

                if (waitForExit)
                {
                    process.WaitForExit();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool OpenUri(string uri)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = uri, UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void OpenPathOrUri(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (value.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase) || value.StartsWith("steam:", StringComparison.OrdinalIgnoreCase))
            {
                OpenUri(value);
                return;
            }

            if (File.Exists(value) || Directory.Exists(value))
            {
                OpenPathInExplorer(value);
                return;
            }

            AppendLog("Path not found: " + value);
        }

        private void OpenPathInExplorer(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{path}\"",
                        UseShellExecute = true
                    });
                    AppendLog("Opened path: " + path);
                    return;
                }

                if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{path}\"",
                        UseShellExecute = true
                    });
                    AppendLog("Opened folder: " + path);
                    return;
                }

                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{directory}\"",
                        UseShellExecute = true
                    });
                    AppendLog("Opened folder: " + directory);
                    return;
                }

                AppendLog("Path not found: " + path);
            }
            catch (Exception ex)
            {
                AppendLog("Failed to open path: " + ex.Message);
            }
        }

        private CleanupResult CleanTargets(IEnumerable<string> targetFolders, bool includeRootFiles)
        {
            CleanupResult result = new CleanupResult();

            foreach (string folder in targetFolders.Where(f => !string.IsNullOrWhiteSpace(f)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    if (!Directory.Exists(folder))
                        continue;

                    if (includeRootFiles)
                    {
                        TryDeleteFilesInDirectory(folder, result);
                    }

                    TryDeleteChildDirectories(folder, result);
                }
                catch
                {
                    result.SkippedItems++;
                }
            }

            return result;
        }

        private void TryDeleteFilesInDirectory(string folder, CleanupResult result)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(folder))
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        result.DeletedFiles++;
                    }
                    catch
                    {
                        result.SkippedItems++;
                    }
                }
            }
            catch
            {
                result.SkippedItems++;
            }
        }

        private void TryDeleteChildDirectories(string folder, CleanupResult result)
        {
            try
            {
                foreach (string directory in Directory.EnumerateDirectories(folder))
                {
                    try
                    {
                        ForceDeleteDirectory(directory, result);
                        result.DeletedDirectories++;
                    }
                    catch
                    {
                        result.SkippedItems++;
                    }
                }
            }
            catch
            {
                result.SkippedItems++;
            }
        }

        private void ForceDeleteDirectory(string directoryPath, CleanupResult result)
        {
            foreach (string subDirectory in Directory.EnumerateDirectories(directoryPath))
            {
                try
                {
                    ForceDeleteDirectory(subDirectory, result);
                    result.DeletedDirectories++;
                }
                catch
                {
                    result.SkippedItems++;
                }
            }

            foreach (string file in Directory.EnumerateFiles(directoryPath))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    result.DeletedFiles++;
                }
                catch
                {
                    result.SkippedItems++;
                }
            }

            try
            {
                File.SetAttributes(directoryPath, FileAttributes.Normal);
            }
            catch
            {
            }

            Directory.Delete(directoryPath, false);
        }

        private int GetSelectedProfileFps()
        {
            if (CmbProfileFps.SelectedItem is WpfComboBoxItem item && int.TryParse(item.Content?.ToString(), out int fps))
                return fps;

            return 240;
        }

        private bool IsEmulatorPreset(string preset)
        {
            return preset.Equals("BlueStacks", StringComparison.OrdinalIgnoreCase) || preset.Equals("MSI", StringComparison.OrdinalIgnoreCase);
        }

        private static bool GetResultSucceeded(object result)
        {
            PropertyInfo? prop = result.GetType().GetProperty("Success") ?? result.GetType().GetProperty("IsSuccess");
            object? value = prop?.GetValue(result);
            return value is bool ok && ok;
        }

        private static string GetResultStringValue(object result)
        {
            PropertyInfo? prop = result.GetType().GetProperty("Data") ?? result.GetType().GetProperty("Value") ?? result.GetType().GetProperty("Url");
            return prop?.GetValue(result)?.ToString() ?? string.Empty;
        }

        private static string GetResultMessage(object result)
        {
            PropertyInfo? prop = result.GetType().GetProperty("Message") ?? result.GetType().GetProperty("ErrorMessage");
            return prop?.GetValue(result)?.ToString() ?? string.Empty;
        }

        private static WpfBrush CreateBrush(string hex)
        {
            return (WpfBrush)new BrushConverter().ConvertFromString(hex)!;
        }

        private static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static string GetAppVersion()
        {
            try
            {
                return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        private static string GetOsDisplayName()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                string productName = key?.GetValue("ProductName")?.ToString() ?? "Windows";
                string releaseId = key?.GetValue("DisplayVersion")?.ToString() ?? key?.GetValue("ReleaseId")?.ToString() ?? string.Empty;
                return string.IsNullOrWhiteSpace(releaseId) ? productName : $"{productName} {releaseId}";
            }
            catch
            {
                return Environment.OSVersion.VersionString;
            }
        }

        private static string GetCpuNameFromRegistry()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return key?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "Unknown CPU";
            }
            catch
            {
                return "Unknown CPU";
            }
        }

        private static string GetTotalRamDisplay()
        {
            try
            {
                ComputerInfo ci = new ComputerInfo();
                double gb = ci.TotalPhysicalMemory / 1024d / 1024d / 1024d;
                return $"{gb:0.0} GB";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string GetReadOnlySummary(bool blueExists, bool msiExists)
        {
            List<string> states = new List<string>();

            if (blueExists)
            {
                bool blueReadOnly = (File.GetAttributes(BlueStacksConfigPath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                states.Add($"BlueStacks: {(blueReadOnly ? "Read-only" : "Writable")}");
            }

            if (msiExists)
            {
                bool msiReadOnly = (File.GetAttributes(MsiAppPlayerConfigPath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                states.Add($"MSI: {(msiReadOnly ? "Read-only" : "Writable")}");
            }

            return states.Count == 0 ? "No config found" : string.Join(" / ", states);
        }

        private static string GetSelectedComboText(WpfComboBox comboBox, string fallback)
        {
            return comboBox.SelectedItem is WpfComboBoxItem item
                ? item.Content?.ToString() ?? fallback
                : fallback;
        }

        private void AppendRunnerOutput(string message)
        {
            AppendToTextBox(TxtRunnerOutput, message);
        }

        private void AppendLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _logBuilder.AppendLine($"{DateTime.Now:HH:mm:ss}  {message}");
            PushLogsToUi();
        }

        private void AppendTweakAction(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _tweakActionLogBuilder.AppendLine($"{DateTime.Now:HH:mm:ss}  {message}");
            PushLogsToUi();
        }

        private void PushLogsToUi()
        {
            string main = _logBuilder.ToString();
            TxtLogs.Text = main;
            TxtDashboardPreviewLog.Text = main;
            TxtTweakActionLog.Text = _tweakActionLogBuilder.ToString();

            if (!string.IsNullOrEmpty(main))
            {
                TxtLogs.ScrollToEnd();
                TxtDashboardPreviewLog.ScrollToEnd();
            }

            if (!string.IsNullOrEmpty(TxtTweakActionLog.Text))
            {
                TxtTweakActionLog.ScrollToEnd();
            }
        }

        private static void AppendToTextBox(WpfTextBox textBox, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.AppendText(Environment.NewLine);
            }

            textBox.AppendText($"{DateTime.Now:HH:mm:ss}  {message}");
            textBox.ScrollToEnd();
        }

        private sealed class CleanupResult
        {
            public int DeletedFiles { get; set; }
            public int DeletedDirectories { get; set; }
            public int SkippedItems { get; set; }
        }

        private sealed class RegistryToggleBackup
        {
            public string BackupId { get; set; } = string.Empty;
            public object? OriginalValue { get; set; }
            public RegistryValueKind ValueKind { get; set; }
        }

        private sealed class TweakDefinition
        {
            public string Id { get; set; } = string.Empty;
            public string Preset { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Filter { get; set; } = string.Empty;
            public string RestartBadge { get; set; } = string.Empty;
            public string RiskBadge { get; set; } = string.Empty;
            public TweakCardKind CardKind { get; set; }
            public string ActionId { get; set; } = string.Empty;
            public ToggleKind ToggleKind { get; set; }
        }

        private enum TweakCardKind
        {
            Action,
            Toggle,
            Note
        }

        private enum ToggleKind
        {
            None,
            GameDvr,
            Hags,
            EmulatorReadOnly,
            EmulatorHighFps,
            EmulatorRoot,
            FullscreenOptimizations
        }
    }
}
