// build CoreEx stack
using My.Hr.Infra.Services;

return await Pulumi.Deployment.RunAsync(() =>
{
    // create ans use actual instance of DB Operations service
    return CoreEx.Infra.CoreExStack.ExecuteStackAsync(new DbOperations());
}, null);
