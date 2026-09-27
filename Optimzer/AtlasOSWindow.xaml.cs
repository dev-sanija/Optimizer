using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace Optimzer
{
    public partial class AtlasOSWindow : Window
    {
        public AtlasOSWindow()
        {
            InitializeComponent();
        }

        // -----------------------------
        // WINDOW DRAG SUPPORT
        // -----------------------------
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        // -----------------------------
        // CLOSE BUTTON (CLEAN EXIT FOR THIS WINDOW ONLY)
        // -----------------------------
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // IMPORTANT: do NOT Shutdown entire app unless you want full exit
        }

        // -----------------------------
        // LOGO CLICK
        // -----------------------------
        private void AtlasButton_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://atlasos.net/");
        }

        // -----------------------------
        // WIZARD BUTTON
        // -----------------------------
        private void OpenAtlasWizard(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "Atlas OS Wizard starting...",
                "Atlas OS",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // -----------------------------
        // DOWNLOAD / GUIDE BUTTONS
        // -----------------------------
        private void OpenAEM(object sender, RoutedEventArgs e)
            => OpenUrl("https://www.dropbox.com/scl/fi/5e2p1o6wpxnz94ct76eqs/AME-Beta.zip?rlkey=057tybk4bm1s40hxvv216j78b&st=f3bsvxdc&dl=1](https://www.dropbox.com/scl/fi/5e2p1o6wpxnz94ct76eqs/AME-Beta.zip?rlkey=057tybk4bm1s40hxvv216j78b&st=f3bsvxdc&dl=1");

        private void OpenWin10Guide(object sender, RoutedEventArgs e)
            => OpenUrl("https://www.dropbox.com/scl/fi/l9pwf22crdy5odln4fcg0/AtlasPlaybook_v0.4.1.zip?rlkey=21u6mc8oksra76oh6w2dbuw0e&st=rs53vlo8&dl=1](https://www.dropbox.com/scl/fi/l9pwf22crdy5odln4fcg0/AtlasPlaybook_v0.4.1.zip?rlkey=21u6mc8oksra76oh6w2dbuw0e&st=rs53vlo8&dl=1");

        private void OpenWin11Guide(object sender, RoutedEventArgs e)
            => OpenUrl("https://www.dropbox.com/scl/fi/az4ko76jghu5rp5965874/AtlasPlaybook_v0.5.0-hotfix.zip?rlkey=7tajcohhu650z5jyskw0gunoz&st=48amd2yf&dl=1](https://www.dropbox.com/scl/fi/az4ko76jghu5rp5965874/AtlasPlaybook_v0.5.0-hotfix.zip?rlkey=7tajcohhu650z5jyskw0gunoz&st=48amd2yf&dl=1");

        private void OpenWin10Boot(object sender, RoutedEventArgs e)
            => OpenUrl("https://www.microsoft.com/software-download/windows10");

        private void OpenWin11Boot(object sender, RoutedEventArgs e)
            => OpenUrl("https://www.microsoft.com/software-download/windows11");

        // -----------------------------
        // SAFE URL OPENER
        // -----------------------------
        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    "Unable to open link:\n" + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}