namespace OfX.HotChocolate.Constants;

internal static class GraphQlConstants
{
    private const string ContextDataParametersHeader = "ofx.parameters";
    private const string ContextDataGroupIdHeader = "ofx.group.id";
    private const string ContextFieldContextHeader = "ofx.field.context";

    internal static string GetContextDataParametersHeader(string methodPath) =>
        $"{ContextDataParametersHeader}.{methodPath}";
    
    internal static string GetContextDataGroupIdHeader(string methodPath) =>
        $"{ContextDataGroupIdHeader}.{methodPath}";
    
    internal static string GetContextFieldContextHeader(string methodPath) =>
        $"{ContextFieldContextHeader}.{methodPath}";
}