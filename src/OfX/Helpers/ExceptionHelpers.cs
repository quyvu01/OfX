namespace OfX.Helpers;

public static class ExceptionHelpers
{
    public static void ThrowIfNull(object obj)
    {
        if (obj is null)
            throw new NullReferenceException();
    }
}