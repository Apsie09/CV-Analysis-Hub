using CVAnalysisHub.Application.AnalysisRuns;
using CVAnalysisHub.Infrastructure.ComputerVision;
using Microsoft.Extensions.Options;

namespace CVAnalysisHub.Infrastructure.AnalysisRuns;

public sealed class ConfigurableAnalysisInferenceEngine(
    IOptions<ComputerVisionOptions> options,
    PlaceholderAnalysisInferenceEngine placeholderAnalysisInferenceEngine,
    YoloDotNetAnalysisInferenceEngine yoloDotNetAnalysisInferenceEngine) : IAnalysisInferenceEngine
{
    public Task<AnalysisInferenceResult> RunAsync(
        AnalysisInferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        return options.Value.Provider.Trim().ToLowerInvariant() switch
        {
            "yolodotnet" => yoloDotNetAnalysisInferenceEngine.RunAsync(request, cancellationToken),
            _ => placeholderAnalysisInferenceEngine.RunAsync(request, cancellationToken)
        };
    }
}
