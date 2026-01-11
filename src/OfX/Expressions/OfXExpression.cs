using System.Linq.Expressions;
using OfX.Accessors.TypeAccessors;
using OfX.Cached;
using OfX.Expressions.Building;
using OfX.Expressions.Nodes;
using OfX.Expressions.Parsing;

namespace OfX.Expressions;

/// <summary>
/// Main entry point for parsing and building OfX expressions.
/// </summary>
/// <remarks>
/// <para>
/// Supported expression syntax:
/// </para>
/// <list type="bullet">
///   <item><description>Simple property: <c>Name</c>, <c>Email</c></description></item>
///   <item><description>Navigation: <c>Country.Name</c>, <c>User.Address.City</c></description></item>
///   <item><description>Null-safe navigation: <c>Country?.Name</c></description></item>
///   <item><description>Function (count/length): <c>Name:count</c>, <c>Orders:count</c></description></item>
///   <item><description>Aggregation: <c>Orders:sum(Total)</c>, <c>Items:avg(Price)</c></description></item>
///   <item><description>Filter: <c>Orders(Status = 'Completed')</c></description></item>
///   <item><description>Complex filter: <c>Provinces(Name:count > 3 &amp;&amp; Active = true)</c></description></item>
///   <item><description>Indexer (single): <c>Orders[0 asc CreatedAt]</c></description></item>
///   <item><description>Indexer (range): <c>Orders[0 10 desc CreatedAt]</c></description></item>
///   <item><description>Projection: <c>Orders.{Id, Total, Status}</c></description></item>
///   <item><description>Combined: <c>Country.Provinces(Name:count > 3)[0 10 asc Name].{Id, Name}</c></description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Simple usage
/// var expr = OfXExpression.Parse&lt;User&gt;("Country.Name");
/// var lambda = expr.ToLambda&lt;string&gt;();
///
/// // Complex query
/// var expr = OfXExpression.Parse&lt;User&gt;("Orders(Status = 'Completed')[0 10 desc Total].{Id, Total}");
///
/// // With filter
/// var expr = OfXExpression.Parse&lt;Country&gt;("Provinces(Name:count > 3)");
/// </code>
/// </example>
public sealed class OfXExpression<TModel>
{
    private readonly ExpressionNode _rootNode;
    private readonly Func<Type, ITypeAccessor> _typeAccessorProvider;

    private OfXExpression(ExpressionNode rootNode, Func<Type, ITypeAccessor> typeAccessorProvider = null)
    {
        _rootNode = rootNode;
        _typeAccessorProvider = typeAccessorProvider ?? OfXTypeCache.GetTypeAccessor;
    }

    /// <summary>
    /// Gets the AST root node.
    /// </summary>
    public ExpressionNode RootNode => _rootNode;

    /// <summary>
    /// Parses an expression string and returns an OfXExpression instance.
    /// </summary>
    /// <param name="expression">The expression string to parse.</param>
    /// <param name="typeAccessorProvider">Optional custom type accessor provider for ExposedName resolution.</param>
    /// <returns>A parsed OfXExpression ready for building LINQ expressions.</returns>
    public static OfXExpression<TModel> Parse(string expression, Func<Type, ITypeAccessor> typeAccessorProvider = null)
    {
        var node = ExpressionParser.Parse(expression);
        return new OfXExpression<TModel>(node, typeAccessorProvider);
    }

    /// <summary>
    /// Builds the LINQ expression and returns the result.
    /// </summary>
    /// <returns>The build result containing the expression and result type.</returns>
    public ExpressionBuildResult Build()
    {
        return LinqExpressionBuilder.Build<TModel>(_rootNode, _typeAccessorProvider);
    }

    /// <summary>
    /// Builds a typed lambda expression.
    /// </summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <returns>A lambda expression of the form (TModel x) => TResult.</returns>
    public Expression<Func<TModel, TResult>> ToLambda<TResult>()
    {
        var result = Build();
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var context = new ExpressionBuildContext(typeof(TModel), parameter, parameter, _typeAccessorProvider);

        var builder = new LinqExpressionBuilder();
        var buildResult = _rootNode.Accept(builder, context);

        // Convert result if needed
        Expression body = buildResult.Expression;
        if (body.Type != typeof(TResult))
        {
            body = Expression.Convert(body, typeof(TResult));
        }

        return Expression.Lambda<Func<TModel, TResult>>(body, parameter);
    }

    /// <summary>
    /// Builds a lambda expression with automatic result type inference.
    /// </summary>
    /// <returns>A LambdaExpression representing the parsed expression.</returns>
    public LambdaExpression ToLambda()
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var context = new ExpressionBuildContext(typeof(TModel), parameter, parameter, _typeAccessorProvider);

        var builder = new LinqExpressionBuilder();
        var buildResult = _rootNode.Accept(builder, context);

        return Expression.Lambda(buildResult.Expression, parameter);
    }
}

/// <summary>
/// Static helper class for OfX expression operations.
/// </summary>
public static class OfXExpression
{
    /// <summary>
    /// Parses an expression string for the specified model type.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="expression">The expression string.</param>
    /// <returns>A parsed OfXExpression.</returns>
    public static OfXExpression<TModel> Parse<TModel>(string expression) =>
        OfXExpression<TModel>.Parse(expression);

    /// <summary>
    /// Parses and builds a lambda expression in one step.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="expression">The expression string.</param>
    /// <returns>A lambda expression.</returns>
    public static Expression<Func<TModel, TResult>> BuildLambda<TModel, TResult>(string expression) =>
        OfXExpression<TModel>.Parse(expression).ToLambda<TResult>();

    /// <summary>
    /// Parses and builds a lambda expression with inferred result type.
    /// </summary>
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="expression">The expression string.</param>
    /// <returns>A lambda expression.</returns>
    public static LambdaExpression BuildLambda<TModel>(string expression) =>
        OfXExpression<TModel>.Parse(expression).ToLambda();
}
