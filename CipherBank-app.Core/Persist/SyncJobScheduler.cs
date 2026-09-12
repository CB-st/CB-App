// <copyright file="SyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class SyncJobScheduler : ISyncJobScheduler
{
    private readonly object _gate = new();
    private readonly PriorityQueue<QueuedJob, (int Priority, long Sequence)> _queue = new();
    private readonly HashSet<string> _queuedKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Task> _activeJobs = new(StringComparer.Ordinal);
    private readonly TaskFactory _taskFactory;
    private readonly int _maxConcurrency;
    private long _sequence;

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
    public void Enqueue(string key, SyncPriority priority, Func<CancellationToken, Task> work)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(work);

        lock (_gate)
        {
            if (_activeJobs.ContainsKey(key) || _queuedKeys.Contains(key))
            {
                return;
            }

            _queue.Enqueue(new QueuedJob(key, work), ((int)priority, ++_sequence));
            _queuedKeys.Add(key);
            DispatchEligibleLocked();
        }
    }

    /// <summary>
    /// Waits until the factory has no running or queued jobs, via <see cref="Task.WhenAll(Task[])"/>
    /// over the tracked job tasks. Not part of <see cref="ISyncJobScheduler"/> — tests and shutdown
    /// paths hold the concrete type.
    /// Use: Low (tests / shutdown). Scope: SyncJobScheduler instance.
    /// </summary>
    public async Task DrainAsync(CancellationToken ct)
    {
        while (true)
        {
            Task[] active;
            lock (_gate)
            {
                if (_activeJobs.Count == 0 && _queue.Count == 0)
                {
                    return;
                }

                active = [.. _activeJobs.Values];
            }

            await Task.WhenAll(active).WaitAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Starts queued jobs through the task factory while whole-job capacity remains.
    /// Caller holds the gate.
    /// Use: High (after each enqueue / completion). Scope: SyncJobScheduler instance.
    /// </summary>
    private void DispatchEligibleLocked()
    {
        while (_activeJobs.Count < _maxConcurrency && _queue.Count > 0)
        {
            QueuedJob job = _queue.Dequeue();
            _queuedKeys.Remove(job.Key);

            Task run = _taskFactory.StartNew(job.InvokeAsync).Unwrap();

            // Continuation is queued (not synchronous) so completion cannot re-enter the
            // gate held by this dispatch loop; the tracked task completes only after the
            // key is released, which keeps DrainAsync honest.
            Task tracked = run.ContinueWith(
                _ => OnJobCompleted(job.Key),
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
            _activeJobs[job.Key] = tracked;
        }
    }

    /// <summary>
    /// Releases a completed job's key and dispatches the next eligible work.
    /// Use: High (per job). Scope: SyncJobScheduler instance.
    /// </summary>
    private void OnJobCompleted(string key)
    {
        lock (_gate)
        {
            _activeJobs.Remove(key);
            DispatchEligibleLocked();
        }
    }

    private sealed record QueuedJob(string Key, Func<CancellationToken, Task> Work)
    {
        /// <summary>
        /// Runs the job body; jobs own their errors, so expected failure shapes are observed here.
        /// Use: High (per job). Scope: SyncJobScheduler dispatch.
        /// </summary>
        public async Task InvokeAsync()
        {
            try
            {
                await Work(CancellationToken.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Jobs own their errors.
            }
            catch (ObjectDisposedException)
            {
                // Jobs own their errors.
            }
            catch (InvalidOperationException)
            {
                // Jobs own their errors.
            }
            catch (IOException)
            {
                // Jobs own their errors.
            }
        }
    }
}
