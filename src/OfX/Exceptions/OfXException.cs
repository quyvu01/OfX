using OfX.Statics;

namespace OfX.Exceptions;

public static class OfXException
{
    public sealed class OfXAttributesMustBeSet()
        : Exception(
            "You have to call the method: `AddAttributesContainNamespaces` with assemblies to scanning your `OfXAttributes`!");

    public sealed class CurrentIdTypeWasNotSupported() :
        Exception("Current Id type was not supported. Create the IdConverter!");

    public sealed class TypeIsNotReceivedPipelineBehavior(Type type) :
        Exception($"The input type: {type.Name} is not matched with ReceivedPipelineBehavior!");

    public sealed class TypeIsNotSendPipelineBehavior(Type type) :
        Exception($"The input type: {type.Name} is not matched with SendPipelineBehavior!");

    public sealed class CannotFindHandlerForOfAttribute(Type type)
        : Exception($"Cannot find handler for OfXAttribute type: {type.Name}!");

    public sealed class StronglyTypeConfigurationImplementationMustNotBeGeneric(Type type)
        : Exception($"Strongly type configuration implementation must not be generic type: {type.Name}!");

    public sealed class StronglyTypeConfigurationMustNotBeNull()
        : Exception("Strongly type Id configuration must not be null!");

    public sealed class OfXMappingObjectsSpawnReachableTimes()
        : Exception(
            $"OfX mapping engine has been reach out the current max deep: {OfXStatics.MaxObjectSpawnTimes}! Set the MaxObjectSpawnTimes on method `SetMaxObjectSpawnTimes`");

    public sealed class ModelConfigurationMustBeSet()
        : Exception(
            "You have to call the method: `AddModelConfigurationsFromNamespaceContaining<TAssembly>` to create handlers mapping!");

    public sealed class ReceivedException(string message)
        : Exception($"{AppDomain.CurrentDomain.FriendlyName} : {message}");
}