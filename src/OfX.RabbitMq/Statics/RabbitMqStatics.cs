using OfX.RabbitMq.ApplicationModels;

namespace OfX.RabbitMq.Statics;

internal static class RabbitMqStatics
{
    internal static string RabbitMqHost { get; set; }
    internal static string RabbitVirtualHost { get; set; }
    internal static int RabbitMqPort { get; set; }
    internal static string RabbitMqUserName { get; set; }
    internal static string RabbitMqPassword { get; set; }
}