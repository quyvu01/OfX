namespace OfX.HotChocolate.Constants;

/// <summary>
/// Constants used for storing OfX context data in HotChocolate resolver context.
/// </summary>
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