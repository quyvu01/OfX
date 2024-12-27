namespace OfX.Exceptions;

public static class OfXException
{
    public sealed class RequestMustNotBeAddMoreThanOneTimes()
        : Exception("Request must not be add more than one times!");

    public sealed class HandlersFromNamespaceShouldBeAdded() :
        Exception("Handlers from namespace should be added!");

    public sealed class ContractsFromNamespaceShouldBeAdded() :
        Exception("Contracts from namespaces should be added!");
}