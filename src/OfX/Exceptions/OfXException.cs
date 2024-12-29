namespace OfX.Exceptions;

public static class OfXException
{
    public sealed class RequestMustNotBeAddMoreThanOneTimes()
        : Exception("Request must not be add more than one times!");

    public sealed class AttributesFromNamespaceShouldBeAdded() :
        Exception("Attributes from namespaces should be added!");
}