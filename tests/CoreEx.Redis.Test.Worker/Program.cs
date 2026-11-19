var connectionString = Environment.GetEnvironmentVariable("COREEX_REDIS_TEST_CONNECTION") ?? "localhost:6379";
using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
var options = new CoordinationOptions { LeaseDuration = TimeSpan.FromMilliseconds(900), RenewalInterval = TimeSpan.FromMilliseconds(200), OperationTimeout = TimeSpan.FromMilliseconds(300), Retention = TimeSpan.FromSeconds(10) };
var provider = new RedisCoordination(connection, new DefaultCacheKeyProvider(), NullLogger<RedisCoordination>.Instance, options);
await using var ownership = args[0] == "lease" ? await provider.TryAcquireAsync(args[1]) : (await provider.TryBeginAsync(args[1], "request")).Attempt;
Console.WriteLine(ownership is null ? "contended" : "acquired");
if (ownership is not null)
    await Task.Delay(Timeout.InfiniteTimeSpan);
