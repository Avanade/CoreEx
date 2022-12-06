## Introduction

IaC uses [Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/) to provision CoreEx (myHr) app. 

## Prequisites

 - [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows?tabs=azure-cli)
  
 - [Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/install)


## Deploy the Azure resources and App.
 - Open the source code in VSCode. Start a terminal and navigate to the 'deployment' folder.

 - Sign in to your Azure account from the Visual Studio Code terminal.
~~~script
 az login
~~~

 - Set the default subscription
~~~script
 az account set --subscription {your subscription ID}
~~~
 - Create resource group
~~~script
 az group create --name {resource-group-name} --location {location-name}
~~~
 - Set Default Resource group
~~~script
 az configure --defaults group={resource-group-name}
~~~
 - Deploy the bicep template
~~~script
az deployment group create --template-file main.bicep 
~~~

Verify that deployment succeeds!

***NOTE: You will be prompted for SQL Admin Login and complex Password. If you run into any error, delete the resource-group and start again.***