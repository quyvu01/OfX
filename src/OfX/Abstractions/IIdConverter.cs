namespace OfX.Abstractions;

/// <summary>
/// Defines a contract for converting selector IDs into the correct ID type at runtime.
/// </summary>
/// <remarks>
/// This interface is typically used internally by the OfX to ensure that
/// incoming selector IDs (received as <see cref="string"/> values) are converted
/// into their proper type (e.g., <see cref="Guid"/>, <see cref="int"/>, or a strongly-typed ID).
/// </remarks>
public interface IIdConverter
{
    /// <summary>
    /// Converts the given list of selector IDs into the appropriate ID type for the target model.
    /// </summary>
    /// <param name="selectorIds">A list of selector IDs as strings.</param>
    /// <returns>
    /// An <see cref="object"/> representing the converted ID or collection of IDs,
    /// ready to be used in queries or lookups.
    /// </returns>
    object ConvertIds(string[] selectorIds);
}

/// <summary>
/// A generic version of <see cref="IIdConverter"/> that provides type safety
/// for the target ID type.
/// </summary>
/// <typeparam name="TId">
/// The target type to which the selector IDs should be converted
/// (e.g., <see cref="Guid"/>, <see cref="int"/>, or a custom strongly-typed ID).
/// </typeparam>
/// <remarks>
/// Use this interface to implement type-specific ID conversion logic,
/// ensuring that the OfX framework can work with strongly-typed identifiers.
/// </remarks>
public interface IIdConverter<out TId> : IIdConverter;