namespace OfX.Responses;

public sealed class OfXDataResponse
{
    public string Id { get; set; }
    public OfXValueResponse[] OfXValues { get; set; }
}