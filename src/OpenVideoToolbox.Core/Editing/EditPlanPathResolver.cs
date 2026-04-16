namespace OpenVideoToolbox.Core.Editing;

public static class EditPlanPathResolver
{
    public static EditPlan ResolvePaths(EditPlan plan, string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        return plan with
        {
            Source = plan.Source with
            {
                InputPath = ResolvePath(baseDirectory, plan.Source.InputPath)
            },
            AudioTracks = plan.AudioTracks
                .Select(track => track with
                {
                    Path = ResolvePath(baseDirectory, track.Path)
                })
                .ToArray(),
            Artifacts = plan.Artifacts
                .Select(artifact => artifact with
                {
                    Path = ResolvePath(baseDirectory, artifact.Path)
                })
                .ToArray(),
            Transcript = plan.Transcript is null
                ? null
                : plan.Transcript with
                {
                    Path = ResolvePath(baseDirectory, plan.Transcript.Path)
                },
            Beats = plan.Beats is null
                ? null
                : plan.Beats with
                {
                    Path = ResolvePath(baseDirectory, plan.Beats.Path)
                },
            Subtitles = plan.Subtitles is null
                ? null
                : plan.Subtitles with
                {
                    Path = ResolvePath(baseDirectory, plan.Subtitles.Path)
                },
            Output = plan.Output with
            {
                Path = ResolvePath(baseDirectory, plan.Output.Path)
            }
        };
    }

    public static string ResolvePath(string baseDirectory, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(baseDirectory, path));
    }
}
