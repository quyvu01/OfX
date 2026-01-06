namespace OfX.Constants;

/// <summary>
/// Provides global constants and configuration values for the OfX framework.
/// </summary>
public static class OfXConstants
{
    /// <summary>
    /// Gets or sets the default timeout duration for OfX requests.
    /// Default value is 30 seconds.
    /// </summary>
    public static TimeSpan DefaultRequestTimeout { get; internal set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the header key name used for error details in responses.
    /// </summary>
    public static string ErrorDetail => nameof(ErrorDetail);
}