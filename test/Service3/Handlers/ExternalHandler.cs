using OfX.Abstractions;
using Shared.Attributes;

namespace Service3Api.Handlers;

public sealed class ExternalHandler : IDefaultReceivedHandler<ExternalDataOfAttribute>;