using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Optimzer.Core;
using Optimzer.Models;

namespace Optimzer.Services
{
    public sealed class ApiService
    {
        public const string GoogleAppsScriptBaseUrl = "https://script.google.com/macros/s/AKfycbz7vKUmZ3Q3ZsI42XyYoEl887UBUFSfYtF3XOXD1Kajd_rIqKNTCLc9pYzjYvbNzAqzkA/exec";
        public const string GoogleSheetId = "1VHgXftjqyyc6sn60PlnLEK90L7WSvomOFyqf1BpSurM";

        public const string PayHereSandboxCheckoutUrl = "https://sandbox.payhere.lk/pay/checkout";
        public const string PayHereLiveCheckoutUrl = "https://www.payhere.lk/pay/checkout";
        public const string PayHereNotifyUrl = "https://your-domain.example/payhere/notify";
        public const string PayHereReturnUrl = "https://your-domain.example/payhere/return";
        public const string PayHereCancelUrl = "https://your-domain.example/payhere/cancel";
        public const string ConnectivityProbeUrl = "https://clients3.google.com/generate_204";

        private const int PasswordHashIterations = 100_000;
        private const int PasswordSaltSizeBytes = 16;
        private const int PasswordHashSizeBytes = 32;
        private const int MinUsernameLength = 4;
        private const int MaxUsernameLength = 32;
        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 128;

        private static readonly Uri _appsScriptUri = new Uri(GoogleAppsScriptBaseUrl, UriKind.Absolute);
        private static readonly Uri _connectivityProbeUri = new Uri(ConnectivityProbeUrl, UriKind.Absolute);

        private static readonly HttpClient _apiClient = CreateApiHttpClient();
        private static readonly HttpClient _probeClient = CreateProbeHttpClient();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public string GetPaymentPortalUrl(bool useLive)
        {
            return useLive ? PayHereLiveCheckoutUrl : PayHereSandboxCheckoutUrl;
        }

        public async Task<bool> HasInternetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _connectivityProbeUri);
                using HttpResponseMessage response = await _probeClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Result<string>> GetMainScriptUrlAsync()
        {
            try
            {
                Uri requestUri = BuildUri(new Dictionary<string, string>(1)
                {
                    ["action"] = "get_main_script_url"
                });

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                using HttpResponseMessage response = await _apiClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    CancellationToken.None).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return Result<string>.Fail("Unable to load dashboard script URL.");

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                ApiEnvelope<MainScriptUrlDto>? envelope = DeserializeEnvelope<MainScriptUrlDto>(json);

                if (envelope == null)
                    return Result<string>.Fail("The dashboard service returned an invalid response.");

                if (!envelope.Success || envelope.Data == null || string.IsNullOrWhiteSpace(envelope.Data.MainScriptUrl))
                {
                    return Result<string>.Fail(
                        string.IsNullOrWhiteSpace(envelope.Message)
                            ? "Dashboard script URL is not available."
                            : envelope.Message);
                }

                return Result<string>.Ok(
                    envelope.Data.MainScriptUrl.Trim(),
                    string.IsNullOrWhiteSpace(envelope.Message)
                        ? "Dashboard script URL loaded successfully."
                        : envelope.Message);
            }
            catch (TaskCanceledException)
            {
                return Result<string>.Fail("Dashboard script request timed out.");
            }
            catch (HttpRequestException)
            {
                return Result<string>.Fail("Unable to reach the dashboard service.");
            }
            catch (JsonException)
            {
                return Result<string>.Fail("The dashboard service returned an invalid response.");
            }
            catch
            {
                return Result<string>.Fail("An unexpected error occurred while loading the dashboard script URL.");
            }
        }

        public async Task<LicenseInfo> GetInstallStateAsync(string installHash, string displayInstallId)
        {
            string normalizedInstallHash = NormalizeHash(installHash);
            string normalizedDisplayInstallId = NormalizeDisplayInstallId(displayInstallId);

            if (string.IsNullOrWhiteSpace(normalizedInstallHash) &&
                string.IsNullOrWhiteSpace(normalizedDisplayInstallId))
            {
                return CreateInactiveLicenseInfo(string.Empty, string.Empty);
            }

            try
            {
                Uri requestUri = BuildUri(new Dictionary<string, string>(3)
                {
                    ["action"] = "get_install_state",
                    ["installHash"] = normalizedInstallHash,
                    ["displayInstallId"] = normalizedDisplayInstallId
                });

                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                using HttpResponseMessage response = await _apiClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    CancellationToken.None).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return CreateInactiveLicenseInfo(normalizedInstallHash, normalizedDisplayInstallId);

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                ApiEnvelope<LicenseInfoDto>? envelope = DeserializeEnvelope<LicenseInfoDto>(json);

                if (envelope == null || !envelope.Success || envelope.Data == null)
                    return CreateInactiveLicenseInfo(normalizedInstallHash, normalizedDisplayInstallId);

                return MapToLicenseInfo(envelope.Data, normalizedInstallHash, normalizedDisplayInstallId);
            }
            catch
            {
                return CreateInactiveLicenseInfo(normalizedInstallHash, normalizedDisplayInstallId);
            }
        }

        public async Task<Result<LicenseInfo>> CompleteOrUpdateAccountAsync(
            string installHash,
            string displayInstallId,
            string username,
            string password)
        {
            string normalizedInstallHash = NormalizeHash(installHash);
            string normalizedDisplayInstallId = NormalizeDisplayInstallId(displayInstallId);
            string normalizedUsername = NormalizeUsername(username);

            if (string.IsNullOrWhiteSpace(normalizedDisplayInstallId))
                return Result<LicenseInfo>.Fail("Invalid Install ID.");

            if (!IsValidUsername(normalizedUsername))
                return Result<LicenseInfo>.Fail("Username must be 4-32 characters and contain only letters, numbers, '.', '_' or '-'.");

            if (!IsValidPassword(password))
                return Result<LicenseInfo>.Fail("Password must be between 8 and 128 characters.");

            string passwordHash = HashPassword(password);

            Dictionary<string, string> payload = new Dictionary<string, string>(5)
            {
                ["action"] = "complete_or_update_account",
                ["installHash"] = normalizedInstallHash,
                ["displayInstallId"] = normalizedDisplayInstallId,
                ["username"] = normalizedUsername,
                ["passwordHash"] = passwordHash
            };

            return await PostForLicenseInfoAsync(payload, normalizedInstallHash, normalizedDisplayInstallId).ConfigureAwait(false);
        }

        public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
        {
            if (request == null)
                return Result<LoginResponse>.Fail("Invalid login request.");

            string normalizedInstallHash = NormalizeHash(request.InstallHash);
            string normalizedDisplayInstallId = NormalizeDisplayInstallId(request.DisplayInstallId);
            string normalizedUsername = NormalizeUsername(request.Username);

            if (string.IsNullOrWhiteSpace(normalizedDisplayInstallId))
                return Result<LoginResponse>.Fail("Invalid Install ID.");

            if (!IsValidUsername(normalizedUsername))
                return Result<LoginResponse>.Fail("Invalid username.");

            if (request.Password == null || request.Password.Length == 0)
                return Result<LoginResponse>.Fail("Password is required.");

            try
            {
                Dictionary<string, string> payload = new Dictionary<string, string>(4)
                {
                    ["action"] = "login",
                    ["installHash"] = normalizedInstallHash,
                    ["displayInstallId"] = normalizedDisplayInstallId,
                    ["username"] = normalizedUsername
                };

                using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, _appsScriptUri)
                {
                    Content = new FormUrlEncodedContent(payload)
                };

                using HttpResponseMessage response = await _apiClient.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseContentRead,
                    CancellationToken.None).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return Result<LoginResponse>.Fail("Unable to reach the login service.");

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                ApiEnvelope<LoginResponseDto>? envelope = DeserializeEnvelope<LoginResponseDto>(json);

                if (envelope == null)
                    return Result<LoginResponse>.Fail("The login service returned an invalid response.");

                if (!envelope.Success || envelope.Data == null)
                    return Result<LoginResponse>.Fail(string.IsNullOrWhiteSpace(envelope.Message) ? "Login failed." : envelope.Message);

                if (string.IsNullOrWhiteSpace(envelope.Data.StoredPasswordHash))
                    return Result<LoginResponse>.Fail("Login failed.");

                bool passwordOk = VerifyPassword(request.Password, envelope.Data.StoredPasswordHash);
                if (!passwordOk)
                    return Result<LoginResponse>.Fail("Invalid password.");

                LoginResponse result = MapToLoginResponse(envelope.Data, normalizedInstallHash, normalizedDisplayInstallId);

                if (!result.Allowed)
                    return Result<LoginResponse>.Fail(string.IsNullOrWhiteSpace(envelope.Message) ? "Login failed." : envelope.Message);

                return Result<LoginResponse>.Ok(
                    result,
                    string.IsNullOrWhiteSpace(envelope.Message) ? "Login successful." : envelope.Message);
            }
            catch (TaskCanceledException)
            {
                return Result<LoginResponse>.Fail("Login request timed out.");
            }
            catch (HttpRequestException)
            {
                return Result<LoginResponse>.Fail("Unable to reach the login service.");
            }
            catch (JsonException)
            {
                return Result<LoginResponse>.Fail("The login service returned an invalid response.");
            }
            catch
            {
                return Result<LoginResponse>.Fail("An unexpected error occurred during login.");
            }
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
                return false;

            string[] parts = storedHash.Split('$');
            if (parts.Length != 4)
                return false;

            if (!string.Equals(parts[0], "PBKDF2", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int iterations) || iterations <= 0)
                return false;

            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash = Convert.FromBase64String(parts[3]);

                if (salt.Length == 0 || expectedHash.Length == 0)
                    return false;

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private async Task<Result<LicenseInfo>> PostForLicenseInfoAsync(
            Dictionary<string, string> payload,
            string installHash,
            string displayInstallId)
        {
            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _appsScriptUri)
                {
                    Content = new FormUrlEncodedContent(payload)
                };

                using HttpResponseMessage response = await _apiClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    CancellationToken.None).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return Result<LicenseInfo>.Fail("Unable to reach the activation service.");

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                ApiEnvelope<LicenseInfoDto>? envelope = DeserializeEnvelope<LicenseInfoDto>(json);

                if (envelope == null)
                    return Result<LicenseInfo>.Fail("The activation service returned an invalid response.");

                if (!envelope.Success || envelope.Data == null)
                    return Result<LicenseInfo>.Fail(string.IsNullOrWhiteSpace(envelope.Message) ? "Request failed." : envelope.Message);

                LicenseInfo licenseInfo = MapToLicenseInfo(envelope.Data, installHash, displayInstallId);
                return Result<LicenseInfo>.Ok(
                    licenseInfo,
                    string.IsNullOrWhiteSpace(envelope.Message) ? "Request completed successfully." : envelope.Message);
            }
            catch (TaskCanceledException)
            {
                return Result<LicenseInfo>.Fail("Request timed out.");
            }
            catch (HttpRequestException)
            {
                return Result<LicenseInfo>.Fail("Unable to reach the activation service.");
            }
            catch (JsonException)
            {
                return Result<LicenseInfo>.Fail("The activation service returned an invalid response.");
            }
            catch
            {
                return Result<LicenseInfo>.Fail("An unexpected error occurred while processing the request.");
            }
        }

        private static HttpClient CreateApiHttpClient()
        {
            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(45)
            };

            client.DefaultRequestHeaders.ConnectionClose = false;
            return client;
        }

        private static HttpClient CreateProbeHttpClient()
        {
            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(3)
            };

            client.DefaultRequestHeaders.ConnectionClose = false;
            return client;
        }

        private static ApiEnvelope<T>? DeserializeEnvelope<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            return JsonSerializer.Deserialize<ApiEnvelope<T>>(json, _jsonOptions);
        }

        private static LicenseInfo CreateInactiveLicenseInfo(string installHash, string displayInstallId)
        {
            DateTime utcNow = DateTime.UtcNow;

            return new LicenseInfo
            {
                InstallHash = installHash,
                DisplayInstallId = displayInstallId,
                PaymentVerified = false,
                AccountConfigured = false,
                IsActive = false,
                Username = string.Empty,
                CreatedAtUtc = utcNow,
                UpdatedAtUtc = utcNow,
                PaymentVerifiedAtUtc = null
            };
        }

        private static LicenseInfo MapToLicenseInfo(LicenseInfoDto dto, string installHash, string displayInstallId)
        {
            return new LicenseInfo
            {
                InstallHash = string.IsNullOrWhiteSpace(dto.InstallHash) ? installHash : NormalizeHash(dto.InstallHash),
                DisplayInstallId = string.IsNullOrWhiteSpace(dto.DisplayInstallId) ? displayInstallId : NormalizeDisplayInstallId(dto.DisplayInstallId),
                PaymentVerified = dto.PaymentVerified,
                AccountConfigured = dto.AccountConfigured,
                IsActive = dto.IsActive,
                Username = NormalizeUsername(dto.Username),
                CreatedAtUtc = dto.CreatedAtUtc ?? DateTime.UtcNow,
                UpdatedAtUtc = dto.UpdatedAtUtc ?? DateTime.UtcNow,
                PaymentVerifiedAtUtc = dto.PaymentVerifiedAtUtc
            };
        }

        private static LoginResponse MapToLoginResponse(LoginResponseDto dto, string installHash, string displayInstallId)
        {
            LicenseInfo licenseInfo = dto.LicenseInfo == null
                ? CreateInactiveLicenseInfo(installHash, displayInstallId)
                : MapToLicenseInfo(dto.LicenseInfo, installHash, displayInstallId);

            return new LoginResponse
            {
                Allowed = dto.Allowed,
                Username = string.IsNullOrWhiteSpace(dto.Username) ? licenseInfo.Username : NormalizeUsername(dto.Username),
                InstallHash = licenseInfo.InstallHash,
                DisplayInstallId = licenseInfo.DisplayInstallId,
                LicenseInfo = licenseInfo
            };
        }

        private static bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            if (username.Length < MinUsernameLength || username.Length > MaxUsernameLength)
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

        private static bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) &&
                   password.Length >= MinPasswordLength &&
                   password.Length <= MaxPasswordLength;
        }

        private static string NormalizeUsername(string username)
        {
            return (username ?? string.Empty).Trim();
        }

        private static string NormalizeHash(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string trimmed = value.Trim().ToUpperInvariant();
            StringBuilder builder = new StringBuilder(trimmed.Length);

            foreach (char ch in trimmed)
            {
                if (char.IsLetterOrDigit(ch))
                    builder.Append(ch);
            }

            return builder.ToString();
        }

        private static string NormalizeDisplayInstallId(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }

        private static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(PasswordSaltSizeBytes);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                PasswordHashIterations,
                HashAlgorithmName.SHA256,
                PasswordHashSizeBytes);

            return $"PBKDF2${PasswordHashIterations.ToString(CultureInfo.InvariantCulture)}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        private static Uri BuildUri(IReadOnlyDictionary<string, string> queryParameters)
        {
            StringBuilder builder = new StringBuilder(GoogleAppsScriptBaseUrl);

            bool hasQuery = GoogleAppsScriptBaseUrl.Contains("?", StringComparison.Ordinal);
            bool firstParameter = true;

            if (!hasQuery)
            {
                builder.Append('?');
            }
            else if (!GoogleAppsScriptBaseUrl.EndsWith("?", StringComparison.Ordinal) &&
                     !GoogleAppsScriptBaseUrl.EndsWith("&", StringComparison.Ordinal))
            {
                builder.Append('&');
            }

            foreach (KeyValuePair<string, string> pair in queryParameters)
            {
                if (!firstParameter)
                    builder.Append('&');

                builder.Append(Uri.EscapeDataString(pair.Key));
                builder.Append('=');
                builder.Append(Uri.EscapeDataString(pair.Value ?? string.Empty));

                firstParameter = false;
            }

            return new Uri(builder.ToString(), UriKind.Absolute);
        }

        private sealed class ApiEnvelope<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }
        }

        private sealed class LicenseInfoDto
        {
            public string InstallHash { get; set; } = string.Empty;
            public string DisplayInstallId { get; set; } = string.Empty;
            public bool PaymentVerified { get; set; }
            public bool AccountConfigured { get; set; }
            public bool IsActive { get; set; }
            public string Username { get; set; } = string.Empty;
            public DateTime? CreatedAtUtc { get; set; }
            public DateTime? UpdatedAtUtc { get; set; }
            public DateTime? PaymentVerifiedAtUtc { get; set; }
        }

        private sealed class LoginResponseDto
        {
            public bool Allowed { get; set; }
            public string Username { get; set; } = string.Empty;
            public string StoredPasswordHash { get; set; } = string.Empty;
            public LicenseInfoDto? LicenseInfo { get; set; }
        }

        private sealed class MainScriptUrlDto
        {
            public string MainScriptUrl { get; set; } = string.Empty;
        }
    }
}