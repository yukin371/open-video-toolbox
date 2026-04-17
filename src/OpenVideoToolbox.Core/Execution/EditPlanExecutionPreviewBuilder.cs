using OpenVideoToolbox.Core.Editing;

namespace OpenVideoToolbox.Core.Execution;

public sealed class EditPlanExecutionPreviewBuilder
{
    private readonly FfmpegEditPlanRenderCommandBuilder _renderCommandBuilder;
    private readonly FfmpegEditPlanAudioMixCommandBuilder _audioMixCommandBuilder;

    public EditPlanExecutionPreviewBuilder()
        : this(new FfmpegEditPlanRenderCommandBuilder(), new FfmpegEditPlanAudioMixCommandBuilder())
    {
    }

    public EditPlanExecutionPreviewBuilder(
        FfmpegEditPlanRenderCommandBuilder renderCommandBuilder,
        FfmpegEditPlanAudioMixCommandBuilder audioMixCommandBuilder)
    {
        _renderCommandBuilder = renderCommandBuilder;
        _audioMixCommandBuilder = audioMixCommandBuilder;
    }

    public ExecutionPreview BuildRenderPreview(EditPlanRenderRequest request, string executablePath = "ffmpeg")
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ExecutionPreview
        {
            Operation = "render",
            PathsResolved = Path.IsPathFullyQualified(request.Plan.Output.Path),
            CommandPlan = _renderCommandBuilder.Build(request, executablePath),
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
