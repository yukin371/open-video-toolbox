using OpenVideoToolbox.Core.Execution;

namespace OpenVideoToolbox.Core.Tests;

internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Func<ProcessExecutionRequest, Task<ExecutionResult>> _handler;

    public FakeProcessRunner(Func<ProcessExecutionRequest, Task<ExecutionResult>> handler)
    {
        _handler = handler;
    }

    public ProcessExecutionRequest? LastRequest { get; private set; }

    public Task<ExecutionResult> ExecuteAsync(ProcessExecutionRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return _handler(request);
    }
}

