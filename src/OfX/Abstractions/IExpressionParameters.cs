namespace OfX.Abstractions;

/// <summary>
/// Represents a container for dynamic parameters used during expression evaluation
/// within the OfX mapping or resolution process.
/// </summary>
public interface IExpressionParameters
{
    /// <summary>
    /// Gets the collection of runtime parameters available to expressions.
    /// These parameters can be provided as an anonymous object or a dictionary,
    /// and are used to dynamically substitute values (e.g., <c>${offset}</c>, <c>${limit}</c>, <c>${order}</c>)
    /// in OfX expressions during evaluation.
    /// </summary>
    Dictionary<string, object> Parameters { get; }
}