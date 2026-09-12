// <copyright file="ISyncJobScheduler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// Named, deduplicating task factory for market persist work (P1 chart / P2 cold bootstrap).
/// Jobs dispatch through an injected <see cref="TaskScheduler"/> via <see cref="TaskFactory"/>;
/// waiting work is ordered P1-before-P2 (FIFO within a rank) and the whole async job — not just
/// its first synchronous segment — counts against the mobile concurrency cap. The whole-job cap
/// and the keyed skip-duplicate contract are factory policy: a <see cref="TaskScheduler"/>
/// subclass caps only synchronous task segments (an async job frees its scheduler slot at the
/// first await), so inheritance cannot express either guarantee.
/// </summary>
public interface ISyncJobScheduler
{
    /// <summary>
    /// Enqueues keyed work; duplicate keys already pending or in-flight are ignored.
    /// Use: High (Home market refresh). Scope: process-wide sync scheduler.
    /// </summary>
    void Enqueue(string key, SyncPriority priority, Func<CancellationToken, Task> work);
}
