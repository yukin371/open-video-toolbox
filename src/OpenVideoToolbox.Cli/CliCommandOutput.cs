using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OpenVideoToolbox.Core.Editing;
using OpenVideoToolbox.Core.Execution;
using OpenVideoToolbox.Core.Serialization;

namespace OpenVideoToolbox.Cli;

internal static class CliCommandOutput
{
    public static object BuildCommandEnvelope(string command, bool preview, object payload)
    {
        return new
        {
            command,
            preview,
            payload
        };
    }

    public static int WriteCommandEnvelope(string command, bool preview, object payload, string? jsonOutPath = null, int exitCode = 0)
    {
        return WriteResult(BuildCommandEnvelope(command, preview, payload), jsonOutPath, exitCode);
    }

    public static object? BuildPlanTemplateSourcePayload(EditPlan plan)
    {
        var source = plan.Template?.Source;
        if (source is null)
        {
            return null;
        }

        return new
        {
            source.Kind,
            source.PluginId,
            source.PluginVersion
        };
    }

    public static JsonObject BuildFailedPlanCommandPayload(
        string operationName,
        object operationPayload,
        EditPlan plan,
        object? executionPreview,
        string message,
        ExecutionResult? execution = null)
    {
        var payload = new JsonObject
        {
            [operationName] = JsonSerializer.SerializeToNode(operationPayload, OpenVideoToolboxJson.Default),
            ["error"] = JsonSerializer.SerializeToNode(new
            {
                message
            }, OpenVideoToolboxJson.Default)
        };

        var templateSource = BuildPlanTemplateSourcePayload(plan);
        if (templateSource is not null)
        {
            payload["templateSource"] = JsonSerializer.SerializeToNode(templateSource, OpenVideoToolboxJson.Default);
        }

        if (executionPreview is not null)
        {
            payload["executionPreview"] = JsonSerializer.SerializeToNode(executionPreview, OpenVideoToolboxJson.Default);
        }

        if (execution is not null)
        {
            payload["execution"] = JsonSerializer.SerializeToNode(execution, OpenVideoToolboxJson.Default);
        }

        return payload;
    }

    public static JsonObject BuildFailedCommandPayload(
        string operationName,
        object operationPayload,
        string message,
        object? execution = null)
    {
        var payload = new JsonObject
        {
            [operationName] = JsonSerializer.SerializeToNode(operationPayload, OpenVideoToolboxJson.Default),
            ["error"] = JsonSerializer.SerializeToNode(new
            {
                message
            }, OpenVideoToolboxJson.Default)
        };

        if (execution is not null)
        {
            payload["execution"] = JsonSerializer.SerializeToNode(execution, OpenVideoToolboxJson.Default);
        }

        return payload;
    }

    public static string BuildExecutionFailureMessage(ExecutionResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            return result.ErrorMessage;
        }

        return result.Status switch
        {
            ExecutionStatus.Failed when result.ExitCode is { } exitCode
                => $"{result.CommandPlan.ToolName} exited with code {exitCode}.",
            ExecutionStatus.Cancelled
                => $"{result.CommandPlan.ToolName} execution was cancelled.",
            ExecutionStatus.TimedOut
                => $"{result.CommandPlan.ToolName} execution timed out.",
            _ => $"{result.CommandPlan.ToolName} execution did not succeed (status: {result.Status})."
        };
    }

    public static int FailWithCommandEnvelope(
        string command,
        bool preview,
        JsonObject payload,
        string message,
        string? jsonOutPath = null,
        int exitCode = 1)
    {
        Console.Error.WriteLine(message);
        return WriteCommandEnvelope(command, preview, payload, jsonOutPath, exitCode);
    }

    public static void WriteJson<T>(T value)
    {
        Console.WriteLine(JsonSerializer.Serialize(value, OpenVideoToolboxJson.Default));
    }

    public static void WriteOutput<T>(T value, string? jsonOutPath)
    {
        if (!string.IsNullOrWhiteSpace(jsonOutPath))
        {
            var fullPath = Path.GetFullPath(jsonOutPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, JsonSerializer.Serialize(value, OpenVideoToolboxJson.Default), Encoding.UTF8);
        }

        WriteJson(value);
    }

    public static int WriteResult<T>(T value, string? jsonOutPath, int exitCode = 0)
    {
        WriteOutput(value, jsonOutPath);
        return exitCode;
    }
}
