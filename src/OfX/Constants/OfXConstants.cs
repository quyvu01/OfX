namespace OfX.Constants;

public static class OfXConstants
{
    public static TimeSpan DefaultRequestTimeout { get; internal set; } = TimeSpan.FromSeconds(30);
    public static string ErrorDetail => nameof(ErrorDetail);
}