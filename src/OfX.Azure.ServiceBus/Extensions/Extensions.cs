using OfX.Azure.ServiceBus.Statics;

namespace OfX.Azure.ServiceBus.Extensions;

internal static class Extensions
{
    internal static string GetAzureServiceBusRequestQueue(this Type type) =>
        string.IsNullOrEmpty(AzureServiceBusStatic.TopicPrefix)
            ? $"ofx-request-{type.Namespace}-{type.Name}".Replace('.', '-').ToLower()
            : $"{AzureServiceBusStatic.TopicPrefix}-ofx-request-{type.Namespace}-{type.Name}".Replace('.', '-')
                .ToLower();

    internal static string GetAzureServiceBusReplyQueue(this Type type) =>
        string.IsNullOrEmpty(AzureServiceBusStatic.TopicPrefix)
            ? $"ofx-reply-{type.Namespace}-{type.Name}".Replace('.', '-').ToLower()
            : $"{AzureServiceBusStatic.TopicPrefix}-ofx-reply-{type.Namespace}-{type.Name}".Replace('.', '-')
                .ToLower();
}