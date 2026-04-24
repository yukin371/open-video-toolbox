using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

public sealed class EditPlanExecutionPreviewBuilder
{
    private readonly FfmpegEditPlanRenderCommandBuilder _renderCommandBuilder;
    private readonly FfmpegTimelineRenderCommandBuilder _timelineRenderCommandBuilder;
    private readonly FfmpegEditPlanAudioMixCommandBuilder _audioMixCommandBuilder;

    public EditPlanExecutionPreviewBuilder()
        : this(
            new FfmpegEditPlanRenderCommandBuilder(),
            new FfmpegEditPlanAudioMixCommandBuilder(),
            new FfmpegTimelineRenderCommandBuilder())
    {
    }

    public EditPlanExecutionPreviewBuilder(
        FfmpegEditPlanRenderCommandBuilder renderCommandBuilder,
        FfmpegEditPlanAudioMixCommandBuilder audioMixCommandBuilder,
        FfmpegTimelineRenderCommandBuilder? timelineRenderCommandBuilder = null)
    {
        _renderCommandBuilder = renderCommandBuilder;
        _audioMixCommandBuilder = audioMixCommandBuilder;
        _timelineRenderCommandBuilder = timelineRenderCommandBuilder ?? new FfmpegTimelineRenderCommandBuilder();
    }

    public ExecutionPreview BuildRenderPreview(EditPlanRenderRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ExecutionPreview
        {
            Operation = "render",
            PathsResolved = Path.IsPathFullyQualified(request.Plan.Output.Path),
            CommandPlan = request.Plan.Timeline is null
                ? _renderCommandBuilder.Build(request, executablePath)
                : _timelineRenderCommandBuilder.Build(request, executablePath),
            ProducedPaths = EditPlanRenderRunner.BuildProducedPaths(request.Plan),
            SideEffects = EditPlanRenderRunner.BuildSideEffects(request.Plan)
        };
    }

    public ExecutionPreview BuildAudioMixPreview(EditPlanAudioMixRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ExecutionPreview
        {
            Operation = "mix-audio",
            PathsResolved = Path.IsPathFullyQualified(request.OutputPath),
            CommandPlan = _audioMixCommandBuilder.Build(request, executablePath),
            ProducedPaths = EditPlanAudioMixRunner.BuildProducedPaths(request),
            SideEffects = []
        };
    }
}
