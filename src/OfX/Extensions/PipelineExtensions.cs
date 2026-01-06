using OfX.ApplicationModels;
using OfX.Registries;

namespace OfX.Extensions;

/// <summary>
/// Provides extension methods for configuring pipeline behaviors in the OfX framework.
/// </summary>
public static class PipelineExtensions
{
    /// <summary>
    /// Extension methods for OfXRegister to configure pipelines.
    /// </summary>
    extension(OfXRegister ofXRegister)
    {
        /// <summary>
        /// Registers server-side (received) pipeline behaviors.
        /// </summary>
        /// <param name="options">Configuration action for received pipeline behaviors.</param>
        /// <remarks>
        /// Received pipelines execute on the server side when processing incoming requests.
        /// They can be used for logging, validation, caching, or other cross-cutting concerns.
        /// </remarks>
        public void AddReceivedPipelines(Action<ReceivedPipeline> options)
        {
            var receivedPipeline = new ReceivedPipeline(ofXRegister.ServiceCollection);
            options.Invoke(receivedPipeline);
        }

        /// <summary>
        /// Registers client-side (send) pipeline behaviors.
        /// </summary>
        /// <param name="options">Configuration action for send pipeline behaviors.</param>
        /// <remarks>
        /// Send pipelines execute on the client side before requests are transmitted.
        /// They can be used for retry logic, exception handling, or request modification.
        /// </remarks>
        public void AddSendPipelines(Action<SendPipeline> options)
        {
            var receivedPipeline = new SendPipeline(ofXRegister.ServiceCollection);
            options.Invoke(receivedPipeline);
        }

        /// <summary>
        /// Registers custom expression pipeline behaviors.
        /// </summary>
        /// <param name="options">Configuration action for custom expression behaviors.</param>
        /// <remarks>
        /// Custom expression pipelines handle special expression syntax beyond the standard
        /// property navigation and projection capabilities.
        /// </remarks>
        public void AddCustomExpressionPipelines(Action<CustomExpressionPipeline> options)
        {
            var customExpressionPipeline = new CustomExpressionPipeline(ofXRegister.ServiceCollection);
            options.Invoke(customExpressionPipeline);
        }
    }
}