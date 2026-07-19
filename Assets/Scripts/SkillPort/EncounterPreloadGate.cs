using System;
using System.Collections.Generic;

namespace VLTK.SkillPort
{
    public enum EncounterPreloadState
    {
        Idle = 0,
        Loading = 1,
        RetryRequired = 2,
        Ready = 3,
        Failed = 4,
    }

    public enum EncounterPreloadFailure
    {
        None = 0,
        InvalidRequest = 1,
        MemoryBudgetExceeded = 2,
        Timeout = 3,
    }

    [Serializable]
    public sealed class AssetDependency
    {
        public string sha256;
        public long sizeBytes;
        public bool mandatory = true;

        public AssetDependency() { }

        public AssetDependency(string sha256, long sizeBytes, bool mandatory = true)
        {
            this.sha256 = sha256;
            this.sizeBytes = sizeBytes;
            this.mandatory = mandatory;
        }
    }

    public static class AssetMemoryBudget
    {
        private const long MiB = 1024L * 1024L;

        public static long ForSystemMemoryMegabytes(int systemMemoryMegabytes)
        {
            if (systemMemoryMegabytes >= 8192)
                return 512L * MiB;
            if (systemMemoryMegabytes >= 6144)
                return 384L * MiB;
            return 256L * MiB;
        }
    }

    public sealed class EncounterPreloadGate
    {
        public const long DefaultTimeoutMilliseconds = 10_000L;

        private readonly Dictionary<string, AssetDependency> _required =
            new Dictionary<string, AssetDependency>(StringComparer.Ordinal);
        private readonly List<string> _missing = new List<string>();

        private long _deadlineMilliseconds;
        private long _timeoutMilliseconds;
        private int _attempt;

        public string encounterToken { get; private set; }
        public EncounterPreloadState state { get; private set; } = EncounterPreloadState.Idle;
        public EncounterPreloadFailure failure { get; private set; } = EncounterPreloadFailure.None;
        public long requiredBytes { get; private set; }
        public int attempt => _attempt;
        public bool canReveal => state == EncounterPreloadState.Ready;
        public IReadOnlyList<string> missingHashes => _missing;

        public void Start(
            string encounterToken,
            IEnumerable<AssetDependency> dependencies,
            long nowMilliseconds,
            long assetBudgetBytes,
            long activePinnedBytes,
            long timeoutMilliseconds = DefaultTimeoutMilliseconds)
        {
            Reset();

            if (string.IsNullOrEmpty(encounterToken) ||
                nowMilliseconds < 0 ||
                assetBudgetBytes < 0 ||
                activePinnedBytes < 0 ||
                timeoutMilliseconds <= 0)
            {
                Fail(EncounterPreloadFailure.InvalidRequest);
                return;
            }

            this.encounterToken = encounterToken;
            _timeoutMilliseconds = timeoutMilliseconds;

            if (dependencies != null)
            {
                foreach (AssetDependency dependency in dependencies)
                {
                    if (dependency == null ||
                        !dependency.mandatory ||
                        dependency.sizeBytes < 0 ||
                        !ContentReleaseDigest.IsLowerHexSha256(dependency.sha256))
                    {
                        Fail(EncounterPreloadFailure.InvalidRequest);
                        return;
                    }

                    if (_required.TryGetValue(dependency.sha256, out AssetDependency existing))
                    {
                        if (existing.sizeBytes != dependency.sizeBytes)
                        {
                            Fail(EncounterPreloadFailure.InvalidRequest);
                            return;
                        }
                        continue;
                    }

                    _required.Add(dependency.sha256, dependency);
                    requiredBytes = checked(requiredBytes + dependency.sizeBytes);
                }
            }

            if (_required.Count == 0)
            {
                Fail(EncounterPreloadFailure.InvalidRequest);
                return;
            }

            if (activePinnedBytes > assetBudgetBytes ||
                requiredBytes > assetBudgetBytes - activePinnedBytes)
            {
                Fail(EncounterPreloadFailure.MemoryBudgetExceeded);
                return;
            }

            _attempt = 1;
            _deadlineMilliseconds = checked(nowMilliseconds + _timeoutMilliseconds);
            state = EncounterPreloadState.Loading;
        }

        public EncounterPreloadState Evaluate(
            ISet<string> residentHashes,
            long nowMilliseconds)
        {
            if (state != EncounterPreloadState.Loading)
                return state;

            _missing.Clear();
            foreach (string hash in _required.Keys)
            {
                if (residentHashes == null || !residentHashes.Contains(hash))
                    _missing.Add(hash);
            }
            _missing.Sort(StringComparer.Ordinal);

            if (_missing.Count == 0)
            {
                state = EncounterPreloadState.Ready;
                failure = EncounterPreloadFailure.None;
                return state;
            }

            if (nowMilliseconds < _deadlineMilliseconds)
                return state;

            if (_attempt == 1)
            {
                state = EncounterPreloadState.RetryRequired;
                failure = EncounterPreloadFailure.Timeout;
                return state;
            }

            Fail(EncounterPreloadFailure.Timeout);
            return state;
        }

        /// <summary>
        /// Starts the single allowed retry after the caller has evicted the LRU
        /// non-active working set. Active encounter dependencies must remain pinned.
        /// </summary>
        public bool BeginRetryAfterEviction(long nowMilliseconds)
        {
            if (state != EncounterPreloadState.RetryRequired || _attempt != 1 || nowMilliseconds < 0)
                return false;

            _attempt = 2;
            _deadlineMilliseconds = checked(nowMilliseconds + _timeoutMilliseconds);
            failure = EncounterPreloadFailure.None;
            state = EncounterPreloadState.Loading;
            return true;
        }

        private void Reset()
        {
            encounterToken = null;
            state = EncounterPreloadState.Idle;
            failure = EncounterPreloadFailure.None;
            requiredBytes = 0;
            _deadlineMilliseconds = 0;
            _timeoutMilliseconds = 0;
            _attempt = 0;
            _required.Clear();
            _missing.Clear();
        }

        private void Fail(EncounterPreloadFailure reason)
        {
            failure = reason;
            state = EncounterPreloadState.Failed;
        }
    }

    [Serializable]
    public sealed class CachedAssetState
    {
        public string sha256;
        public long sizeBytes;
        public long lastUsedSequence;
        public bool resident;
        public bool pinnedByActiveEncounter;
    }

    public readonly struct AssetWorkingSetPlan
    {
        public readonly bool success;
        public readonly IReadOnlyList<string> loadHashes;
        public readonly IReadOnlyList<string> evictHashes;
        public readonly long projectedResidentBytes;

        public AssetWorkingSetPlan(
            bool success,
            IReadOnlyList<string> loadHashes,
            IReadOnlyList<string> evictHashes,
            long projectedResidentBytes)
        {
            this.success = success;
            this.loadHashes = loadHashes;
            this.evictHashes = evictHashes;
            this.projectedResidentBytes = projectedResidentBytes;
        }
    }

    public static class AssetWorkingSetPlanner
    {
        private sealed class EvictionCandidate
        {
            public string hash;
            public long sizeBytes;
            public long lastUsedSequence;
        }

        public static AssetWorkingSetPlan Plan(
            IEnumerable<CachedAssetState> cache,
            IEnumerable<AssetDependency> required,
            long budgetBytes)
        {
            var cacheByHash = new Dictionary<string, CachedAssetState>(StringComparer.Ordinal);
            var requiredByHash = new Dictionary<string, AssetDependency>(StringComparer.Ordinal);
            var load = new List<string>();
            var evict = new List<string>();
            var candidates = new List<EvictionCandidate>();
            long residentBytes = 0;

            if (budgetBytes < 0)
                return new AssetWorkingSetPlan(false, load, evict, 0);

            if (cache != null)
            {
                foreach (CachedAssetState entry in cache)
                {
                    if (entry == null || entry.sizeBytes < 0 ||
                        !ContentReleaseDigest.IsLowerHexSha256(entry.sha256) ||
                        cacheByHash.ContainsKey(entry.sha256))
                    {
                        return new AssetWorkingSetPlan(false, load, evict, 0);
                    }
                    cacheByHash.Add(entry.sha256, entry);
                    if (entry.resident)
                        residentBytes = checked(residentBytes + entry.sizeBytes);
                }
            }

            if (required != null)
            {
                foreach (AssetDependency dependency in required)
                {
                    if (dependency == null || !dependency.mandatory || dependency.sizeBytes < 0 ||
                        !ContentReleaseDigest.IsLowerHexSha256(dependency.sha256))
                    {
                        return new AssetWorkingSetPlan(false, load, evict, residentBytes);
                    }
                    if (requiredByHash.TryGetValue(dependency.sha256, out AssetDependency existing))
                    {
                        if (existing.sizeBytes != dependency.sizeBytes)
                            return new AssetWorkingSetPlan(false, load, evict, residentBytes);
                        continue;
                    }
                    requiredByHash.Add(dependency.sha256, dependency);
                }
            }

            long projected = residentBytes;
            foreach (AssetDependency dependency in requiredByHash.Values)
            {
                if (!cacheByHash.TryGetValue(dependency.sha256, out CachedAssetState cached) || !cached.resident)
                {
                    load.Add(dependency.sha256);
                    projected = checked(projected + dependency.sizeBytes);
                }
                else if (cached.sizeBytes != dependency.sizeBytes)
                {
                    return new AssetWorkingSetPlan(false, load, evict, projected);
                }
            }

            foreach (CachedAssetState cached in cacheByHash.Values)
            {
                if (!cached.resident || cached.pinnedByActiveEncounter || requiredByHash.ContainsKey(cached.sha256))
                    continue;
                candidates.Add(new EvictionCandidate
                {
                    hash = cached.sha256,
                    sizeBytes = cached.sizeBytes,
                    lastUsedSequence = cached.lastUsedSequence,
                });
            }
            candidates.Sort((a, b) =>
            {
                int byUse = a.lastUsedSequence.CompareTo(b.lastUsedSequence);
                return byUse != 0 ? byUse : string.CompareOrdinal(a.hash, b.hash);
            });

            for (int i = 0; projected > budgetBytes && i < candidates.Count; i++)
            {
                evict.Add(candidates[i].hash);
                projected -= candidates[i].sizeBytes;
            }

            load.Sort(StringComparer.Ordinal);
            return new AssetWorkingSetPlan(projected <= budgetBytes, load, evict, projected);
        }
    }
}
