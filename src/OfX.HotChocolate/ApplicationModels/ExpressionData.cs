using System.Reflection;

namespace OfX.HotChocolate.ApplicationModels;

internal sealed record ExpressionData(
    object ParentObject,
    string Expression,
    int Order,
    Type AttributeType,
    PropertyInfo TargetPropertyInfo,
    PropertyInfo RequiredPropertyInfo)
{
    public string SelectorId { get; set; }

    public bool Equals(ExpressionData other)
    {
        if (other is null) return false;
        return ParentObject.Equals(other.ParentObject) && Expression == other.Expression && Order == other.Order &&
               AttributeType == other.AttributeType &&
               TargetPropertyInfo == other.TargetPropertyInfo;
    }

    public (PropertyInfo, object) PreviousObject => (TargetPropertyInfo, ParentObject);
    public (PropertyInfo, object) NextObject => (RequiredPropertyInfo, ParentObject);

    public override int GetHashCode() =>
        HashCode.Combine(Expression, Order, AttributeType, TargetPropertyInfo, RequiredPropertyInfo);
}