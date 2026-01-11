using System.Linq.Expressions;
using System.Reflection;

namespace OfX.Extensions;

/// <summary>
/// Provides extension methods for working with LINQ expression trees.
/// </summary>
/// <remarks>
/// These utilities are used internally by OfX for extracting member names and property information
/// from lambda expressions in a type-safe manner.
/// </remarks>
public static class ExpressionExtensions
{
    /// <summary>
    /// Gets the name of the member specified
    /// </summary>
    /// <typeparam name="T">The type referenced</typeparam>
    /// <typeparam name="TMember">The type of the member referenced</typeparam>
    /// <param name="expression">The expression referencing the member</param>
    /// <returns>The name of the member referenced by the expression</returns>
    public static string GetMemberName<T, TMember>(this Expression<Func<T, TMember>> expression) =>
        expression.GetMemberExpression().Member.Name;

    /// <summary>
    /// Gets the name of the member specified
    /// </summary>
    /// <typeparam name="T">The type referenced</typeparam>
    /// <param name="expression">The expression referencing the member</param>
    /// <returns>The name of the member referenced by the expression</returns>
    public static string GetMemberName<T>(this Expression<Action<T>> expression) =>
        expression.GetMemberExpression().Member.Name;

    /// <summary>
    /// Gets the name of the member specified in the expression.
    /// </summary>
    /// <typeparam name="T">The return type of the expression.</typeparam>
    /// <param name="expression">The expression referencing the member.</param>
    /// <returns>The name of the member referenced by the expression.</returns>
    public static string GetMemberName<T>(this Expression<Func<T>> expression) =>
        expression.GetMemberExpression().Member.Name;

    /// <summary>
    /// Gets the PropertyInfo for the property specified in the expression.
    /// </summary>
    /// <typeparam name="T">The type containing the property.</typeparam>
    /// <typeparam name="TMember">The type of the property.</typeparam>
    /// <param name="expression">The expression referencing the property.</param>
    /// <returns>The PropertyInfo for the referenced property.</returns>
    public static PropertyInfo GetPropertyInfo<T, TMember>(this Expression<Func<T, TMember>> expression) =>
        expression.GetMemberExpression().Member as PropertyInfo;

    /// <summary>
    /// Gets the PropertyInfo for the property specified in the expression.
    /// </summary>
    /// <typeparam name="T">The return type of the expression.</typeparam>
    /// <param name="expression">The expression referencing the property.</param>
    /// <returns>The PropertyInfo for the referenced property.</returns>
    public static PropertyInfo GetPropertyInfo<T>(this Expression<Func<T>> expression) =>
        expression.GetMemberExpression().Member as PropertyInfo;

    /// <summary>
    /// Gets the MemberInfo for the member specified in the action expression.
    /// </summary>
    /// <typeparam name="T">The type on which the action operates.</typeparam>
    /// <param name="expression">The expression referencing the member.</param>
    /// <returns>The MemberInfo for the referenced member.</returns>
    public static MemberInfo GetMemberInfo<T>(this Expression<Action<T>> expression) =>
        expression.GetMemberExpression().Member;

    /// <summary>
    /// Extracts the MemberExpression from the specified lambda expression.
    /// </summary>
    /// <typeparam name="T">The type containing the member.</typeparam>
    /// <typeparam name="TMember">The type of the member.</typeparam>
    /// <param name="expression">The lambda expression.</param>
    /// <returns>The MemberExpression from the expression body.</returns>
    public static MemberExpression GetMemberExpression<T, TMember>(this Expression<Func<T, TMember>> expression) =>
        expression == null ? throw new ArgumentNullException(nameof(expression)) : GetMemberExpression(expression.Body);

    /// <summary>
    /// Extracts the MemberExpression from the specified action expression.
    /// </summary>
    /// <typeparam name="T">The type on which the action operates.</typeparam>
    /// <param name="expression">The action expression.</param>
    /// <returns>The MemberExpression from the expression body.</returns>
    public static MemberExpression GetMemberExpression<T>(this Expression<Action<T>> expression) =>
        expression == null
            ? throw new ArgumentNullException(nameof(expression))
            : GetMemberExpression(expression.Body);

    /// <summary>
    /// Extracts the MemberExpression from the specified parameterless expression.
    /// </summary>
    /// <typeparam name="T">The return type of the expression.</typeparam>
    /// <param name="expression">The expression.</param>
    /// <returns>The MemberExpression from the expression body.</returns>
    public static MemberExpression GetMemberExpression<T>(this Expression<Func<T>> expression) =>
        expression == null ? throw new ArgumentNullException(nameof(expression)) : GetMemberExpression(expression.Body);

    /// <summary>
    /// Extracts the MemberExpression from the specified two-parameter action expression.
    /// </summary>
    /// <typeparam name="T1">The first parameter type.</typeparam>
    /// <typeparam name="T2">The second parameter type.</typeparam>
    /// <param name="expression">The action expression.</param>
    /// <returns>The MemberExpression from the expression body.</returns>
    public static MemberExpression GetMemberExpression<T1, T2>(this Expression<Action<T1, T2>> expression) =>
        expression == null ? throw new ArgumentNullException(nameof(expression)) : GetMemberExpression(expression.Body);

    static MemberExpression GetMemberExpression(Expression body)
    {
        ArgumentNullException.ThrowIfNull(body);

        MemberExpression memberExpression = null;
        switch (body.NodeType)
        {
            case ExpressionType.Convert:
            {
                var unaryExpression = (UnaryExpression)body;
                memberExpression = unaryExpression.Operand as MemberExpression;
                break;
            }
            case ExpressionType.MemberAccess:
                memberExpression = body as MemberExpression;
                break;
        }

        return memberExpression ?? throw new ArgumentException("Expression is not a member access");
    }
}