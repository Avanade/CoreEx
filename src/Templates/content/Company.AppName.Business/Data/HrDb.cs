using CoreEx.Database;
using CoreEx.Database.SqlServer;
using Microsoft.Data.SqlClient;

namespace Company.AppName.Business.Data
{
    public class HrDb : SqlServerDatabase
    {
        public HrDb(SettingsBase settings) : base(() => new SqlConnection(settings.GetRequiredValue<string>("ConnectionStrings:Database"))) { }
    }
}