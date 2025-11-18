using OfX.BenchmarkLab.Attributes;

namespace OfX.BenchmarkLab.OfXBenchmarks.Objects;

public class User
{
    public string Id { get; set; }

    [UserOf(nameof(Id), Expression = "Name")]
    public string Name { get; set; }

    [UserOf(nameof(Id), Expression = "Email")]
    public string Email { get; set; }
}

public class UserMock
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}