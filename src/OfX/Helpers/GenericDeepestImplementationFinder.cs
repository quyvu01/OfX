using System.Reflection;

namespace OfX.Helpers;

public record ImplementedResult(Type ClassType, Type ImplementedClosedInterface);

public static class GenericDeepestImplementationFinder
{
    public static ImplementedResult[] GetDeepestClassesWithInterface(Assembly assembly, Type openGenericInterface,
        bool interfaceContainsGenericParameters = false)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(openGenericInterface);
        if (!openGenericInterface.IsInterface || !openGenericInterface.IsGenericTypeDefinition)
            throw new ArgumentException("Must be an open generic interface definition, e.g. IMappableRequestHandler<>");

        var allTypes = assembly
            .GetTypes()
            .Where(a => a.IsClass && !a.IsAbstract);

        // 1) Collect candidate classes and their corresponding closed interface (if any)
        var candidates = new List<(Type Class, Type ClosedInterface)>();
        foreach (var t in allTypes)
        {
            var closed = FindClosedInterfaceForType(t, openGenericInterface);
            if (closed != null) candidates.Add((t, closed));
        }

        if (candidates.Count == 0) return [];

        // Build quick lookup for candidates
        var candidateSet = new HashSet<Type>(candidates.Select(x => x.Class));

        // 2) We want to find "leaf" classes among candidates:
        //    a class is NOT a leaf if there exists another candidate that is a descendant of it.
        // We'll mark ancestors that have at least one descendant candidate.
        var hasDescendant = new Dictionary<Type, bool>(); // candidate -> bool
        foreach (var c in candidateSet) hasDescendant[c] = false;

        // Memo: for a given Type we cache nearest candidate ancestor found when walking upward
        // (null means "no candidate ancestor found up to object")
        var nearestCandidateAncestorCache = new Dictionary<Type, Type>();

        foreach (var child in candidateSet)
        {
            var visited = new List<Type>();
            Type foundAncestorCandidate = null;

            var cur = child.BaseType;
            while (cur != null && cur != typeof(object))
            {
                // If we've cached this node before, reuse result
                if (nearestCandidateAncestorCache.TryGetValue(cur, out var cached))
                {
                    foundAncestorCandidate = cached;
                    break;
                }

                visited.Add(cur);

                if (candidateSet.Contains(cur))
                {
                    foundAncestorCandidate = cur;
                    break;
                }

                cur = cur.BaseType;
            }

            // mark the ancestor (if any) as having descendant
            if (foundAncestorCandidate != null) hasDescendant[foundAncestorCandidate] = true;

            // populate cache for visited nodes
            foreach (var v in visited)
            {
                // cache the nearest candidate ancestor (may be null)
                nearestCandidateAncestorCache[v] = foundAncestorCandidate;
            }
        }

        // 3) leaf candidates are those candidate classes that do NOT have any descendant candidate
        var leafCandidates = candidates
            .Where(t => !hasDescendant[t.Class] &&
                        t.ClosedInterface.ContainsGenericParameters == interfaceContainsGenericParameters)
            .Select(t => new ImplementedResult(t.Class, t.ClosedInterface))
            .ToArray();

        return leafCandidates;
    }

    // Returns a closed interface implemented by `type` that maps to openGeneric (e.g. ITestBased<string>),
    // or null if none found.
    private static Type FindClosedInterfaceForType(Type type, Type openGeneric)
    {
        // GetInterfaces() returns all interfaces implemented by the type, including via base classes
        foreach (var iface in type.GetInterfaces())
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == openGeneric) return iface;
        }

        // As a fallback (shouldn't be necessary since GetInterfaces is comprehensive),
        // check declared base classes if any explicit closed generic base class matches the definition.
        var cur = type;
        while (cur != null && cur != typeof(object))
        {
            if (cur.IsGenericType && cur.GetGenericTypeDefinition() == openGeneric) return cur;
            cur = cur.BaseType;
        }

        return null;
    }
}