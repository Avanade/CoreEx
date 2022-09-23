using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.Storage;

using AzureNative = Pulumi.AzureNative;

namespace Company.AppName.Infra.Components;

public class Storage : ComponentResource
{
    private readonly Output<string> id = default!;
    public Output<string> AccountName { get; private set; } = default!;
    public Output<string> DeploymentContainerName { get; private set; } = default!;
    public Output<string> ConnectionString { get; private set; } = default!;

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
        }, new CustomResourceOptions { Parent = this });

        var connectionString = GetConnectionString(args.ResourceGroupName, storageAccount.Name);

        AccountName = storageAccount.Name!;
        id = storageAccount.Id!;
        ConnectionString = connectionString;
        DeploymentContainerName = deploymentContainer.Name;

        RegisterOutputs();
    }

    private static Output<string> GetConnectionString(Input<string> resourceGroupNameInput, Input<string> accountNameInput)
    {
        return Output.Tuple(resourceGroupNameInput, accountNameInput)
             .Apply(async t =>
             {
                 var (resourceGroupName, accountName) = t;

                 var storageAccountKeys = await ListStorageAccountKeys.InvokeAsync(new ListStorageAccountKeysArgs
                 {
                     ResourceGroupName = resourceGroupName,
                     AccountName = accountName
                 });

                 // Retrieve the primary storage account key.
                 return $"DefaultEndpointsProtocol=https;AccountName={accountNameInput};AccountKey={storageAccountKeys.Keys.First().Value}";
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
                Scope = id
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