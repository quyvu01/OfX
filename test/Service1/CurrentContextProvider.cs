namespace Service1;

public class CurrentContextProvider : ICurrentContextProvider
{
    private static readonly AsyncLocal<CurrentContext> AsyncLocal = new();

    public CurrentContext CreateContext()
    {
        AsyncLocal.Value = new CurrentContext();
        return AsyncLocal.Value;
    }

    public CurrentContext GetContext() => AsyncLocal.Value;
}