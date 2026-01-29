namespace OfX.Analyzers.Nodes;

/// <summary>
/// Represents a single property in a root projection, which can include navigation paths, aliases, or computed expressions.
/// </summary>
/// <param name="Path">The property path (e.g., "Name" or "Country.Name"). Null if this is a computed expression.</param>
/// <param name="Alias">The output alias name. Required for computed expressions, optional for path-based properties.</param>
/// <param name="Expression">The computed expression (e.g., ternary, coalesce). Null for simple path-based properties.</param>
/// <remarks>
/// Examples:
/// <list type="bullet">
///   <item><description><c>Name</c> → Path="Name", Alias=null → output key is "Name"</description></item>
///   <item><description><c>Country.Name</c> → Path="Country.Name", Alias=null → output key is "Name"</description></item>
///   <item><description><c>Country.Name as CountryName</c> → Path="Country.Name", Alias="CountryName"</description></item>
///   <item><description><c>(Nickname ?? Name) as DisplayName</c> → Expression=CoalesceNode, Alias="DisplayName"</description></item>
///   <item><description><c>(Status = 'Active' ? 'Yes' : 'No') as StatusText</c> → Expression=TernaryNode, Alias="StatusText"</description></item>
/// </list>
/// </remarks>
public sealed record ProjectionProperty(string Path, string Alias = null, ExpressionNode Expression = null)
{
    /// <summary>
    /// Gets the output key name for this property.
    /// Uses Alias if provided, otherwise the last segment of Path.
    /// For computed expressions, Alias is required.
    /// </summary>
    public string OutputKey => Alias ?? Path?.Split('.')[^1]
        ?? throw new InvalidOperationException("Computed expression requires an alias");

    /// <summary>
    /// Gets the path segments for navigation. Returns empty array for computed expressions.
    /// </summary>
    public string[] PathSegments => Path?.Split('.') ?? [];

    /// <summary>
    /// Returns true if this property has a navigation path (contains '.').
    /// </summary>
    public bool HasNavigation => Path?.Contains('.') ?? false;

    /// <summary>
    /// Returns true if this is a computed expression property.
    /// </summary>
    public bool IsComputed => Expression != null;

    /// <summary>
    /// Creates a path-based projection property.
    /// </summary>
    public static ProjectionProperty FromPath(string path, string alias = null)
        => new(path, alias);

    /// <summary>
    /// Creates a computed expression projection property.
    /// </summary>
    public static ProjectionProperty FromExpression(ExpressionNode expression, string alias)
        => new(null, alias, expression);
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
