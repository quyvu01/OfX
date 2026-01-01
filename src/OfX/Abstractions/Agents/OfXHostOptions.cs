namespace OfX.Abstractions.Agents;

public class OfXHostOptions
{
    public bool WaitUntilStarted { get; set; } = false;
    public TimeSpan? StartTimeout { get; set; }
    public TimeSpan? StopTimeout { get; set; }
}