namespace OfX.Expressions.Nodes;

/// <summary>
/// Represents a single property in a root projection, which can include navigation paths and aliases.
/// </summary>
/// <param name="Path">The property path (e.g., "Name" or "Country.Name").</param>
/// <param name="Alias">The output alias name. If null, the last segment of Path is used.</param>
/// <remarks>
/// Examples:
/// <list type="bullet">
///   <item><description><c>Name</c> → Path="Name", Alias=null → output key is "Name"</description></item>
///   <item><description><c>Country.Name</c> → Path="Country.Name", Alias=null → output key is "Name"</description></item>
///   <item><description><c>Country.Name as CountryName</c> → Path="Country.Name", Alias="CountryName"</description></item>
/// </list>
/// </remarks>
public sealed record ProjectionProperty(string Path, string Alias = null)
{
    /// <summary>
    /// Gets the output key name for this property.
    /// Uses Alias if provided, otherwise the last segment of Path.
    /// </summary>
    public string OutputKey => Alias ?? Path.Split('.')[^1];

    /// <summary>
    /// Gets the path segments for navigation.
    /// </summary>
    public string[] PathSegments => Path.Split('.');

    /// <summary>
    /// Returns true if this property has a navigation path (contains '.').
    /// </summary>
    public bool HasNavigation => Path.Contains('.');
}

/// <summary>
/// Represents a root-level projection selecting specific properties from the root object: {Id, Name, Country.Name as CountryName}
/// This is different from <see cref="ProjectionNode"/> which projects from a collection source.
/// </summary>
/// <param name="Properties">The list of projection properties to select from the root object.</param>
/// <remarks>
/// Supports:
/// <list type="bullet">
///   <item><description>Simple properties: <c>{Id, Name}</c></description></item>
///   <item><description>Navigation properties: <c>{Id, Country.Name}</c></description></item>
///   <item><description>Aliased properties: <c>{Id, Country.Name as CountryName}</c></description></item>
/// </list>
/// </remarks>
public sealed record RootProjectionNode(IReadOnlyList<ProjectionProperty> Properties) : ExpressionNode
{
    public override TResult Accept<TResult, TContext>(IExpressionNodeVisitor<TResult, TContext> visitor, TContext context)
        => visitor.VisitRootProjection(this, context);
}
