using MongoDB.Bson.Serialization.Attributes;

namespace OfX.Tests.Models;

public sealed class OtherMemberAddressMongo
{
    [BsonId] public Guid Id { get; set; }
    public string OtherName { get; set; }
}