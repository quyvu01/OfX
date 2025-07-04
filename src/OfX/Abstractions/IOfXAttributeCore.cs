namespace OfX.Abstractions;

/// <summary>
/// OfXAttributeCore, this is the Core Of OfX Attribute, all the Attribute should have those properties!
/// <param>This is the nameof selector property, we will use this propertyName to locate the selector!
///     <name>PropertyName</name>
/// </param>
/// <param>Use this when you want to get customize data, like Expression="Email"
///     <name>Expression</name>
/// </param>
/// <param>When you want to map data with Order, ex: If the table A has only Id of table X on other service, we have to
/// get this Id first, then we will get ordered data by that Id!
///     <name>Order</name>
/// </param>
/// </summary>
public interface IOfXAttributeCore
{
    string PropertyName { get; }
    string Expression { get; set; }
}