// namespace OfX.Attributes;
//
// [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
// public sealed class AccessForAttribute(AccessLevel accessLevel) : Attribute
// {
//     public AccessLevel AccessLevel => accessLevel;
// }
//
// // The `Restricted` will be used for feature function.
// // Hmm, I think we can use for Authentication and Authorization Purpose.
// // This sound great but currently, it will be equaled to Internal
// public enum AccessLevel
// {
//     Public,
//     Internal,
//     Restricted
// }