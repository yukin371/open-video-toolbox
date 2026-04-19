using System.Diagnostics;
using System.Text;

namespace OpenVideoToolbox.Cli.Tests;

internal sealed record CliRunResult
{
    public required int ExitCode { get; init; }

    public required string StdOut { get; init; }

    public required string StdErr { get; init; }
}

internal static class CliTestProcessHelper
{
    public static async Task<CliRunResult> RunCliAsync(params string[] args)
    {
        return await RunCliAsync(environmentVariables: null, args);
    }

    public static async Task<CliRunResult> RunCliAsync(
        IReadOnlyDictionary<string, string?>? environmentVariables,
        params string[] args)
    {
        var cliAssemblyPath = typeof(TemplateCommandArtifactsBuilder).Assembly.Location;
        var repoRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(cliAssemblyPath)!, "..", "..", "..", "..", ".."));

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add(cliAssemblyPath);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        if (environmentVariables is not null)
        {
            foreach (var pair in environmentVariables)
            {
                if (pair.Value is null)
                {
                    startInfo.Environment.Remove(pair.Key);
                }
                else
                {
                    startInfo.Environment[pair.Key] = pair.Value;
                }
            }
        }

        using var process = Process.Start(startInfo)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new CliRunResult
        {
            ExitCode = process.ExitCode,
            StdOut = stdout,
            StdErr = stderr
        };
    }

    public static string WriteExecutableScript(string directory, string baseName, string windowsContent, string unixContent)
    {
        if (OperatingSystem.IsWindows())
        {
            var windowsPath = Path.Combine(directory, $"{baseName}.cmd");
            File.WriteAllText(windowsPath, windowsContent.ReplaceLineEndings("\r\n"), new UTF8Encoding(false));
            return windowsPath;
        }

        var unixPath = Path.Combine(directory, baseName);
        File.WriteAllText(unixPath, unixContent.ReplaceLineEndings("\n"), new UTF8Encoding(false));
        File.SetUnixFileMode(
            unixPath,
            UnixFileMode.UserRead
            | UnixFileMode.UserWrite
            | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead
            | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead
            | UnixFileMode.OtherExecute);
        return unixPath;
    }

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

    public static string GetToolFromEnvironmentOrDefault(string variableName, string defaultToolName)
    {
        return GetEnvironmentValue(variableName) ?? defaultToolName;
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
