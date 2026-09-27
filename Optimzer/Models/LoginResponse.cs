namespace Optimzer.Models
{
    public sealed class LoginResponse
    {
        private string _username = string.Empty;
        private string _installHash = string.Empty;
        private string _displayInstallId = string.Empty;
        private LicenseInfo _licenseInfo = new LicenseInfo();

        public bool Allowed { get; set; }

        public string Username
        {
            get => _username;
            set => _username = Normalize(value);
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

        public LicenseInfo LicenseInfo
        {
            get => _licenseInfo;
            set => _licenseInfo = value?.Clone() ?? new LicenseInfo();
        }

        public bool IsProvisioned => Allowed && LicenseInfo.IsProvisioned;

        public LoginResponse Clone()
        {
            return new LoginResponse
            {
                Allowed = Allowed,
                Username = Username,
                InstallHash = InstallHash,
                DisplayInstallId = DisplayInstallId,
                LicenseInfo = LicenseInfo.Clone()
            };
        }

        private static string Normalize(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }
    }
}