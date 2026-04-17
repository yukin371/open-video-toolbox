using System.Diagnostics;

namespace OpenVideoToolbox.Core.Tests;

internal static class RealMediaSmokeTestHelper
{
    public static bool IsToolAvailable(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return false;
        }

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = toolName,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (process is null)
            {
                return false;
            }

            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static string? GetEnvironmentValue(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    public static string? GetOptionalFilePathFromEnvironment(string variableName)
    {
        var value = GetEnvironmentValue(variableName);
        if (value is null)
        {
            return null;
        }

        var fullPath = Path.GetFullPath(value);
        return File.Exists(fullPath)
            ? fullPath
            : null;
    }

    public static string? GetOptionalDirectoryPathFromEnvironment(string variableName)
    {
        var value = GetEnvironmentValue(variableName);
        if (value is null)
        {
            return null;
        }

        var fullPath = Path.GetFullPath(value);
        return Directory.Exists(fullPath)
            ? fullPath
            : null;
    }

    public static string GetToolFromEnvironmentOrDefault(string variableName, string defaultToolName)
    {
        return GetEnvironmentValue(variableName) ?? defaultToolName;
    }

    public static async Task CreateSampleVideoAsync(string outputPath, TimeSpan duration)
    {
        await RunProcessAsync(
            "ffmpeg",
            [
                "-y",
                "-f", "lavfi",
                "-i", "testsrc=size=320x240:rate=25",
                "-f", "lavfi",
                "-i", "sine=frequency=880:sample_rate=48000",
                "-t", duration.TotalSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                "-c:v", "libx264",
                "-pix_fmt", "yuv420p",
                "-c:a", "aac",
                outputPath
            ]);
    }

    public static async Task CreateSampleAudioAsync(string outputPath, TimeSpan duration)
    {
        await RunProcessAsync(
            "ffmpeg",
            [
                "-y",
                "-f", "lavfi",
                "-i", "sine=frequency=440:sample_rate=48000",
                "-t", duration.TotalSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
                "-c:a", "pcm_s16le",
                outputPath
            ]);
    }

    private static async Task RunProcessAsync(string fileName, IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process '{fileName}'.");
        var stdOut = await process.StandardOutput.ReadToEndAsync();
        var stdErr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Process '{fileName}' failed with exit code {process.ExitCode}.{Environment.NewLine}{stdOut}{Environment.NewLine}{stdErr}");
        }
    }
}
