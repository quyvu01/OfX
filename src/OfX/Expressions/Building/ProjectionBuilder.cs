using System.Linq.Expressions;
using OfX.Accessors.TypeAccessors;
using OfX.Cached;
using OfX.Expressions.Parsing;

namespace OfX.Expressions.Building;

/// <summary>
/// Builds a single projection expression from multiple OfX expression strings.
/// </summary>
/// <remarks>
/// <para>
/// This builder combines multiple expression strings into a single database query,
/// projecting all requested values as object[] in one round-trip.
/// </para>
/// <para>
/// Example input: ["Name", "Email", "Country.Name", "Orders:count"]
/// </para>
/// <para>
/// Output projection: x => new object[] {
///     x.Id,                    // [0] = Id (always first)
///     x.Name,                  // [1] = Expression null (default)
///     x.Email,                 // [2] = Expression "Email"
///     x.Country.Name,          // [3] = Expression "Country.Name"
///     x.Orders.Count()         // [4] = Expression "Orders:count"
/// }
/// </para>
/// <para>
/// The result can then be transformed to OfXDataResponse in memory.
/// </para>
/// </remarks>
public sealed class ProjectionBuilder<TModel>(
    string idProperty,
    string defaultProperty = null,
    Func<Type, ITypeAccessor> typeAccessorProvider = null)
    where TModel : class
{
    private readonly ParameterExpression _parameter = Expression.Parameter(typeof(TModel), "x");

    private readonly Func<Type, ITypeAccessor> _typeAccessorProvider =
        typeAccessorProvider ?? OfXTypeCache.GetTypeAccessor;

    /// <summary>
    /// Builds a projection expression that returns object[].
    /// Index 0 is always the Id, followed by each expression value.
    /// </summary>
    /// <param name="expressions">The expression strings to project.</param>
    /// <returns>A lambda expression projecting TModel to object[].</returns>
    public Expression<Func<TModel, object[]>> Build(IEnumerable<string> expressions)
    {
        var expressionList = expressions.ToList();
        var projections = new List<Expression>();

        // [0] = Id (always first)
        var idExpr = BuildIdExpression();
        projections.Add(Expression.Convert(idExpr, typeof(object)));

        // [1..n] = Expression values
        foreach (var expr in expressionList)
        {
            var isDefaultProperty = expr == null;
            var actualExpr = expr ?? defaultProperty;
            var valueExpr = BuildExpressionValue(actualExpr, isDefaultProperty);
            projections.Add(Expression.Convert(valueExpr, typeof(object)));
        }

        var arrayInit = Expression.NewArrayInit(typeof(object), projections);
        return Expression.Lambda<Func<TModel, object[]>>(arrayInit, _parameter);
    }

    /// <summary>
    /// Builds a projection expression with explicit result tracking.
    /// Returns tuple of (expression, index mapping).
    /// </summary>
    /// <param name="expressions">The expression strings to project.</param>
    /// <returns>The projection expression and metadata about each index.</returns>
    public ProjectionResult<TModel> BuildWithMetadata(IEnumerable<string> expressions)
    {
        var expressionList = expressions.ToList();
        var projections = new List<Expression>();
        var metadata = new List<ProjectionMetadata>();

        // [0] = Id (always first)
        var idExpr = BuildIdExpression();
        projections.Add(Expression.Convert(idExpr, typeof(object)));
        metadata.Add(new ProjectionMetadata(0, null, true));

        // [1..n] = Expression values
        for (var i = 0; i < expressionList.Count; i++)
        {
            var expr = expressionList[i];
            var isDefaultProperty = expr == null;
            var actualExpr = expr ?? defaultProperty;

            try
            {
                var valueExpr = BuildExpressionValue(actualExpr, isDefaultProperty);
                projections.Add(Expression.Convert(valueExpr, typeof(object)));
                metadata.Add(new ProjectionMetadata(i + 1, expr, false));
            }
            catch (Exception ex)
            {
                // Add null placeholder for failed expressions
                projections.Add(Expression.Constant(null, typeof(object)));
                metadata.Add(new ProjectionMetadata(i + 1, expr, false, ex.Message));
            }
        }

        var arrayInit = Expression.NewArrayInit(typeof(object), projections);
        var lambda = Expression.Lambda<Func<TModel, object[]>>(arrayInit, _parameter);

        return new ProjectionResult<TModel>(lambda, metadata);
    }

    private Expression BuildIdExpression()
    {
        var typeAccessor = _typeAccessorProvider(typeof(TModel));
        // Use GetPropertyInfoDirect to bypass ExposedName for Id property
        var idPropertyInfo = typeAccessor.GetPropertyInfoDirect(idProperty)
                             ?? throw new InvalidOperationException(
                                 $"Id property '{idProperty}' not found on type '{typeof(TModel).Name}'");

        return Expression.Property(_parameter, idPropertyInfo);
    }

    /// <summary>
    /// Builds expression for defaultProperty, bypassing ExposedName.
    /// </summary>
    private Expression BuildDefaultPropertyExpression()
    {
        if (string.IsNullOrEmpty(defaultProperty))
            return Expression.Constant(null, typeof(object));

        var typeAccessor = _typeAccessorProvider(typeof(TModel));
        // Use GetPropertyInfoDirect to bypass ExposedName for defaultProperty
        var propertyInfo = typeAccessor.GetPropertyInfoDirect(defaultProperty)
                           ?? throw new InvalidOperationException(
                               $"Default property '{defaultProperty}' not found on type '{typeof(TModel).Name}'");

        return Expression.Property(_parameter, propertyInfo);
    }

    private Expression BuildExpressionValue(string expression, bool isDefaultProperty = false)
    {
        if (string.IsNullOrEmpty(expression))
            return Expression.Constant(null, typeof(object));

        // If this is the default property (expression was null, using _defaultProperty),
        // bypass ExposedName and access property directly
        if (isDefaultProperty)
            return BuildDefaultPropertyExpression();

        // Parse the expression using our new parser
        var node = ExpressionParser.Parse(expression);

        // Build context
        var context = new ExpressionBuildContext(
            typeof(TModel),
            _parameter,
            _parameter,
            _typeAccessorProvider);

        // Build the LINQ expression
        var builder = new LinqExpressionBuilder();
        var result = node.Accept(builder, context);

        return result.Expression;
    }
}

/// <summary>
/// Result of building a projection with metadata.
/// </summary>
/// <typeparam name="TModel">The model type.</typeparam>
/// <param name="Projection">The projection lambda expression.</param>
/// <param name="Metadata">Metadata about each projected value.</param>
public sealed record ProjectionResult<TModel>(
    Expression<Func<TModel, object[]>> Projection,
    IReadOnlyList<ProjectionMetadata> Metadata) where TModel : class;

/// <summary>
/// Metadata about a single projected value.
/// </summary>
/// <param name="Index">The index in the result array.</param>
/// <param name="Expression">The original expression string (null for default/Id).</param>
/// <param name="IsId">Whether this is the Id field.</param>
/// <param name="Error">Error message if expression building failed.</param>
public sealed record ProjectionMetadata(
    int Index,
    string Expression,
    bool IsId,
    string Error = null)
{
    public bool HasError => Error != null;
}

/// <summary>
/// Static factory methods for ProjectionBuilder.
/// </summary>
public static class ProjectionBuilder
{
    /// <summary>
    /// Creates a new ProjectionBuilder for the specified model type.
    /// </summary>
    public static ProjectionBuilder<TModel> Create<TModel>(
        string idProperty,
        string defaultProperty = null,
        Func<Type, ITypeAccessor> typeAccessorProvider = null)
        where TModel : class =>
        new(idProperty, defaultProperty, typeAccessorProvider);
}