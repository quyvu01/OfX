using Kernel.ModelIds;

namespace Service1.Contract.Responses;

public class ProvinceResponse
{
    public StronglyTypedId<Guid> Id { get; set; }
    public string Name { get; set; }
}