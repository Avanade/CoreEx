using System.Threading.Tasks;
using Pulumi;

namespace Company.AppName.Infra.Services;

public interface IDbOperations
{
    Task<int> DeployDbSchemaAsync(string connectionString);
    void ProvisionUsers(Input<string> connectionString, string groupName);
}