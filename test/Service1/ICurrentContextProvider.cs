namespace Service1;

public interface ICurrentContextProvider
{
    CurrentContext CreateContext();
    CurrentContext GetContext();
}