namespace OfX.Responses;

/// <summary>
/// Represents fault information for a failed OfX request.
/// Inspired by MassTransit's Fault message structure.
/// </summary>
public sealed class Fault
{
    /// <summary>
    /// The maximum depth of exception chain to capture.
    /// Prevents infinite loops from circular exception references.
    /// </summary>
    private const int MaxExceptionDepth = 16;

    /// <summary>
    /// Gets or sets the unique identifier for this fault.
    /// </summary>
    public string FaultId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the message that caused the fault.
    /// </summary>
    public string FaultedMessageId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the fault occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the exception information associated with this fault.
    /// </summary>
    public ExceptionInfo[] Exceptions { get; set; }

    /// <summary>
    /// Gets or sets the host information where the fault occurred.
    /// </summary>
    public HostInfo Host { get; set; }

    /// <summary>
    /// Creates a fault from an exception.
    /// </summary>
    /// <param name="exception">The exception that caused the fault.</param>
    /// <param name="faultedMessageId">Optional identifier for the faulted message.</param>
    /// <returns>A new Fault instance.</returns>
    public static Fault FromException(Exception exception, string faultedMessageId = null)
    {
        var exceptions = new List<ExceptionInfo>();
        var currentException = exception;
        var depth = 0;

        while (currentException != null && depth < MaxExceptionDepth)
        {
            exceptions.Add(new ExceptionInfo
            {
                ExceptionType = currentException.GetType().FullName,
                Message = currentException.Message,
                StackTrace = currentException.StackTrace,
                Source = currentException.Source
            });
            currentException = currentException.InnerException;
            depth++;
        }

        return new Fault
        {
            FaultId = Guid.NewGuid().ToString(),
            FaultedMessageId = faultedMessageId,
            Timestamp = DateTime.UtcNow,
            Exceptions = exceptions.ToArray(),
            Host = HostInfo.Current
        };
    }

    /// <summary>
    /// Converts this fault back to an exception.
    /// Reconstructs the exception chain from the stored exception information.
    /// </summary>
    /// <returns>An exception representing this fault, or null if no exception info exists.</returns>
    public Exception ToException()
    {
        if (Exceptions is not { Length: > 0 })
            return null;

        // Build exception chain from innermost to outermost
        Exception innerException = null;
        for (var i = Exceptions.Length - 1; i >= 0; i--)
        {
            var exInfo = Exceptions[i];
            innerException = new FaultException(exInfo, innerException);
        }

        return innerException;
    }
}

/// <summary>
/// Represents a reconstructed exception from fault information.
/// </summary>
public sealed class FaultException : Exception
{
    /// <summary>
    /// Gets the original exception type name.
    /// </summary>
    public string OriginalExceptionType { get; }

    /// <summary>
    /// Gets the original stack trace.
    /// </summary>
    public string OriginalStackTrace { get; }

    /// <summary>
    /// Gets the original source.
    /// </summary>
    public string OriginalSource { get; }

    /// <summary>
    /// Creates a new FaultException from exception info.
    /// </summary>
    public FaultException(ExceptionInfo exceptionInfo, Exception innerException = null)
        : base(exceptionInfo?.Message ?? "Unknown error", innerException)
    {
        OriginalExceptionType = exceptionInfo?.ExceptionType;
        OriginalStackTrace = exceptionInfo?.StackTrace;
        OriginalSource = exceptionInfo?.Source;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var result = $"{OriginalExceptionType ?? GetType().FullName}: {Message}";
        if (!string.IsNullOrEmpty(OriginalStackTrace))
            result += Environment.NewLine + OriginalStackTrace;
        return result;
    }
}

/// <summary>
/// Represents exception information within a fault.
/// </summary>
public sealed class ExceptionInfo
{
    /// <summary>
    /// Gets or sets the full type name of the exception.
    /// </summary>
    public string ExceptionType { get; set; }

    /// <summary>
    /// Gets or sets the exception message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the stack trace of the exception.
    /// </summary>
    public string StackTrace { get; set; }

    /// <summary>
    /// Gets or sets the source of the exception.
    /// </summary>
    public string Source { get; set; }
}

/// <summary>
/// Represents host information where the fault occurred.
/// </summary>
public sealed class HostInfo
{
    /// <summary>
    /// Gets or sets the machine name.
    /// </summary>
    public string MachineName { get; set; }

    /// <summary>
    /// Gets or sets the process name.
    /// </summary>
    public string ProcessName { get; set; }

    /// <summary>
    /// Gets or sets the process ID.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the assembly name.
    /// </summary>
    public string Assembly { get; set; }

    /// <summary>
    /// Gets the current host information.
    /// </summary>
    public static HostInfo Current
    {
        get
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            return new HostInfo
            {
                MachineName = Environment.MachineName,
                ProcessName = process.ProcessName,
                ProcessId = process.Id,
                Assembly = AppDomain.CurrentDomain.FriendlyName
            };
        }
    }
}