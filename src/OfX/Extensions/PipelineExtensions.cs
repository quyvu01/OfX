using OfX.ApplicationModels;
using OfX.Registries;

namespace OfX.Extensions;

public static class PipelineExtensions
{
    public static void AddReceivedPipelines(this OfXRegister ofXRegister, Action<ReceivedPipeline> options)
    {
        var receivedPipeline = new ReceivedPipeline(ofXRegister.ServiceCollection);
        options.Invoke(receivedPipeline);
    }
}