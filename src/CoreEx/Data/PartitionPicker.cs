namespace CoreEx.Data;

/// <summary>
/// Determines which partitions a worker should probe on a given polling iteration.
/// </summary>
/// <remarks>
/// Design goals:
/// <list type="bullet">
///  <item>No sticky ownership of partitions (workers can come and go freely).</item>
///  <item>Deterministic distribution across workers (reduces repository contention / herd effects).</item>
///  <item>Temporal locality: if a partition just produced work, try it again first.</item>
///  <item>Stable behavior across OS/arch.</item>
/// </list>
/// <para><i>Note</i>: this class is <b>not</b> thread-safe for concurrent use by multiple workers; create one instance per worker task (to be used for its lifetime).</para>
/// </remarks>
public sealed class PartitionPicker
{
    /* To be clear up-front; this has been largely developed with assistance from AI - suggest using it to explain where applicable :-) */

    private readonly Guid _workerId = Guid.NewGuid();
    private readonly int _partitionSize; 
    private readonly int _perWorkerPartitionCount; 
    private readonly int _rotationSeconds;
    private readonly int _epochSkew;
    private int _prioritizedPartition = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="PartitionPicker"/> class.
    /// </summary>
    /// <param name="partitionSize">The total number of partitions available for distribution.</param>
    /// <param name="perWorkerPartitionCount">The number of partitions assigned to each worker (is essentially the probe count per polling loop).</param>
    /// <param name="rotationSeconds">The time interval, in seconds, for rotating partition assignments.</param>
    public PartitionPicker(int partitionSize, int perWorkerPartitionCount, int rotationSeconds = 5)
    {
        _partitionSize = partitionSize.ThrowWhen(_partitionSize => _partitionSize <= 0);
        _perWorkerPartitionCount = perWorkerPartitionCount.ThrowWhen(perWorkerPartitionCount => perWorkerPartitionCount <= 0 || perWorkerPartitionCount > partitionSize);
        _rotationSeconds = rotationSeconds.ThrowWhen(rotationSeconds => rotationSeconds <= 0);

        // Derive a small deterministic skew from worker-id so epoch boundaries do not cause synchronized shifts.
        static uint Hash32(byte[] bytes)
        {
            var hash = SHA256.HashData(bytes);
            return (uint)(hash[0] | (hash[1] << 8) | (hash[2] << 16) | (hash[3] << 24));
        }

        _epochSkew = (int)(Hash32(_workerId.ToByteArray()) % 3);
    }

    /// <summary>
    /// Gets the total number of partitions available for distribution.
    /// </summary>
    public int PartitionSize => _partitionSize;

    /// <summary>
    /// Gets the number of partitions assigned to each worker.
    /// </summary>
    public int PerWorkerPartitionCount => _perWorkerPartitionCount;

    /// <summary>
    /// Gets the time interval, in seconds, for rotating partition assignments.
    /// </summary>
    public int RotationSeconds => _rotationSeconds;

    /// <summary>
    /// Gets an ordered set of partitions to probe during the next poll loop.
    /// </summary>
    public int[] GetNextPartitions(DateTimeOffset utcNow)
    {
        long epoch = utcNow.ToUnixTimeSeconds() / _rotationSeconds;

        // Apply worker-specific skew to reduce boundary synchronization.
        epoch += _epochSkew;

        // Fast path: probe all partitions: useful in local dev when running a single worker and you want full sweep.
        if (_perWorkerPartitionCount == _partitionSize)
        {
            var all = new int[_partitionSize];
            int ls = System.Threading.Volatile.Read(ref _prioritizedPartition);
            int idx = 0;

            // Drain last-success first (temporal locality)
            if ((uint)ls < (uint)_partitionSize)
                all[idx++] = ls;

            for (int p = 0; p < _partitionSize; p++)
            {
                if (p == ls) continue;
                all[idx++] = p;
            }

            return all;
        }

        // Fast path: only one partition per loop. Minimizes repository chatter but may slow discovery of work.
        if (_perWorkerPartitionCount == 1)
        {
            int ls = System.Threading.Volatile.Read(ref _prioritizedPartition);
            if ((uint)ls < (uint)_partitionSize)
                return [ls];

            int start = (int)(Hash32(_workerId, epoch, salt: 1) % (uint)_partitionSize);
            return [start];
        }

        // Deterministic start position for this worker + epoch.
        int startPos = (int)(Hash32(_workerId, epoch, salt: 1) % (uint)_partitionSize);

        // Deterministic stride ensures good coverage of partitions; for power-of-two totals (32), stride must be odd.
        int strideCandidate = (int)(Hash32(_workerId, epoch, salt: 2) % (uint)(_partitionSize - 1)) + 1;
        int stride = EnsureCoprimeStride(_partitionSize, strideCandidate);

        var result = new List<int>(_perWorkerPartitionCount + 1);
        var seen = new HashSet<int>(_perWorkerPartitionCount + 1);

        // Prefer last successful partition first.
        int last = System.Threading.Volatile.Read(ref _prioritizedPartition);
        if ((uint)last < (uint)_partitionSize && seen.Add(last))
            result.Add(last);

        // Walk partitions in (start + i*stride) mod total; long arithmetic avoids overflow.
        for (int i = 0; result.Count < _perWorkerPartitionCount; i++)
        {
            int p = (int)(((long)startPos + ((long)i * stride)) % _partitionSize);

            if (seen.Add(p))
                result.Add(p);
        }

        return [.. result];
    }

    /// <summary>
    /// Hashes worker-id + epoch + salt into a stable 32-bit value to avoid runtime-dependent GetHashCode behavior.
    /// </summary>
    private static uint Hash32(Guid workerId, long epoch, int salt)
    {
        Span<byte> buffer = stackalloc byte[16 + sizeof(long) + sizeof(int)];

        workerId.TryWriteBytes(buffer);
        BitConverter.TryWriteBytes(buffer[16..], epoch);
        BitConverter.TryWriteBytes(buffer[(16 + sizeof(long))..], salt);

        var hash = SHA256.HashData(buffer);
        return (uint)(hash[0] | (hash[1] << 8) | (hash[2] << 16) | (hash[3] << 24));
    }

    /// <summary>
    /// Ensures stride is coprime with total. For power-of-two totals (e.g., 32), coprime means odd.
    /// </summary>
    private static int EnsureCoprimeStride(int total, int candidate)
    {
        // Computes the greatest common divisor (GCD) of two integers using the classic Euclidean algorithm.
        static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                int t = a % b;
                a = b;
                b = t;
            }

            return Math.Abs(a);
        }

        if ((total & (total - 1)) == 0)
        {
            candidate |= 1;
            if (candidate >= total) candidate -= 2;
            if (candidate <= 0) candidate = 1;
            return candidate;
        }

        while (Gcd(candidate, total) != 1)
        {
            candidate++;
            if (candidate >= total) candidate = 1;
        }
        return candidate;
    }

    /// <summary>
    /// Prioritizes the specified partition for the next pick, by virtue of it being the most likely to have immediate work given recent successful processing.
    /// </summary>
    /// <remarks>This should only be called where work was completed successfully for the specified <paramref name="partitionId"/> and that there is a high-likelihood of immediate work for that partition.
    /// This is a hint and not a guarantee that the partition will be picked first next time, but it will increase the likelihood.</remarks>
    public void PrioritizePartition(int partitionId)
    {
        if ((uint)partitionId < (uint)_partitionSize)
            System.Threading.Volatile.Write(ref _prioritizedPartition, partitionId);
    }
}