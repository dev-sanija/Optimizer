using System;
using Optimzer.Models;

namespace Optimzer.Core
{
    public static class AppSession
    {
        private static readonly object _sync = new object();

        public static bool IsAuthenticated { get; private set; }
        public static string Username { get; private set; } = string.Empty;
        public static string InstallHash { get; private set; } = string.Empty;
        public static string DisplayInstallId { get; private set; } = string.Empty;
        public static LicenseInfo CurrentLicense { get; private set; } = CreateEmptyLicense();

        public static void SetAuthenticated(LoginResponse response)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            lock (_sync)
            {
                IsAuthenticated = true;
                Username = Normalize(response.Username);
                InstallHash = Normalize(response.InstallHash);
                DisplayInstallId = Normalize(response.DisplayInstallId);
                CurrentLicense = CloneLicense(response.LicenseInfo);
            }
        }

        public static void Clear()
        {
            lock (_sync)
            {
                IsAuthenticated = false;
                Username = string.Empty;
                InstallHash = string.Empty;
                DisplayInstallId = string.Empty;
                CurrentLicense = CreateEmptyLicense();
            }
        }

        public static SessionSnapshot Snapshot()
        {
            lock (_sync)
            {
                return new SessionSnapshot(
                    IsAuthenticated,
                    Username,
                    InstallHash,
                    DisplayInstallId,
                    CloneLicense(CurrentLicense));
            }
        }

        private static string Normalize(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        private static LicenseInfo CloneLicense(LicenseInfo? source)
        {
            if (source == null)
                return CreateEmptyLicense();

            return new LicenseInfo
            {
                InstallHash = Normalize(source.InstallHash),
                DisplayInstallId = Normalize(source.DisplayInstallId),
                PaymentVerified = source.PaymentVerified,
                AccountConfigured = source.AccountConfigured,
                IsActive = source.IsActive,
                Username = Normalize(source.Username),
                CreatedAtUtc = source.CreatedAtUtc,
                UpdatedAtUtc = source.UpdatedAtUtc,
                PaymentVerifiedAtUtc = source.PaymentVerifiedAtUtc
            };
        }

        private static LicenseInfo CreateEmptyLicense()
        {
            return new LicenseInfo
            {
                InstallHash = string.Empty,
                DisplayInstallId = string.Empty,
                PaymentVerified = false,
                AccountConfigured = false,
                IsActive = false,
                Username = string.Empty,
                CreatedAtUtc = DateTime.MinValue,
                UpdatedAtUtc = DateTime.MinValue,
                PaymentVerifiedAtUtc = null
            };
        }
    }

    public sealed class SessionSnapshot
    {
        public bool IsAuthenticated { get; }
        public string Username { get; }
        public string InstallHash { get; }
        public string DisplayInstallId { get; }
        public LicenseInfo CurrentLicense { get; }

        public SessionSnapshot(
            bool isAuthenticated,
            string username,
            string installHash,
            string displayInstallId,
            LicenseInfo currentLicense)
        {
            IsAuthenticated = isAuthenticated;
            Username = username ?? string.Empty;
            InstallHash = installHash ?? string.Empty;
            DisplayInstallId = displayInstallId ?? string.Empty;
            CurrentLicense = currentLicense ?? new LicenseInfo();
        }
    }
}