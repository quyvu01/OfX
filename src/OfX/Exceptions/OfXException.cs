namespace OfX.Exceptions;

public static class OfXException
{
    public sealed class OfXAttributesMustBeSet()
        : Exception("You have to call the method: `AddAttributesContainNamespaces` with assemblies to scanning your `OfXAttributes`!");
    public sealed class RequestMustNotBeAddMoreThanOneTimes()
        : Exception("Request must not be add more than one times!");

    public sealed class CurrentIdTypeWasNotSupported() :
        Exception("Current Id type was not supported. Please create a join us to contribute more!");

    public sealed class PipelineIsNotReceivedPipelineBehavior(Type type) :
        Exception($"The input pipeline: {type.Name} is not matched with ReceivedPipelineBehavior. Please check again!");

    public sealed class PipelineIsNotSendPipelineBehavior(Type type) :
        Exception($"The input pipeline: {type.Name} is not matched with SendPipelineBehavior. Please check again!");

    public sealed class CannotFindHandlerForOfAttribute(Type type)
        : Exception($"Cannot find handler for OfXAttribute type: {type.Name}!");

    public sealed class StronglyTypeConfigurationImplementationMustNotBeGeneric(Type type)
        : Exception($"Strongly type configuration implementation must not be generic type: {type.Name}!");

    public sealed class StronglyTypeConfigurationMustNotBeNull()
        : Exception("Strongly type Id configuration must not be null!");

    public sealed class OfXMappingObjectsSpawnReachableTimes()
        : Exception("OfX could cannot be mapped because of objects spawn reach the deep!");

    public sealed class ModelConfigurationMustBeSet()
        : Exception(
            "You have to call the method: `AddModelConfigurationsFromNamespaceContaining<TAssembly>` to create handlers mapping!");
}