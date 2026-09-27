using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Optimzer.Core;

namespace Optimzer.Services
{
    public sealed class PowerShellRunner
    {
        private const int DefaultExecutionTimeoutSeconds = 600;
        private const string OptimizerScriptRelativePath = @"Assets\Scripts\Optimizer.ps1";

        public async Task<Result> RunOptimizerAsync(CancellationToken cancellationToken = default)
        {
            return await RunInternalAsync(
                Array.Empty<string>(),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<Result> RunSelectedTweaksAsync(IEnumerable<string> selectedTweaks, CancellationToken cancellationToken = default)
        {
            if (selectedTweaks == null)
                return Result.Fail("No tweak selection was provided.");

            string[] normalizedTweaks = selectedTweaks
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (normalizedTweaks.Length == 0)
                return Result.Fail("No valid tweaks were selected.");

            return await RunInternalAsync(
                normalizedTweaks,
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<Result> RunInternalAsync(string[] selectedTweaks, CancellationToken cancellationToken)
        {
            string scriptPath = GetScriptPath();
            if (!File.Exists(scriptPath))
                return Result.Fail($"Optimizer script was not found: {scriptPath}");

            string? powerShellPath = ResolvePowerShellPath();
            if (string.IsNullOrWhiteSpace(powerShellPath))
                return Result.Fail("PowerShell executable was not found on this system.");

            string tweakPayload = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(selectedTweaks)));

            string arguments = BuildArguments(scriptPath, tweakPayload);

            using Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = powerShellPath,
                    Arguments = arguments,
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = false
                },
                EnableRaisingEvents = true
            };

            try
            {
                if (!process.Start())
                    return Result.Fail("Failed to start the optimizer process.");

                Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

                using CancellationTokenSource timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(DefaultExecutionTimeoutSeconds));
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                try
                {
                    await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    TryTerminateProcess(process);

                    if (cancellationToken.IsCancellationRequested)
                        return Result.Fail("Optimizer execution was cancelled.");

                    return Result.Fail("Optimizer execution timed out.");
                }

                string standardOutput = (await standardOutputTask.ConfigureAwait(false)).Trim();
                string standardError = (await standardErrorTask.ConfigureAwait(false)).Trim();

                if (process.ExitCode != 0)
                {
                    string failureMessage = BuildFailureMessage(process.ExitCode, standardError, standardOutput);
                    return Result.Fail(failureMessage);
                }

                string successMessage = string.IsNullOrWhiteSpace(standardOutput)
                    ? "Optimizer execution completed successfully."
                    : standardOutput;

                return Result.Ok(successMessage);
            }
            catch (Exception ex)
            {
                TryTerminateProcess(process);
                return Result.Fail("Optimizer execution failed: " + ex.Message);
            }
        }

        private static string GetScriptPath()
        {
            return Path.Combine(AppContext.BaseDirectory, OptimizerScriptRelativePath);
        }

        private static string BuildArguments(string scriptPath, string tweakPayload)
        {
            return string.Create(
                256 + scriptPath.Length + tweakPayload.Length,
                (scriptPath, tweakPayload),
                static (span, state) =>
                {
                    string value =
                        "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass " +
                        $"-File \"{state.scriptPath}\" " +
                        $"-SelectedTweaksBase64 \"{state.tweakPayload}\"";

                    value.AsSpan().CopyTo(span);
                });
        }

        private static string? ResolvePowerShellPath()
        {
            string systemDirectory = Environment.SystemDirectory;

            string[] candidates =
            {
                Path.Combine(systemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"PowerShell\7\pwsh.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"PowerShell\7\pwsh.exe"),
                "powershell.exe",
                "pwsh.exe"
            };

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                if (candidate.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                    candidate.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
                {
                    if (File.Exists(candidate))
                        return candidate;
                }
                else
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string BuildFailureMessage(int exitCode, string standardError, string standardOutput)
        {
            if (!string.IsNullOrWhiteSpace(standardError))
                return $"Optimizer failed with exit code {exitCode}: {standardError}";

            if (!string.IsNullOrWhiteSpace(standardOutput))
                return $"Optimizer failed with exit code {exitCode}: {standardOutput}";

            return $"Optimizer failed with exit code {exitCode}.";
        }

        private static void TryTerminateProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
            }
        }
    }
}