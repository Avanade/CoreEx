# About

Infrastructure is built with [Pulumi](https://www.pulumi.com/).

The easiest way to deploy it is by using Pulumi account (Free), but it's not mandatory.

Prerequisites:

1. [Pulumi CLI](https://www.pulumi.com/docs/get-started/install/)
2. Azure CLI - logged in to Azure subscription with permissions to create service principals

> Note: Some corporate AAD restrict what can be done in AAD. Since this sample creates AAD User and Group, infrastructure needs to be created in AAD tenant that allows it.

## Pulumi with azure storage

Pulumi can be used without Pulumi Account, by using [Azure Storage as backend](https://www.techwatching.dev/posts/pulumi-azure-backend).

1. set the `AZURE_STORAGE_ACCOUNT` environment variable to specify the Azure storage account to use
1. set the `AZURE_STORAGE_KEY` or the `AZURE_STORAGE_SAS_TOKEN` environment variables to let Pulumi access the storage
1. create a container in the storage account
1. execute the following command `pulumi login azblob://<container-path>` where container-path is the path to a blob container in the storage account

## Configuring Pulumi (optional)

Infrastructure project has only few settings:

* `Company.AppName.Infra:isAppsDeploymentEnabled` for controlling application deployment via zip deploy
* `Company.AppName.Infra:isDBSchemaDeploymentEnabled` for publishing Database schema and data
* `Company.AppName.Infra:developerEmails` comma separated list of developer team emails that will get access to created resources

> When `isAppsDeploymentEnabled` flag is set, pulumi code executes `dotnet publish -c RELEASE` to create app packages.

Pulumi can be configured and previewed with:

```bash
pulumi preview -c azure-native:location=EastUs -c Company.AppName.Infra:isAppsDeploymentEnabled=true -c Company.AppName.Infra:isDBSchemaDeploymentEnabled=true
```

which creates a stack config file `Pulumi.dev.yaml`

```yaml
config:
  azure-native:location: EastUs
  Company.AppName.Infra:isAppsDeploymentEnabled: true
  Company.AppName.Infra:isDBSchemaDeploymentEnabled: true
  Company.AppName.Infra:developerEmails: "bob@mycustomad.onmicrosoft.com, alice@mycustomad.onmicrosoft.com"
```

### Note on best practices

Infrastructure project has built-in ability to deploy application code and database schema, in real-life scenarios those operations should be separated out. Code will most likely be deployed more often than infrastructure piece, with additional options for tagging, versioning etc.

It's also important to keep infrastructure project up to date and deploy it often. Pulumi state change analysis is quick and should not add a lot to deployment time.

## Infrastructure deployed

Pulumi creates full stack infrastructure designed for production. Resources deployed include:

* Storage account with RBAC enabled for app service and function app managed identities
* App Service Plan
* Log analytics workspace and Application Insights
* SQL Server with SQL Database enabled for Azure AD access with permissions setup for app service and function app managed identities
* Service Bus with queues and topics and permissions setup for app service and function app managed identities
* Function App and App Service
* Azure AD group for developer access

> **Considerations for enhancing security pasture**
>
> Networking stack can be enhanced with private networking capabilities - private endpoints, service endpoints. Tunneling traffic via API Management with WAF, cross region replication and more.

## Deploy with Pulumi

To deploy in `samples/Company.AppName/Company.AppName.Infra` run `pulumi up -c azure-native:location=EastUs -c Company.AppName.Infra:isAppsDeploymentEnabled=true -c Company.AppName.Infra:isDBSchemaDeploymentEnabled=true`

To display outputs of the stack deployment run: `pulumi stack output --show-secrets` which will display function links with secret api key.

## Alternative deployment methods

Apps can also be deployed with Azure CLI, once published apps are zipped.

```bash
az webapp deploy --resource-group coreEx-dev4011fb65 --name app17b7c4c8 --src-path app.zip
az functionapp deployment source config-zip -g coreEx-dev4011fb65 -n fun17b7c4c8 --src fun.zip
```

## Deploying from CI/CD with service principal

When deploying using service principal, SP needs to be given appropriate permissions to be allowed to create resources:

* Owner role on the subscription to be able to assign permissions to manage identities created (this can be achieved by creating/assigning custom role too).
* Azure AD create user role in order to create SQL Admin user and SQL access group.
* Microsoft Graph permissions according to [Terraform Docs](https://registry.terraform.io/providers/hashicorp/azuread/latest/docs/guides/service_principal_configuration) to be able to query domain, create/read users and create/read groups.
