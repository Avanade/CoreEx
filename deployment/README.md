## Introduction


CoreEx leverages [Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/) to provision resources in Azure. Following are the steps to be performed when deploying CoreEx sample app to Azure.

## Prequisites

 - [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows?tabs=azure-cli)
  
 - [Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/install)

 - [.NET CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/)

 - [Powershell](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/powershell) 

## Deploy Azure resources
 - Open the source code in VSCode. Start a terminal and navigate to the ***deployment*** folder.

 - Sign in to your Azure account from the Visual Studio Code terminal.
~~~script
 az login
~~~

 - Set the default subscription
~~~script
 az account set --subscription {your-subscription-ID}
~~~
 - Create resource group
~~~script
 az group create --name {resource-group-name} --location {location-name}
~~~
 - Set default resource group
~~~script
 az configure --defaults group={resource-group-name}
~~~
 - Deploy bicep template
~~~script
az deployment group create --template-file main.bicep 
~~~
 - Verify deployment
~~~script
az deployment group list --output table
~~~

***NOTE:*** You will be prompted for SQL Admin Login and **complex** Password. If you run into any error, delete the resource-group and start again.

## Deploy App Code

 - Create Publish artifacts
~~~script
Remove-Item -Recurse -Force "\artifacts" -ErrorAction SilentlyContinue

dotnet publish "..\samples\My.Hr\My.Hr.Api\My.Hr.Api.csproj" -c Release --output "\artifacts"

compress-Archive -Path "\artifacts\*" -DestinationPath "MyHrApp.zip" -Force
~~~

 - Publish Web API
~~~script
az webapp deployment source config-zip --name {App-Service-Name} --src "MyHrApp.zip"
~~~