using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CSharpUtil.IntegrationTests;

/// <summary>
/// Thin wrapper around the dotnet CLI. Every command runs in its own process and a non-zero
/// exit code surfaces both stdout and stderr, so a failing integration test explains itself.
/// </summary>
internal static class DotnetCli
{
    private const int TimeoutMilliseconds = 120_000;

    public static string Run(string workingDirectory, params string[] args)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            // The CLI and consumer programs emit UTF-8 (including non-ASCII messages); decode it explicitly
            // so assertions on Chinese text do not compare mojibake.
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (string arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // Keep the CLI quiet and telemetry-free so captured output only contains real results.
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the dotnet CLI.");

        // Read both pipes concurrently, otherwise a full buffer deadlocks the child process.
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(TimeoutMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException($"dotnet {string.Join(' ', args)} timed out after {TimeoutMilliseconds} ms in '{workingDirectory}'.");
        }

        string output = stdout.GetAwaiter().GetResult();
        string error = stderr.GetAwaiter().GetResult();

        // Opt-in step timing: set NETUTIL_IT_STEPS=<file> to find which CLI call dominates the run.
        string? stepLog = Environment.GetEnvironmentVariable("NETUTIL_IT_STEPS");
        if (!string.IsNullOrEmpty(stepLog))
        {
            File.AppendAllText(
                stepLog,
                $"{DateTime.Now:HH:mm:ss.fff} {process.ExitCode} dotnet {string.Join(' ', args)} @ {workingDirectory}{Environment.NewLine}");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet {string.Join(' ', args)} exited with code {process.ExitCode} in '{workingDirectory}'." +
                $"{Environment.NewLine}--- stdout ---{Environment.NewLine}{output}" +
                $"{Environment.NewLine}--- stderr ---{Environment.NewLine}{error}");
        }

        return output;
    }
}
