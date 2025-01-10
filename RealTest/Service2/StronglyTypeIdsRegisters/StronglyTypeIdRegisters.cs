using OfX.Abstractions;
using WorkerService1.ModelIds;

namespace WorkerService1.StronglyTypeIdsRegisters;

public sealed class StronglyTypeIdRegisters : IStronglyTypeConverter<UserId>
{
    public UserId Convert(string input) => new(input);

    public bool CanConvert(string input) => true;
}