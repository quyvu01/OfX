namespace OfX.EntityFrameworkCore.Exceptions;

public static class OfXEntityFrameworkException
{
    public class EntityFrameworkDbContextNotRegister(string message) : Exception;
}