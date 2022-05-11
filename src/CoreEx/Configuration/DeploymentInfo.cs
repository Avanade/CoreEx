// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Configuration;

namespace CoreEx.Configuration
{
    /// <summary>
    /// Provides the common deployment information setting. 
    /// </summary>
    /// <remarks>This class should be inherited to add additional properties where required.</remarks>
    public class DeploymentInfo
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeploymentInfo"/> class.
        /// </summary>
        public DeploymentInfo(IConfiguration configuration) => _configuration = configuration;

        /// <summary>
        /// Gets the username who performed the deployment.
        /// </summary>
        public virtual string By => _configuration.GetValue<string>("Deployment.By");

        /// <summary>
        /// Gets the deployment build number.
        /// </summary>
        public virtual string Build => _configuration.GetValue<string>("Deployment.Build");

        /// <summary>
        /// Gets the name of the deployment job that deployed the <see cref="Build"/>.
        /// </summary>
        public virtual string Name => _configuration.GetValue<string>("Deployment.Name");

        /// <summary> 
        /// Gets the deployment build version, such as the Git information (branch and commit) of the deployed <see cref="Build"/>.
        /// </summary>
        public virtual string Version => _configuration.GetValue<string>("Deployment.Version");

        /// <summary>
        /// Gets the date and time (UTC) when deployment <see cref="Name"/> was performed.
        /// </summary>
        public virtual string DateUtc => _configuration.GetValue<string>("Deployment.Date");
    }
}