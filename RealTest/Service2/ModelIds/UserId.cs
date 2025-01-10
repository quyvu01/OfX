namespace WorkerService1.ModelIds;

public record UserId(string Value) : StronglyTypedId<string>(Value)
{
    public override string ToString() => base.ToString();
}