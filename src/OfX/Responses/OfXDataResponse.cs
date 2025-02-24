namespace OfX.Responses;

public sealed class OfXDataResponse
{
    public string Id { get; set; }
    public List<OfXValueResponse> OfXValues { get; set; }
}