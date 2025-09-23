using OfX.Attributes;

namespace OfX.Abstractions;

/// <summary>
/// The foundational interface for all OfX components that are bound to a specific <see cref="OfXAttribute"/>.
/// </summary>
/// <typeparam name="TAttribute">
/// The type of <see cref="OfXAttribute"/> that defines the metadata, mapping rules,
/// or behavior for the implementing component.
/// </typeparam>
/// <remarks>
/// <para>
/// This is the **starting point** of the OfX framework — everything begins with an <see cref="OfXAttribute"/>.
/// </para>
/// <para>
/// Any service, handler, or component that participates in the OfX mapping or data pipeline
/// will typically implement this interface to indicate its association with a specific attribute type.
/// </para>
/// </remarks>
public interface IOfXBase<TAttribute> where TAttribute : OfXAttribute;