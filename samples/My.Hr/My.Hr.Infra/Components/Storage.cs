using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.Storage;

using AzureNative = Pulumi.AzureNative;

namespace CoreEx.Infra.Components;

public class Storage : ComponentResource
{
    public Output<string> Id { get; } = default!;
    public Output<string> AccountName { get; } = default!;
    public Output<string> ConnectionString { get; } = default!;
    public Output<string> DeploymentContainerName { get; } = default!;

    public Storage(string name, StorageArgs args, ComponentResourceOptions? options = null)
        : base("coreexinfra:web:storage", name, options)
    {
        // Create an Azure resource (Storage Account)
        var storageAccount = new StorageAccount(name, new StorageAccountArgs
        {
            ResourceGroupName = args.ResourceGroupName,
            Sku = new AzureNative.Storage.Inputs.SkuArgs
            {
                Name = SkuName.Standard_LRS
            },
            Kind = Kind.StorageV2,
            AllowBlobPublicAccess = false,
            EnableHttpsTrafficOnly = true,
            MinimumTlsVersion = "TLS1_2",
            Tags = args.Tags
        }, new CustomResourceOptions { Parent = this });

        var deploymentContainer = new BlobContainer("zips-container", new BlobContainerArgs
        {
            AccountName = storageAccount.Name,
            PublicAccess = PublicAccess.None,
            ResourceGroupName = args.ResourceGroupName,
        });

        var connectionString = GetConnectionString(args.ResourceGroupName, storageAccount.Name);

        AccountName = storageAccount.Name;
        Id = storageAccount.Id;
        ConnectionString = connectionString;
        DeploymentContainerName = deploymentContainer.Name;

        RegisterOutputs();
    }

    static Output<string> GetConnectionString(Input<string> resourceGroupName, Input<string> accountName)
    {
        // Retrieve the primary storage account key.
        var storageAccountKeys = ListStorageAccountKeys.Invoke(new ListStorageAccountKeysInvokeArgs
        {
            ResourceGroupName = resourceGroupName,
            AccountName = accountName
        });

        return storageAccountKeys.Apply(keys =>
        {
            var primaryStorageKey = keys.Keys[0].Value;

            // Build the connection string to the storage account.
            return Output.Format($"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={primaryStorageKey}");
        });
    }

    public RoleAssignment AddAccess(Input<string> principalId, string name)
    {

        return new RoleAssignment(
        $"useblob-for-{name}",
            new RoleAssignmentArgs
            {
                Description = $"{name} accessing storage account",
                PrincipalId = principalId,
                PrincipalType = "ServicePrincipal",
                RoleDefinitionId = Roles.BuiltInRolesIds.StorageBlobDataOwner,
                Scope = Id
            },
            new CustomResourceOptions { Parent = this }
        );
    }

    public class StorageArgs
    {
        public Input<string> ResourceGroupName { get; set; } = default!;
        public InputMap<string> Tags { get; set; } = default!;
    }
}