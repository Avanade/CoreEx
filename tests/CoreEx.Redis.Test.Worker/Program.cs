var connectionString = Environment.GetEnvironmentVariable("COREEX_REDIS_TEST_CONNECTION") ?? "localhost:6379";
using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
var options = new CoordinationOptions { LeaseDuration = TimeSpan.FromMilliseconds(900), RenewalInterval = TimeSpan.FromMilliseconds(200), OperationTimeout = TimeSpan.FromMilliseconds(300), Retention = TimeSpan.FromSeconds(10) };
var provider = new RedisCoordination(connection, NullLogger<RedisCoordination>.Instance, options);
var key = System.Text.Json.JsonSerializer.Deserialize<CoreEx.Operations.OperationKey>(args[1]) ?? throw new InvalidOperationException("Missing operation key.");
await using ISynchronizationLease? ownership = args[0] switch
{
    "lease" => await provider.TryAcquireAsync(key),
    "attempt" => (await provider.TryBeginAsync(key, "request")).Attempt,
    _ => throw new ArgumentException("Unsupported worker operation.", nameof(args))
};
Console.WriteLine(ownership is null ? "contended" : "acquired");
if (ownership is not null)
    await Task.Delay(Timeout.InfiniteTimeSpan);
