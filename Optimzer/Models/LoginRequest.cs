namespace Optimzer.Models
{
    public sealed class LoginRequest
    {
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _installHash = string.Empty;
        private string _displayInstallId = string.Empty;

        public string Username
        {
            get => _username;
            set => _username = Normalize(value);
        }

        public string Password
        {
            get => _password;
            set => _password = value ?? string.Empty;
        }

        public string InstallHash
        {
            get => _installHash;
            set => _installHash = Normalize(value);
        }

        public string DisplayInstallId
        {
            get => _displayInstallId;
            set => _displayInstallId = Normalize(value);
        }

        private static string Normalize(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }
    }
}