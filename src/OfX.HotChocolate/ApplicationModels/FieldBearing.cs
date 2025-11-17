using System.Reflection;

namespace OfX.HotChocolate.ApplicationModels;

internal sealed record FieldBearing(
    object ParentObject,
    string Expression,
    int Order,
    Type AttributeType,
    PropertyInfo TargetPropertyInfo,
    PropertyInfo RequiredPropertyInfo)
{
    public string SelectorId { get; set; }
    public Dictionary<string, string> ExpressionParameters { get; set; }

    public bool Equals(FieldBearing other)
    {
        if (other is null) return false;
        return ParentObject.Equals(other.ParentObject) && Expression == other.Expression && Order == other.Order &&
               AttributeType == other.AttributeType && TargetPropertyInfo == other.TargetPropertyInfo;
    }

    public (PropertyInfo, object) PreviousComparable => (TargetPropertyInfo, ParentObject);
    public (PropertyInfo, object) NextComparable => (RequiredPropertyInfo, ParentObject);

    public override int GetHashCode() =>
        HashCode.Combine(Expression, Order, AttributeType, TargetPropertyInfo, RequiredPropertyInfo);

    public FieldBearing Copy() => new(ParentObject, Expression, Order, AttributeType, TargetPropertyInfo,
        RequiredPropertyInfo) { SelectorId = SelectorId,  ExpressionParameters = ExpressionParameters };
}