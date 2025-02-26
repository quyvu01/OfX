using MongoDB.Bson.Serialization.Attributes;

namespace OfX.Tests.Models;

public sealed class MemberAddress
{
    [BsonId] public Guid Id { get; set; }
    public string Name { get; set; }
}