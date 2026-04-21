using System.Text.Json.Nodes;
using Xunit;

namespace OpenVideoToolbox.Cli.Tests;

public sealed partial class CommandArtifactsIntegrationTests
{
    [Fact]
    public async Task SeparateAudio_CanWriteStructuredResultToJsonOut()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"ovt-separate-audio-json-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        var jsonOutPath = Path.Combine(outputDirectory, "separate-audio.json");
        var demucsPath = WriteExecutableScript(
            outputDirectory,
            "fake-demucs",
            """
            @echo off
            setlocal
            set "out="
            set "model=htdemucs"
            set "input="
            :parse
            if "%~1"=="" goto done
            if "%~1"=="-o" (
              set "out=%~2"
              shift
            ) else if "%~1"=="-n" (
              set "model=%~2"
              shift
            ) else (
              set "input=%~1"
            )
            shift
            goto parse
            :done
            if "%out%"=="" exit /b 2
            if "%input%"=="" exit /b 2
            set "name=%~n1"
            if not defined name (
              for %%I in ("%input%") do set "name=%%~nI"
            )
            set "trackdir=%out%\%model%\%name%"
            mkdir "%trackdir%" >nul 2>nul
            break> "%trackdir%\vocals.wav"
            break> "%trackdir%\no_vocals.wav"
            exit /b 0
            """,
            """
            #!/usr/bin/env bash
            out=""
            model="htdemucs"
            input=""
            while [ "$#" -gt 0 ]; do
              case "$1" in
                -o)
                  out="$2"
                  shift 2
                  ;;
                -n)
                  model="$2"
                  shift 2
                  ;;
                *)
                  input="$1"
                  shift
                  ;;
              esac
            done
            if [ -z "$out" ] || [ -z "$input" ]; then
              exit 2
            fi
            name="$(basename "$input")"
            name="${name%.*}"
            trackdir="$out/$model/$name"
            mkdir -p "$trackdir"
            : > "$trackdir/vocals.wav"
            : > "$trackdir/no_vocals.wav"
            """);

        try
        {
            var result = await RunCliAsync(
                "separate-audio",
                "input.mp4",
                "--output-dir",
                outputDirectory,
                "--demucs",
                demucsPath,
                "--json-out",
                jsonOutPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(jsonOutPath));

            var stdout = JsonNode.Parse(result.StdOut)!.AsObject();
            var file = JsonNode.Parse(await File.ReadAllTextAsync(jsonOutPath))!.AsObject();
            Assert.True(JsonNode.DeepEquals(stdout, file));
            Assert.Equal("separate-audio", stdout["command"]!.GetValue<string>());
            Assert.False(stdout["preview"]!.GetValue<bool>());
            var envelope = stdout["payload"]!.AsObject();
            Assert.Equal(Path.GetFullPath(outputDirectory), envelope["separateAudio"]!["outputDirectory"]!.GetValue<string>());
            Assert.Equal("htdemucs", envelope["separateAudio"]!["model"]!.GetValue<string>());
            Assert.EndsWith(Path.Combine("htdemucs", "input", "vocals.wav"), envelope["stems"]!["vocals"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(Path.Combine("htdemucs", "input", "no_vocals.wav"), envelope["stems"]!["accompaniment"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
