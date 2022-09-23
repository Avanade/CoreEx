using CoreEx.Database;
using CoreEx.Database.SqlServer;
using Microsoft.Data.SqlClient;

namespace Company.AppName.Business.Data
{
    public class AppName : SqlServerDatabase
    {
        public AppName(SettingsBase settings) : base(() => new SqlConnection(settings.GetRequiredValue<string>("ConnectionStrings:Database"))) { }
    }
}