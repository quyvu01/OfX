using OfX.Abstractions;
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
        Exception($"{type.Name} must implement {typeof(IReceivedPipelineBehavior<>).FullName}!");

    public sealed class TypeIsNotSendPipelineBehavior(Type type) :
        Exception($"{type.Name} must implement {typeof(ISendPipelineBehavior<>).FullName}!");

    public sealed class TypeIsNotCustomExpressionPipelineBehavior(Type type) :
        Exception($"{type.Name} must implement {typeof(ICustomExpressionBehavior<>).FullName}!");

    public sealed class CannotFindHandlerForOfAttribute(Type type)
        : Exception($"Cannot find handler for OfXAttribute type: {type.Name}!");

    public sealed class StronglyTypeConfigurationImplementationMustNotBeGeneric(Type type)
        : Exception($"Strongly type configuration implementation must not be generic type: {type.Name}!");

    public sealed class StronglyTypeConfigurationMustNotBeNull()
        : Exception("Strongly type Id configuration must not be null!");

    public sealed class AttributeHasBeenConfiguredForModel(Type modelType, Type attributeType)
        : Exception(
            $"OfXAttribute: {attributeType.FullName} has been configured for {modelType.FullName} at least twice!");

    public sealed class OfXMappingObjectsSpawnReachableTimes()
        : Exception(
            $"OfX mapping engine has been reach out the current max deep: {OfXStatics.MaxObjectSpawnTimes}! Set the MaxObjectSpawnTimes on method `SetMaxObjectSpawnTimes`");

    public sealed class ModelConfigurationMustBeSet()
        : Exception(
            "You have to call the method: `AddModelConfigurationsFromNamespaceContaining<TAssembly>` to create handlers mapping!");

    public sealed class ReceivedException(string message)
        : Exception($"{AppDomain.CurrentDomain.FriendlyName} : {message}");

    public sealed class CollectionFormatNotCorrected(string collectionPropertyName) : Exception($"""
         Collection data [{collectionPropertyName}] must be defined as 
         [OrderDirection OrderedProperty] or 
         [Offset Limit OrderDirection OrderedProperty] or 
         [0 OrderDirection OrderedProperty](First item) or 
         [-1 OrderDirection OrderedProperty](Last item)
         """);

    public sealed class CollectionIndexIncorrect(string indexAsString)
        : Exception($"First parameter [{indexAsString}] must be 0(First item) or -1(Last item).");

    public sealed class CollectionOrderDirectionIncorrect(string orderDirection)
        : Exception($"Second parameter [{orderDirection}] must be an ordered direction `ASC|DESC`");

    public sealed class NavigatorIncorrect(string navigator, string parentType)
        : Exception($"Object: '{parentType}' does not include navigator: {navigator}");

    public sealed class InvalidParameter(string expression)
        : Exception(
            $"Expression: '{expression}' is must be look like this: '${{parameter|default}}'(i.e: '${{index|0}}')");

    public sealed class AmbiguousHandlers(Type interfaceType) :
        Exception($"Ambiguous handlers for interface '{interfaceType.FullName}'.");

    public sealed class NoHandlerForAttribute(Type type) : Exception($"There is no handler for '{type.FullName}'!");
}