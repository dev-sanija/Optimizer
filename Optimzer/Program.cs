using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoUpdaterDotNET;
using Optimzer.Core;

namespace Optimzer
{
    internal static class Program
    {
        // Replace this with your real raw GitHub update.xml URL
        private const string UpdateXmlUrl = "https://raw.githubusercontent.com/sanija123t/XML-SxSOptimizer/refs/heads/main/update.xml";

        private static readonly object _logSync = new object();
        private static int _updateCheckStarted = 0;

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // --- ADD THIS FOR WPF SUPPORT ---
            // This creates the WPF application context safely
            System.Windows.Application? wpfApp = new System.Windows.Application();

            // Prevent the app from exiting when windows are closed
            wpfApp.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;

            // Clean shutdown hook when WinForms app exits
            Application.ApplicationExit += (s, e) =>
            {
                try
                {
                    wpfApp?.Shutdown();
                    wpfApp = null;
                }
                catch { }
            };
            // --------------------------------

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ThreadException += Application_ThreadException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            Application.ApplicationExit += Application_ApplicationExit;

            try
            {
                AppSession.Clear();
                ConfigureAutoUpdater();

                // Start your WinForms Login
                Application.Run(new FrmLogin());
            }
            catch (Exception ex)
            {
                SafeLog("Fatal startup exception", ex);
                ShowFatalError(ex);
            }
            finally
            {
                AppSession.Clear();

                AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
                Application.ThreadException -= Application_ThreadException;
                TaskScheduler.UnobservedTaskException -= TaskScheduler_UnobservedTaskException;
                Application.ApplicationExit -= Application_ApplicationExit;
            }
        }

        private static void Application_ApplicationExit(object? sender, EventArgs e)
        {
            try
            {
                SafeReleaseAppResources();
            }
            catch
            {
            }
        }

        // ============================================================
        // UPDATER
        // ============================================================

        public static async Task<bool> HasWorkingInternetForUpdateAsync()
        {
            return await HasWorkingInternetAsync().ConfigureAwait(false);
        }

        public static void BeginUiThreadUpdateCheck(Form owner)
        {
            try
            {
                if (owner == null || owner.IsDisposed)
                    return;

                if (Interlocked.Exchange(ref _updateCheckStarted, 1) == 1)
                    return;

                var timer = new System.Windows.Forms.Timer
                {
                    Interval = 2000
                };

                EventHandler? tickHandler = null;
                FormClosedEventHandler? closeHandler = null;

                closeHandler = (s, e) =>
                {
                    try
                    {
                        timer.Stop();
                        timer.Dispose();
                    }
                    catch
                    {
                    }

                    Interlocked.Exchange(ref _updateCheckStarted, 0);

                    try
                    {
                        owner.FormClosed -= closeHandler;
                    }
                    catch
                    {
                    }
                };

                tickHandler = async (s, e) =>
                {
                    timer.Stop();
                    timer.Tick -= tickHandler;

                    try
                    {
                        if (owner.IsDisposed)
                        {
                            Interlocked.Exchange(ref _updateCheckStarted, 0);
                            return;
                        }

                        bool hasInternet = await HasWorkingInternetAsync().ConfigureAwait(true);
                        if (!hasInternet)
                        {
                            Interlocked.Exchange(ref _updateCheckStarted, 0);
                            return;
                        }

                        AutoUpdater.Start(UpdateXmlUrl);
                    }
                    catch (Exception ex)
                    {
                        SafeLog("AutoUpdater start failure", ex);
                        Interlocked.Exchange(ref _updateCheckStarted, 0);
                    }
                    finally
                    {
                        try
                        {
                            timer.Dispose();
                        }
                        catch
                        {
                        }

                        try
                        {
                            if (!owner.IsDisposed)
                                owner.FormClosed -= closeHandler;
                        }
                        catch
                        {
                        }
                    }
                };

                owner.FormClosed += closeHandler;
                timer.Tick += tickHandler;
                timer.Start();
            }
            catch (Exception ex)
            {
                SafeLog("BeginUiThreadUpdateCheck failure", ex);
                Interlocked.Exchange(ref _updateCheckStarted, 0);
            }
        }

        private static void ConfigureAutoUpdater()
        {
            AutoUpdater.ReportErrors = false;
            AutoUpdater.Mandatory = false;
            AutoUpdater.UpdateMode = Mode.ForcedDownload;
            AutoUpdater.ShowSkipButton = true;
            AutoUpdater.ShowRemindLaterButton = true;

            AutoUpdater.ApplicationExitEvent += () =>
            {
                try
                {
                    SafeReleaseAppResources();
                    Process.GetCurrentProcess().Kill();
                }
                catch
                {
                    Environment.Exit(0);
                }
            };
        }

        private static void SafeReleaseAppResources()
        {
            try
            {
                AppSession.Clear();
            }
            catch
            {
            }

            try
            {
                foreach (Form form in Application.OpenForms.Cast<Form>().ToArray())
                {
                    try
                    {
                        form.Hide();
                        form.Enabled = false;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }

            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch
            {
            }
        }

        private static async Task<bool> HasWorkingInternetAsync()
        {
            try
            {
                using HttpClient client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(4)
                };

                using HttpResponseMessage response =
                    await client.GetAsync("https://www.google.com/generate_204").ConfigureAwait(false);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // ERROR HANDLING
        // ============================================================

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            SafeLog("UI thread exception", e.Exception);
            ShowFatalError(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception
                ?? new Exception("Unknown unhandled exception.");

            SafeLog("AppDomain unhandled exception", ex);
            ShowFatalError(ex);
        }

        private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            SafeLog("Unobserved task exception", e.Exception);
            e.SetObserved();
        }

        private static void ShowFatalError(Exception ex)
        {
            try
            {
                MessageBox.Show(
                    "A critical application error occurred.\n\n" +
                    "The error has been logged locally.\n\n" +
                    ex.Message,
                    "SxS Optimizer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
            }
        }

        private static void SafeLog(string title, Exception ex)
        {
            try
            {
                string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
                Directory.CreateDirectory(logDirectory);

                string logFilePath = Path.Combine(logDirectory, "fatal.log");

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("==================================================");
                sb.AppendLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
                sb.AppendLine(title);
                sb.AppendLine(ex.ToString());

                lock (_logSync)
                {
                    File.AppendAllText(logFilePath, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
            }
        }
    }
}