using OfX.ApplicationModels;
using OfX.Registries;

namespace OfX.Extensions;

public static class PipelineExtensions
{
    extension(OfXRegister ofXRegister)
    {
        public void AddReceivedPipelines(Action<ReceivedPipeline> options)
        {
            var receivedPipeline = new ReceivedPipeline(ofXRegister.ServiceCollection);
            options.Invoke(receivedPipeline);
        }

        public void AddSendPipelines(Action<SendPipeline> options)
        {
            var receivedPipeline = new SendPipeline(ofXRegister.ServiceCollection);
            options.Invoke(receivedPipeline);
        }

        public void AddCustomExpressionPipelines(Action<CustomExpressionPipeline> options)
        {
            var customExpressionPipeline = new CustomExpressionPipeline(ofXRegister.ServiceCollection);
            options.Invoke(customExpressionPipeline);
        }
    }
}