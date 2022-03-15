// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Configuration;

namespace CoreEx.Configuration
{
    /// <summary>  Common setting used by shared classes. </summary>
    /// <remarks> Sublass it to add your own settings. </remarks>
    public class DeploymentInfo
    {
        private readonly IConfiguration _configuration;

        /// <summary> ctor </summary>
        public DeploymentInfo(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary> Who performed the deployment </summary>
        public virtual string By => _configuration.GetValue<string>("Deployment.By");

        /// <summary> Build that was deployed </summary>
        public virtual string Build => _configuration.GetValue<string>("Deployment.Build");

        /// <summary> Name of the deployment job that deployed the <see cref="Build"/> </summary>
        public virtual string Name => _configuration.GetValue<string>("Deployment.Name");

        /// <summary> Git information (branch and commit) of the deployed <see cref="Build"/> </summary>
        public virtual string Version => _configuration.GetValue<string>("Deployment.Version");

        /// <summary> Date and time Utc when deployment <see cref="Name"/> was executed </summary>
        public virtual string DateUtc => _configuration.GetValue<string>("Deployment.Date");
    }
}