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

    public static void AddSendPipelines(this OfXRegister ofXRegister, Action<SendPipeline> options)
    {
        var receivedPipeline = new SendPipeline(ofXRegister.ServiceCollection);
        options.Invoke(receivedPipeline);
    }

    public static void AddCustomExpressionPipelines(this OfXRegister ofXRegister,
        Action<CustomExpressionPipeline> options)
    {
        var customExpressionPipeline = new CustomExpressionPipeline(ofXRegister.ServiceCollection);
        options.Invoke(customExpressionPipeline);
    }
}