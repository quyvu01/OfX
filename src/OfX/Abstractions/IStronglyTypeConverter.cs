namespace OfX.Abstractions;

/// <summary>
/// Defines a non-generic marker interface for strongly-typed ID converters.
/// </summary>
/// <remarks>
/// This interface is used by the OfX framework to identify and register all 
/// <see cref="IStronglyTypeConverter{TId}"/> implementations dynamically at runtime.
/// 
/// Typically, you will not implement this interface directly — use the generic
/// <see cref="IStronglyTypeConverter{TId}"/> instead.
/// </remarks>
public interface IStronglyTypeConverter;

/// <summary>
/// Defines a converter that transforms string selector IDs into a strongly-typed ID representation.
/// </summary>
/// <typeparam name="TId">
/// The strongly-typed identifier type (e.g. <c>UserId</c>, <c>OrderId</c>, or a custom StronglyTypedId as a reference type).
/// </typeparam>
/// <remarks>
/// <para>
/// This interface is used in conjunction with <see cref="IIdConverter"/> to support 
/// **strongly-typed identifiers** in OfX.  
/// </para>
/// <para>
/// When a request contains string-based <c>selectorIds</c>, the framework uses 
/// registered <see cref="IStronglyTypeConverter{TId}"/> implementations to:
/// </para>
/// <list type="number">
/// <item>Check if a given string can be converted to <typeparamref name="TId"/> via <see cref="CanConvert"/>.</item>
/// <item>Convert the string into the strongly-typed identifier via <see cref="Convert"/>.</item>
/// </list>
/// </remarks>
public interface IStronglyTypeConverter<out TId> : IStronglyTypeConverter
{
    /// <summary>
    /// Converts the input string into a strongly-typed identifier of type <typeparamref name="TId"/>.
    /// </summary>
    /// <param name="input">The string value to convert.</param>
    /// <returns>The converted <typeparamref name="TId"/> value.</returns>
    /// <exception cref="FormatException">
    /// Thrown if the input cannot be converted to <typeparamref name="TId"/>.
    /// </exception>
    TId Convert(string input);

    /// <summary>
    /// Determines whether the given input string can be converted to <typeparamref name="TId"/>.
    /// </summary>
    /// <param name="input">The string value to check.</param>
    /// <returns>
    /// <see langword="true"/> if the input can be converted to <typeparamref name="TId"/>, 
    /// otherwise <see langword="false"/>.
    /// </returns>
    bool CanConvert(string input);
}