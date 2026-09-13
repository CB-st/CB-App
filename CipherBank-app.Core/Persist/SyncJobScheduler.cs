// <copyright file="SyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class SyncJobScheduler : ISyncJobScheduler, IDisposable
{
    private readonly object _gate = new();
    private readonly PriorityQueue<QueuedJob, (int Priority, long Sequence)> _queue = new();
    private readonly Dictionary<string, QueuedJob> _jobs = new(StringComparer.Ordinal);
    private readonly TaskFactory _taskFactory;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly int _maxConcurrency;
    private long _sequence;
    private int _running;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncJobScheduler"/> class over
    /// <see cref="TaskScheduler.Default"/> with default options.
    /// Use: Medium (tests). Scope: persist.
    /// </summary>
    public SyncJobScheduler()
        : this(TaskScheduler.Default, new SyncSchedulerOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncJobScheduler"/> class over an injected
    /// platform <see cref="TaskScheduler"/>.
    /// Use: Medium (host DI / tests). Scope: persist.
    /// </summary>
    public SyncJobScheduler(TaskScheduler taskScheduler, SyncSchedulerOptions options)
    {
        ArgumentNullException.ThrowIfNull(taskScheduler);
        ArgumentNullException.ThrowIfNull(options);
        _taskFactory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach,
            TaskContinuationOptions.None,
            taskScheduler);
        _maxConcurrency = options.Resolve();
    }

    /// <inheritdoc />
    public Task EnqueueAsync(
        string key,
        SyncPriority priority,
        Func<CancellationToken, Task> work,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(work);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_jobs.TryGetValue(key, out QueuedJob? existing))
            {
                return existing.Completion.Task;
            }

            CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                ct,
                _shutdown.Token);
            QueuedJob job = new QueuedJob(key, work, cancellation);
            _jobs.Add(key, job);
            _queue.Enqueue(job, ((int)priority, ++_sequence));
            DispatchEligibleLocked();
            return job.Completion.Task;
        }
    }

    /// <inheritdoc />
    public async Task DrainAsync(CancellationToken ct = default)
    {
        while (true)
        {
            Task[] jobs;
            lock (_gate)
            {
                if (_jobs.Count == 0)
                {
                    return;
                }

                jobs = _jobs.Values.Select(job => job.Completion.Task).ToArray();
            }

            await Task.WhenAll(jobs).WaitAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Cancels queued and running jobs during container shutdown.
    /// Use: Low (container shutdown). Scope: process-wide scheduler.
    /// </summary>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _shutdown.Cancel();
            _shutdown.Dispose();
        }
    }

    /// <summary>
    /// Starts queued jobs through the task factory while whole-job capacity remains.
    /// Caller holds the gate.
    /// Use: High (after each enqueue / completion). Scope: SyncJobScheduler instance.
    /// </summary>
    private void DispatchEligibleLocked()
    {
        while (_running < _maxConcurrency && _queue.Count > 0)
        {
            QueuedJob job = _queue.Dequeue();
            _running++;
            _ = _taskFactory.StartNew(
                () => RunJobAsync(job),
                CancellationToken.None).Unwrap();
        }
    }

    /// <summary>
    /// Completes one observable job and releases its deduplication key.
    /// Use: High (per job). Scope: SyncJobScheduler dispatch.
    /// </summary>
    private async Task RunJobAsync(QueuedJob job)
    {
        try
        {
            job.Cancellation.Token.ThrowIfCancellationRequested();
            await job.Work(job.Cancellation.Token).ConfigureAwait(false);
            job.Completion.TrySetResult();
        }
        catch (OperationCanceledException) when (job.Cancellation.IsCancellationRequested)
        {
            job.Completion.TrySetCanceled(job.Cancellation.Token);
        }
        catch (OperationCanceledException exception)
        {
            job.Completion.TrySetException(exception);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            job.Completion.TrySetException(exception);
        }
        finally
        {
            lock (_gate)
            {
                _jobs.Remove(job.Key);
                _running--;
                DispatchEligibleLocked();
            }

            job.Cancellation.Dispose();
        }
    }

    private sealed record QueuedJob(
        string Key,
        Func<CancellationToken, Task> Work,
        CancellationTokenSource Cancellation)
    {
        internal TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
