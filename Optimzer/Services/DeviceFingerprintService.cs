using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace Optimzer.Services
{
    public sealed class DeviceFingerprintService
    {
        private const string FingerprintVersion = "FPV2";

        private string? _cachedFullHash;

        public string GetFullHashedFingerprint()
        {
            if (!string.IsNullOrWhiteSpace(_cachedFullHash))
                return _cachedFullHash;

            string machineGuid = ReadRegistryString(
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Cryptography",
                "MachineGuid");

            string installDate = ReadRegistryString(
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                "InstallDate");

            string productId = ReadRegistryString(
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                "ProductId");

            string systemDriveSerial = GetSystemDriveVolumeSerialHex();

            string combined = string.Join("|",
                FingerprintVersion,
                NormalizePart(machineGuid),
                NormalizePart(installDate),
                NormalizePart(productId),
                NormalizePart(systemDriveSerial));

            _cachedFullHash = ComputeSha256(combined);
            return _cachedFullHash;
        }

        public string GetDisplayInstallId(string fullHash)
        {
            if (string.IsNullOrWhiteSpace(fullHash))
                return $"{FingerprintVersion}-UNKNOWN";

            string normalized = NormalizeHashForDisplay(fullHash);

            if (normalized.Length >= 16)
            {
                return $"{FingerprintVersion}-{normalized.Substring(0, 4)}-{normalized.Substring(4, 4)}-{normalized.Substring(8, 4)}-{normalized.Substring(12, 4)}";
            }

            if (normalized.Length >= 12)
            {
                return $"{FingerprintVersion}-{normalized.Substring(0, 4)}-{normalized.Substring(4, 4)}-{normalized.Substring(8, 4)}";
            }

            return $"{FingerprintVersion}-{normalized}";
        }

        private static string ComputeSha256(string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            byte[] hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static string NormalizeHashForDisplay(string value)
        {
            StringBuilder builder = new StringBuilder(value.Length);

            foreach (char ch in value.Trim().ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                    builder.Append(ch);
            }

            return builder.Length == 0 ? "UNKNOWN" : builder.ToString();
        }

        private static string NormalizePart(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "NA";

            string trimmed = value.Trim().ToUpperInvariant();
            StringBuilder builder = new StringBuilder(trimmed.Length);

            foreach (char ch in trimmed)
            {
                if (char.IsLetterOrDigit(ch))
                    builder.Append(ch);
            }

            return builder.Length == 0 ? "NA" : builder.ToString();
        }

        private static string ReadRegistryString(RegistryHive hive, string subKeyPath, string valueName)
        {
            string? value = TryReadRegistryString(hive, RegistryView.Registry64, subKeyPath, valueName);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            value = TryReadRegistryString(hive, RegistryView.Registry32, subKeyPath, valueName);
            return value ?? string.Empty;
        }

        private static string? TryReadRegistryString(
            RegistryHive hive,
            RegistryView view,
            string subKeyPath,
            string valueName)
        {
            try
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
                using RegistryKey? subKey = baseKey.OpenSubKey(subKeyPath, false);

                if (subKey == null)
                    return null;

                object? value = subKey.GetValue(valueName, string.Empty);
                return ConvertRegistryValueToString(value);
            }
            catch
            {
                return null;
            }
        }

        private static string ConvertRegistryValueToString(object? value)
        {
            return value switch
            {
                null => string.Empty,
                byte[] bytes => Convert.ToHexString(bytes),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
            };
        }

        private static string GetSystemDriveVolumeSerialHex()
        {
            try
            {
                string rootPath = GetSystemDriveRootPath();
                if (string.IsNullOrWhiteSpace(rootPath))
                    return string.Empty;

                bool ok = GetVolumeInformation(
                    rootPath,
                    null,
                    0,
                    out uint serialNumber,
                    out _,
                    out _,
                    null,
                    0);

                return ok
                    ? serialNumber.ToString("X8", CultureInfo.InvariantCulture)
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetSystemDriveRootPath()
        {
            string? systemDirectory = Environment.SystemDirectory;
            if (!string.IsNullOrWhiteSpace(systemDirectory))
            {
                string? root = Path.GetPathRoot(systemDirectory);
                if (!string.IsNullOrWhiteSpace(root))
                    return root;
            }

            string? systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            if (!string.IsNullOrWhiteSpace(systemDrive))
                return systemDrive.EndsWith("\\", StringComparison.Ordinal) ? systemDrive : systemDrive + "\\";

            return @"C:\";
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetVolumeInformation(
            string lpRootPathName,
            StringBuilder? lpVolumeNameBuffer,
            int nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            StringBuilder? lpFileSystemNameBuffer,
            int nFileSystemNameSize);
    }
}