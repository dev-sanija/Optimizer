using System;

namespace Optimzer.Models
{
    public sealed class LicenseInfo
    {
        private string _installHash = string.Empty;
        private string _displayInstallId = string.Empty;
        private string _username = string.Empty;
        private DateTime _createdAtUtc;
        private DateTime _updatedAtUtc;
        private DateTime? _paymentVerifiedAtUtc;

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

        public bool PaymentVerified { get; set; }
        public bool AccountConfigured { get; set; }
        public bool IsActive { get; set; }

        public string Username
        {
            get => _username;
            set => _username = Normalize(value);
        }

        public DateTime CreatedAtUtc
        {
            get => _createdAtUtc;
            set => _createdAtUtc = NormalizeUtc(value);
        }

        public DateTime UpdatedAtUtc
        {
            get => _updatedAtUtc;
            set => _updatedAtUtc = NormalizeUtc(value);
        }

        public DateTime? PaymentVerifiedAtUtc
        {
            get => _paymentVerifiedAtUtc;
            set => _paymentVerifiedAtUtc = value.HasValue ? NormalizeUtc(value.Value) : null;
        }

        public bool IsProvisioned => PaymentVerified && AccountConfigured && IsActive;

        public LicenseInfo Clone()
        {
            return new LicenseInfo
            {
                InstallHash = InstallHash,
                DisplayInstallId = DisplayInstallId,
                PaymentVerified = PaymentVerified,
                AccountConfigured = AccountConfigured,
                IsActive = IsActive,
                Username = Username,
                CreatedAtUtc = CreatedAtUtc,
                UpdatedAtUtc = UpdatedAtUtc,
                PaymentVerifiedAtUtc = PaymentVerifiedAtUtc
            };
        }

        private static string Normalize(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            if (value == DateTime.MinValue || value == DateTime.MaxValue)
                return value;

            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}