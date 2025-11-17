namespace OfX.ApplicationModels;

public sealed record OfXRequest(List<string> SelectorIds, string Expression);