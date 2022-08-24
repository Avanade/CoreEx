using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using CoreEx.Configuration;
using HealthChecks.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreEx.Azure.HealthChecks
{

    /// <summary> Sql Server Health Check. </summary>
    public class SqlHealthCheck : IHealthCheck
    {
        private const string HEALTH_QUERY = "SELECT 1;";
        private readonly string _sqlConnectionString;
        private readonly string _connectionName;
        private IHealthCheck? _innerHealthCheck;
        private IReadOnlyDictionary<string, object>? _data;

        /// <summary> constructor. </summary>
        /// <remarks> Note that constructor takes setting NAMES not values, values are looked up from <paramref name="settings"/>. </remarks>
        public SqlHealthCheck(SettingsBase settings, string connectionName)
        {
            _sqlConnectionString = settings.GetValue<string>(connectionName);
            _connectionName = connectionName;
        }

        /// <summary> constructor. </summary>
        /// <remarks> Note that constructor takes setting NAMES not values, values are looked up from <paramref name="settings"/>. </remarks>
        public SqlHealthCheck(SettingsBase settings, string connectionName, IHealthCheck sqlCheck)
        {
            _sqlConnectionString = settings.GetValue<string>(connectionName);
            _connectionName = connectionName;
            _innerHealthCheck = sqlCheck;
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_sqlConnectionString))
            {
                return HealthCheckResult.Unhealthy($"Sql Server connection is not configured under {_connectionName} in settings");
            }

            if (_data == null)
            {
                try
                {
                    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_sqlConnectionString);
                    _data = new Dictionary<string, object>
                    {
                        { "server", builder.DataSource },
                        { "database", builder.InitialCatalog },
                        { "timeout", builder.ConnectTimeout }
                    };
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"Sql Server connection could not be parsed. Check the value under {_connectionName} in settings", ex);
                }
            }

            string? accessToken = null;

            if (_sqlConnectionString.Contains("Password") || _sqlConnectionString.Contains("PWD"))
            {
                // SQL connection is using SQL Auth
            }
            else
            {
                accessToken = await GetAccessToken();
                // SQL connection is using Integrated AD Auth with token
            }

            _innerHealthCheck ??= new SqlServerHealthCheck(_sqlConnectionString, HEALTH_QUERY, beforeOpenConnectionConfigurer: (connection) =>
            {
                if (accessToken != null)
                {
                    connection.AccessToken = accessToken;
                    // SQL connection is using Integrated AD Auth with token
                }
            });

            try
            {
                var result = await _innerHealthCheck.CheckHealthAsync(context, cancellationToken);
                return new HealthCheckResult(result.Status, result.Description, result.Exception, data: _data);
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex, data: _data);
            }
        }

        private static async Task<string> GetAccessToken()
        {
            var tokenCredential = new DefaultAzureCredential(includeInteractiveCredentials: true);
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(scopes: new string[] { "https://database.windows.net/.default" }) { }
            );

            return accessToken.Token;
        }
    }
}