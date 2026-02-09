using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OfX.Extensions;

/// <summary>
/// Provides extension methods for working with Tasks, including cancellation and timeout support.
/// </summary>
/// <remarks>
/// These utilities provide robust async operation handling with proper exception management,
/// timeout support with detailed error messages, and cancellation token integration.
/// </remarks>
public static class TaskExtensions
{
    private static readonly TimeSpan DefaultTimeout = new(0, 0, 0, 5, 0);

    /// <summary>
    /// Returns a task that completes when the original task completes or throws if cancelled.
    /// </summary>
    /// <param name="task">The task to wrap.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A task that completes when the original completes or throws OperationCanceledException.</returns>
    public static Task OrCanceled(this Task task, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            return task;

        if (!cancellationToken.IsCancellationRequested) return WaitAsync();
        task.IgnoreUnobservedExceptions();
        throw new OperationCanceledException(cancellationToken);

        async Task WaitAsync()
        {
            await using var registration =
                RegisterTask(cancellationToken, out var cancelTask).ConfigureAwait(false);

            var completed = await Task.WhenAny(task, cancelTask).ConfigureAwait(false);
            if (completed != task)
            {
                task.IgnoreUnobservedExceptions();
                throw new OperationCanceledException(cancellationToken);
            }

            task.GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Returns a task that completes when the original task completes or throws if cancelled.
    /// </summary>
    /// <typeparam name="T">The result type of the task.</typeparam>
    /// <param name="task">The task to wrap.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A task that completes when the original completes or throws OperationCanceledException.</returns>
    public static Task<T> OrCanceled<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            return task;

        if (!cancellationToken.IsCancellationRequested) return WaitAsync();
        task.IgnoreUnobservedExceptions();
        throw new OperationCanceledException(cancellationToken);

        async Task<T> WaitAsync()
        {
            await using var registration =
                RegisterTask(cancellationToken, out var cancelTask).ConfigureAwait(false);


            var completed = await Task.WhenAny(task, cancelTask).ConfigureAwait(false);
            if (completed == task) return task.GetAwaiter().GetResult();
            task.IgnoreUnobservedExceptions();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    extension(Task task)
    {
        public Task OrTimeout(int ms = 0, int s = 0, int m = 0, int h = 0, int d = 0,
            CancellationToken cancellationToken = default,
            [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null,
            [CallerLineNumber] int? lineNumber = null)
        {
            var timeout = new TimeSpan(d, h, m, s, ms);
            if (timeout == TimeSpan.Zero)
                timeout = DefaultTimeout;

            return task.OrTimeoutInternal(timeout, cancellationToken, memberName, filePath, lineNumber);
        }

        public Task OrTimeout(TimeSpan timeout, CancellationToken cancellationToken = default,
            [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null,
            [CallerLineNumber] int? lineNumber = null)
        {
            return task.OrTimeoutInternal(timeout, cancellationToken, memberName, filePath, lineNumber);
        }

        private Task OrTimeoutInternal(TimeSpan timeout, CancellationToken cancellationToken,
            string memberName, string filePath,
            int? lineNumber)
        {
            if (task.IsCompleted)
                return task;

            if (!cancellationToken.IsCancellationRequested) return WaitAsync();
            task.IgnoreUnobservedExceptions();
            throw new TimeoutException(FormatTimeoutMessage(memberName, filePath, lineNumber));

            async Task WaitAsync()
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var delayTask = Task.Delay(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : timeout, cts.Token);
                var completed = await Task.WhenAny(task, delayTask).ConfigureAwait(false);

                if (completed == delayTask)
                {
                    task.IgnoreUnobservedExceptions();

                    throw new TimeoutException(FormatTimeoutMessage(memberName, filePath, lineNumber));
                }

                await cts.CancelAsync();

                task.GetAwaiter().GetResult();
            }
        }

        public void Forget()
        {
            // note: this code is inspired by a tweet from Ben Adams: https://twitter.com/ben_a_adams/status/1045060828700037125
            // Only care about tasks that may fault (not completed) or are faulted,
            // so fast-path for SuccessfullyCompleted and Canceled tasks.
            if (!task.IsCompleted || task.IsFaulted)
            {
                // use "_" (Discard operation) to remove the warning IDE0058: Because this call is not awaited, execution of the current method continues before the call is completed
                // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/discards?WT.mc_id=DT-MVP-5003978#a-standalone-discard
                _ = ForgetAwaited(task);
            }

            return;

            // Allocate the async/await state machine only when needed for performance reasons.
            // More info about the state machine: https://blogs.msdn.microsoft.com/seteplia/2017/11/30/dissecting-the-async-methods-in-c/?WT.mc_id=DT-MVP-5003978
            static async Task ForgetAwaited(Task task)
            {
                try
                {
                    // No need to resume on the original SynchronizationContext, so use ConfigureAwait(false)
                    await task.ConfigureAwait(false);
                }
                catch
                {
                    // Nothing to do here
                }
            }
        }
    }

    /// <param name="task">The task to wrap.</param>
    /// <typeparam name="T">The result type of the task.</typeparam>
    extension<T>(Task<T> task)
    {
        /// <summary>
        /// Returns a task that completes when the original task completes or throws TimeoutException if timed out.
        /// </summary>
        /// <param name="ms">Milliseconds component of the timeout.</param>
        /// <param name="s">Seconds component of the timeout.</param>
        /// <param name="m">Minutes component of the timeout.</param>
        /// <param name="h">Hours component of the timeout.</param>
        /// <param name="d">Days component of the timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <param name="memberName">Caller member name (auto-populated).</param>
        /// <param name="filePath">Caller file path (auto-populated).</param>
        /// <param name="lineNumber">Caller line number (auto-populated).</param>
        /// <returns>A task that completes when the original completes or throws TimeoutException.</returns>
        public Task<T> OrTimeout(int ms = 0, int s = 0, int m = 0, int h = 0, int d = 0,
            CancellationToken cancellationToken = default,
            [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null,
            [CallerLineNumber] int? lineNumber = null)
        {
            var timeout = new TimeSpan(d, h, m, s, ms);
            if (timeout == TimeSpan.Zero)
                timeout = DefaultTimeout;

            return task.OrTimeoutInternal(timeout, cancellationToken, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// Returns a task that completes when the original task completes or throws TimeoutException if timed out.
        /// </summary>
        /// <param name="timeout">The timeout duration.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <param name="memberName">Caller member name (auto-populated).</param>
        /// <param name="filePath">Caller file path (auto-populated).</param>
        /// <param name="lineNumber">Caller line number (auto-populated).</param>
        /// <returns>A task that completes when the original completes or throws TimeoutException.</returns>
        public Task<T> OrTimeout(TimeSpan timeout,
            CancellationToken cancellationToken = default,
            [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null,
            [CallerLineNumber] int? lineNumber = null)
        {
            return task.OrTimeoutInternal(timeout, cancellationToken, memberName, filePath, lineNumber);
        }

        private Task<T> OrTimeoutInternal(TimeSpan timeout, CancellationToken cancellationToken,
            string memberName, string filePath,
            int? lineNumber)
        {
            if (task.IsCompleted)
                return task;

            if (!cancellationToken.IsCancellationRequested) return WaitAsync();
            task.IgnoreUnobservedExceptions();
            throw new TimeoutException(FormatTimeoutMessage(memberName, filePath, lineNumber));

            async Task<T> WaitAsync()
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var delayTask = Task.Delay(Debugger.IsAttached ? Timeout.InfiniteTimeSpan : timeout, cts.Token);
                var completed = await Task.WhenAny(task, delayTask).ConfigureAwait(false);

                if (completed == delayTask)
                {
                    task.IgnoreUnobservedExceptions();

                    throw new TimeoutException(FormatTimeoutMessage(memberName, filePath, lineNumber));
                }

                await cts.CancelAsync();

                return task.GetAwaiter().GetResult();
            }
        }
    }

    static string FormatTimeoutMessage(string memberName, string filePath, int? lineNumber) =>
        !string.IsNullOrEmpty(memberName)
            ? $"Operation in {memberName} timed out at {filePath}:{lineNumber}"
            : "Operation timed out";

    /// <param name="task"></param>
    extension(Task task)
    {
        /// <summary>
        /// Returns true if a Task was ran to completion (without being cancelled or faulted)
        /// </summary>
        /// <returns></returns>
        public bool IsCompletedSuccessfully() => task.Status == TaskStatus.RanToCompletion;

        private void IgnoreUnobservedExceptions()
        {
            if (task.IsCompleted)
                _ = task.Exception;
            else
            {
                task.ContinueWith(t => { t.Exception?.Handle(_ => true); },
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }
        }
    }

    extension<T>(TaskCompletionSource<T> source)
    {
        public void TrySetFromTask(Task task, T value)
        {
            switch (task)
            {
                case { IsCanceled: true }:
                    source.TrySetCanceled();
                    break;
                case { IsFaulted: true, Exception.InnerExceptions: not null }:
                    source.TrySetException(task.Exception.InnerExceptions);
                    break;
                case { IsFaulted: true, Exception: not null }:
                    source.TrySetException(task.Exception);
                    break;
                case { IsFaulted: true, Exception: null }:
                    source.TrySetException(
                        new InvalidOperationException("The context faulted but no exception was present."));
                    break;
                default:
                    source.TrySetResult(value);
                    break;
            }
        }

        public void TrySetFromTask(Task<T> task)
        {
            switch (task)
            {
                case { IsCanceled: true }:
                    source.TrySetCanceled();
                    break;
                case { IsFaulted: true, Exception.InnerExceptions: not null }:
                    source.TrySetException(task.Exception.InnerExceptions);
                    break;
                case { IsFaulted: true, Exception: not null }:
                    source.TrySetException(task.Exception);
                    break;
                case { IsFaulted: true, Exception: null }:
                    source.TrySetException(
                        new InvalidOperationException("The context faulted but no exception was present."));
                    break;
                default:
                    source.TrySetResult(task.Result);
                    break;
            }
        }
    }

    /// <summary>
    /// Register a callback on the <paramref name="cancellationToken" /> which completes the resulting task.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="cancelTask"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static CancellationTokenRegistration RegisterTask(CancellationToken cancellationToken, out Task cancelTask)
    {
        if (!cancellationToken.CanBeCanceled)
            throw new ArgumentException("The cancellationToken must support cancellation",
                nameof(cancellationToken));

        var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        cancelTask = source.Task;

        return cancellationToken.Register(SetCompleted, source);
    }

    private static void SetCompleted(object obj)
    {
        if (obj is TaskCompletionSource<bool> source)
            source.TrySetResult(true);
    }
}