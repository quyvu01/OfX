using System.Reflection;
using System.Runtime.CompilerServices;
using OfX.Helpers;

namespace OfX.Extensions;

public static class TypeExtensions
{
    /// <param name="type"></param>
    extension(Type type)
    {
        public IEnumerable<PropertyInfo> GetAllProperties() => type.GetTypeInfo().GetAllProperties();
        internal bool IsPrimitiveType() => GeneralHelpers.IsPrimitiveType(type);
    }

    public static IEnumerable<PropertyInfo> GetAllProperties(this TypeInfo typeInfo)
    {
        if (typeInfo.BaseType != null)
        {
            foreach (var prop in typeInfo.BaseType.GetAllProperties())
                yield return prop;
        }

        var specialGetPropertyNames = typeInfo.DeclaredMethods
            .Where(x => x.IsSpecialName && x.Name.StartsWith("get_") && !x.IsStatic)
            .Select(x => x.Name["get_".Length..]).Distinct();

        var properties = typeInfo.DeclaredProperties
            .Where(x => specialGetPropertyNames.Contains(x.Name))
            .ToList();

        if (typeInfo.IsInterface)
        {
            var sourceProperties = properties
                .Concat(typeInfo.ImplementedInterfaces.SelectMany(x => x.GetProperties(BindingFlags.DeclaredOnly |
                    BindingFlags.Instance |
                    BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic)));

            foreach (var prop in sourceProperties)
                yield return prop;

            yield break;
        }

        foreach (var info in properties)
            yield return info;
    }

    /// <param name="type">The type to check</param>
    extension(Type type)
    {
        public IEnumerable<Type> GetAllInterfaces()
        {
            if (type.IsInterface)
                yield return type;

            foreach (var interfaceType in type.GetInterfaces())
                yield return interfaceType;
        }

        public IEnumerable<PropertyInfo> GetAllStaticProperties()
        {
            var info = type.GetTypeInfo();

            if (type.BaseType != null)
            {
                foreach (var prop in type.BaseType.GetAllStaticProperties())
                    yield return prop;
            }

            var props = info.DeclaredMethods
                .Where(x => x.IsSpecialName && x.Name.StartsWith("get_") && x.IsStatic)
                .Select(x => info.GetDeclaredProperty(x.Name.Substring("get_".Length)));

            foreach (var propertyInfo in props)
                if (propertyInfo != null)
                    yield return propertyInfo;
        }

        public IEnumerable<PropertyInfo> GetStaticProperties()
        {
            var info = type.GetTypeInfo();

            return info.DeclaredMethods
                .Where(x => x.IsSpecialName && x.Name.StartsWith("get_") && x.IsStatic)
                .Select(x => info.GetDeclaredProperty(x.Name["get_".Length..]));
        }

        /// <summary>
        /// Determines if a type is neither abstract nor an interface and can be constructed.
        /// </summary>
        /// <returns>True if the type can be constructed, otherwise false.</returns>
        public bool IsConcrete() => type is { IsAbstract: false, IsInterface: false };

        public bool IsInterfaceOrConcreteClass()
        {
            if (type.IsInterface) return true;

            return type is { IsClass: true, IsAbstract: false };
        }

        /// <summary>
        /// Determines if a type can be constructed, and if it can, additionally determines
        /// if the type can be assigned to the specified type.
        /// </summary>
        /// <param name="assignableType">The type to which the subject type should be checked against</param>
        /// <returns>
        /// True if the type is concrete and can be assigned to the assignableType, otherwise false.
        /// </returns>
        public bool IsConcreteAndAssignableTo(Type assignableType)
        {
            return IsConcrete(type) && assignableType.IsAssignableFrom(type);
        }

        /// <summary>
        /// Determines if a type can be constructed, and if it can, additionally determines
        /// if the type can be assigned to the specified type.
        /// </summary>
        /// <typeparam name="T">The type to which the subject type should be checked against</typeparam>
        /// <returns>
        /// True if the type is concrete and can be assigned to the assignableType, otherwise false.
        /// </returns>
        public bool IsConcreteAndAssignableTo<T>()
        {
            return IsConcrete(type) && typeof(T).IsAssignableFrom(type);
        }

        /// <summary>
        /// Determines if the type is a nullable type
        /// </summary>
        /// <param name="underlyingType">The underlying type of the nullable</param>
        /// <returns>True if the type can be null</returns>
        public bool IsNullable(out Type underlyingType)
        {
            var isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

            underlyingType = isNullable ? Nullable.GetUnderlyingType(type) : null;
            return isNullable;
        }

        /// <summary>
        /// Determines if the type is an open generic with at least one unspecified generic argument
        /// </summary>
        /// <returns>True if the type is an open generic</returns>
        public bool IsOpenGeneric()
        {
            return type.IsGenericTypeDefinition || type.ContainsGenericParameters;
        }

        /// <summary>
        /// Determines if a type can be null
        /// </summary>
        /// <returns>True if the type can be null</returns>
        public bool CanBeNull() =>
            !type.IsValueType
            || type == typeof(string)
            || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
    }

    /// <param name="provider">An attribute provider, which can be a MethodInfo, PropertyInfo, Type, etc.</param>
    extension(ICustomAttributeProvider provider)
    {
        /// <summary>
        /// Returns the first attribute of the specified type for the object specified
        /// </summary>
        /// <typeparam name="T">The type of attribute</typeparam>
        /// <returns>The attribute instance if found, or null</returns>
        public IEnumerable<T> GetAttribute<T>()
            where T : Attribute
        {
            return provider.GetCustomAttributes(typeof(T), true)
                .Cast<T>();
        }

        /// <summary>
        /// Determines if the target has the specified attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasAttribute<T>() where T : Attribute => provider.GetAttribute<T>().Any();
    }

    /// <param name="type"></param>
    extension(Type type)
    {
        /// <summary>
        /// Returns true if the type is an anonymous type
        /// </summary>
        /// <returns></returns>
        public bool IsAnonymousType()
        {
            return type.FullName != null && type.HasAttribute<CompilerGeneratedAttribute>() &&
                   type.FullName.Contains("AnonymousType");
        }

        /// <summary>
        /// Returns true if the type is an FSharp type (maybe?)
        /// </summary>
        /// <returns></returns>
        public bool IsFSharpType()
        {
            var attributes = type.GetCustomAttributes();

            return attributes.Any(attribute =>
                attribute.GetType().FullName == "Microsoft.FSharp.Core.CompilationMappingAttribute");
        }

        /// <summary>
        /// Returns true if the type is contained within the namespace
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <returns></returns>
        public bool IsInNamespace(string nameSpace)
        {
            var subNameSpace = nameSpace + ".";
            return type.Namespace != null &&
                   (type.Namespace.Equals(nameSpace) || type.Namespace.StartsWith(subNameSpace));
        }

        /// <summary>
        /// True if the type is a value type, or an object type that is treated as a value by MassTransit
        /// </summary>
        /// <returns></returns>
        public bool IsValueTypeOrObject() =>
            type.IsValueType
            || type == typeof(string)
            || type == typeof(Uri)
            || type == typeof(Version)
            || typeof(Exception).IsAssignableFrom(type);
    }
}