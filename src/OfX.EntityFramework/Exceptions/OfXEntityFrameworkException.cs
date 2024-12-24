namespace OfX.EntityFramework.Exceptions;

public static class OfXEntityFrameworkException
{
    public class EntityFrameworkDbContextNotRegister(string message) : Exception;
}