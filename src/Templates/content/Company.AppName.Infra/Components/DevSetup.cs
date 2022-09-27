using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Pulumi;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using AzureNative = Pulumi.AzureNative;

namespace Company.AppName.Infra.Components;

public class DevSetup : ComponentResource
{
    public DevSetup(string name, Input<string> developerEmails, ComponentResourceOptions? options = null)
      : base("coreexinfra:developer:setup", name, options)
    {
        //
    }
}