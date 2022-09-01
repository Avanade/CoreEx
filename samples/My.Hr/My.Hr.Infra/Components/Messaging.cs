using System.Collections.Generic;
using Pulumi;
using Pulumi.AzureNative.Authorization;
using Pulumi.AzureNative.ServiceBus;
using AzureNative = Pulumi.AzureNative;

namespace CoreEx.Infra.Components;

public class Messaging : ComponentResource
{
    private readonly MessagingArgs args;
    private readonly string name;

    public Output<string> NamespaceName { get; } = default!;
    public Output<string> NamespaceId { get; } = default!;

    public Messaging(string name, MessagingArgs args, ComponentResourceOptions? options = null)
        : base("coreexinfra:web:messaging", name, options)
    {
        this.args = args;
        this.name = name;

        var @namespace = new Namespace(name, new NamespaceArgs
        {
            ResourceGroupName = args.ResourceGroupName,
            Sku = new AzureNative.ServiceBus.Inputs.SBSkuArgs
            {
                Name = SkuName.Standard,
                Tier = SkuTier.Standard,
            },
            Tags = args.Tags
        }, new CustomResourceOptions { Parent = this });

        NamespaceName = @namespace.Name;
        NamespaceId = @namespace.Id;

        RegisterOutputs();
    }

    public Queue AddQueue(string queueName, bool batchOperationsEnabled = false)
    {
        return new Queue($"{name}-queue-{queueName}", new()
        {
            EnablePartitioning = false,
            NamespaceName = NamespaceName,
            QueueName = queueName,
            ResourceGroupName = args.ResourceGroupName,
            MaxDeliveryCount = 3,
            EnableBatchedOperations = batchOperationsEnabled
        }, new CustomResourceOptions { Parent = this });
    }

    public IEnumerable<RoleAssignment> AddAccess(Input<string> principalId, string name)
    {
        var receive_permission = new RoleAssignment(
        $"receive-for-{name}",
            new RoleAssignmentArgs
            {
                Description = $"{name} receiving data from service bus",
                PrincipalId = principalId,
                PrincipalType = "ServicePrincipal",
                RoleDefinitionId = Roles.BuiltInRolesIds.ServiceBusDataReceiver,
                Scope = NamespaceId
            },
            new CustomResourceOptions { Parent = this }
        );

        var send_permission = new RoleAssignment(
        $"send-for-{name}",
            new RoleAssignmentArgs
            {
                Description = $"{name} sending data to service bus",
                PrincipalId = principalId,
                PrincipalType = "ServicePrincipal",

                // todo: fix hardcoded subscription
                RoleDefinitionId = Roles.BuiltInRolesIds.ServiceBusDataSender,
                Scope = NamespaceId
            },
            new CustomResourceOptions { Parent = this }
        );

        return new[] { receive_permission, send_permission };
    }

    public class MessagingArgs
    {
        public Input<string> ResourceGroupName { get; set; } = default!;
        public InputMap<string> Tags { get; set; } = default!;
    }
}