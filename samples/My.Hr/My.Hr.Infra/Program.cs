// build CoreEx stack
using My.Hr.Infra.Services;
using Pulumi;

return await Deployment.RunAsync(() =>
{
    var client = new System.Net.Http.HttpClient();
    // create and use actual instance of DB Operations service
    return My.Hr.Infra.CoreExStack.ExecuteStackAsync(new DbOperations(), client);
}, new StackOptions
{
    // apply auto-tagging transformation
    // https://gist.github.com/dbeattie71/1f8a1a9264ceb8161ad4c49de1ee3bb3
    ResourceTransformations = new System.Collections.Generic.List<ResourceTransformation>{
        (args) => {
            var tagsProp = args.Args.GetType().GetProperty("Tags");

            if(tagsProp?.GetValue(args.Args) is InputMap<string> tags)
            {
                Log.Debug("Adding tags to " + args.Resource.GetResourceName());

                tags.Add("user:Stack", Deployment.Instance.StackName);
                tags.Add("user:Project", Deployment.Instance.ProjectName);
                tags.Add("App:Name", "CoreEx");
            }

            return new ResourceTransformationResult(args.Args, args.Options);
        }
    }
});
