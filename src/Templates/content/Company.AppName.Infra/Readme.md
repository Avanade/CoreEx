# About

Infrastructure is built with [Pulumi](https://www.pulumi.com/).

The easiest way to deploy it is by using Pulumi account (Free), but it's not mandatory.

Prerequisites:

1. [Pulumi CLI](https://www.pulumi.com/docs/get-started/install/)
2. Azure CLI - logged in to Azure

## Pulumi with azure storage

Pulumi can be used without Pulumi Account, by using [Azure Storage as backend](https://www.techwatching.dev/posts/pulumi-azure-backend).

1. set the `AZURE_STORAGE_ACCOUNT` environment variable to specify the Azure storage account to use
1. set the `AZURE_STORAGE_KEY` or the `AZURE_STORAGE_SAS_TOKEN` environment variables to let Pulumi access the storage
1. execute the following command `pulumi login azblob://<container-path>` where container-path is the path to a blob container in the storage account

## Configuring Pulumi (optional)

Infrastructure project has only 2 settings:

* `Company.AppName.Infra:isAppsDeploymentEnabled` for controlling application deployment via zip deploy
* `Company.AppName.Infra:isDBSchemaDeploymentEnabled` for publishing Database schema and data

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
```

## Deploy with Pulumi

To deploy in `samples/Company.AppName/Company.AppName.Infra` run `pulumi up -c azure-native:location=EastUs -c Company.AppName.Infra:isAppsDeploymentEnabled=true -c Company.AppName.Infra:isDBSchemaDeploymentEnabled=true`

To display outputs of the stack deployment run: `pulumi stack output --show-secrets` which will display function links with secret api key.

## Alternative deployment methods

Apps can also be deployed with Azure CLI, once published apps are zipped.

```bash
az webapp deploy --resource-group coreEx-dev4011fb65 --name app17b7c4c8 --src-path app.zip
az functionapp deployment source config-zip -g coreEx-dev4011fb65 -n fun17b7c4c8 --src fun.zip
```
