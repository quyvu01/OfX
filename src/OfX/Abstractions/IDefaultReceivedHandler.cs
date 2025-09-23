using OfX.Attributes;

namespace OfX.Abstractions;

/// <summary>
/// This is the default received handler. 
/// Create a class that implements <see cref="IDefaultReceivedHandler{T}"/> where <c>T</c> : <c>OfXAttribute</c>.
/// Then, register the attributes (also known as default attributes — those not explicitly configured for a model)
/// by using the method <c>AddDefaultReceiversFromNamespaceContaining&lt;TAssemblyMarker&gt;</c>.
/// </summary>
public interface IDefaultReceivedHandler<TAttribute> where TAttribute : OfXAttribute;