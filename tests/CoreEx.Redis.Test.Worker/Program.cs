var connectionString = Environment.GetEnvironmentVariable("COREEX_REDIS_TEST_CONNECTION") ?? "localhost:6379";
using var connection = await ConnectionMultiplexer.ConnectAsync(connectionString);
var options = new CoordinationOptions { LeaseDuration = TimeSpan.FromMilliseconds(900), RenewalInterval = TimeSpan.FromMilliseconds(200), OperationTimeout = TimeSpan.FromMilliseconds(300), Retention = TimeSpan.FromSeconds(10) };
var provider = new RedisCoordination(connection, new CoreEx.Hosting.HostSettings { SolutionName = "redis-tests", DomainName = args[1], EnvironmentName = "test" }, NullLogger<RedisCoordination>.Instance, options);
if (args[2] != "-")
    CoreEx.ExecutionContext.SetCurrent(new CoreEx.ExecutionContext { TenantId = args[2] });
var name = args[3];
var key = CoreEx.Entities.CompositeKey.Create(args[4]);
await using ISynchronizationLease? ownership = args[0] switch
{
    "lease" => await provider.TryAcquireAsync(name, key),
    "attempt" => (await provider.TryBeginAsync(name, key, "request")).Attempt,
    _ => throw new ArgumentException("Unsupported worker operation.", nameof(args))
};
Console.WriteLine(ownership is null ? "contended" : "acquired");
if (ownership is not null)
    await Task.Delay(Timeout.InfiniteTimeSpan);
