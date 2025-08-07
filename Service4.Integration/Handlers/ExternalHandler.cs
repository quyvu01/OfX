using OfX.Abstractions;
using Shared.Attributes;

namespace Service4.Integration.Handlers;

public sealed class ExternalHandler : IDefaultReceivedHandler<ExternalDataOfAttribute>;