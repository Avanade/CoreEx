// build CoreEx stack
// todo - use ServiceProvider for dependency injection?
// return await Pulumi.Deployment.RunAsync<CoreEx.Infra.CoreExStack>();

return await Pulumi.Deployment.RunAsync(() => new CoreEx.Infra.CoreExStack().ExecuteStackAsync(), null);
