using OfX.Azure.ServiceBus.Statics;

namespace OfX.Azure.ServiceBus.Extensions;

internal static class Extensions
{
    extension(Type type)
    {
        internal string GetAzureServiceBusRequestQueue() =>
            string.IsNullOrEmpty(AzureServiceBusStatic.TopicPrefix)
                ? $"ofx-request-{type.Namespace}-{type.Name}".Replace('.', '-').ToLower()
                : $"{AzureServiceBusStatic.TopicPrefix}-ofx-request-{type.Namespace}-{type.Name}".Replace('.', '-')
                    .ToLower();

        internal string GetAzureServiceBusReplyQueue() =>
            string.IsNullOrEmpty(AzureServiceBusStatic.TopicPrefix)
                ? $"ofx-reply-{type.Namespace}-{type.Name}".Replace('.', '-').ToLower()
                : $"{AzureServiceBusStatic.TopicPrefix}-ofx-reply-{type.Namespace}-{type.Name}".Replace('.', '-')
                    .ToLower();
    }
}