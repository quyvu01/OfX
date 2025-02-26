namespace OfX.MongoDb.Responses;

internal sealed class OfXDataMongoResponse
{
    public string Id { get; set; }
    public List<OfXValueMongoResponse> OfXValues { get; set; }
}