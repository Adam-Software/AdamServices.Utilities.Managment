using Managment.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using System;

namespace Managment.Services.Common
{
    public class GitHubCilentService : IGitHubCilentService
    {
        #region Services

        private readonly ILogger<UpdateService> mLogger;

        #endregion

        #region ~

        public GitHubCilentService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<UpdateService>>();
            IAppSettingsOptionsService appSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();

            string gitHubUserAgentHeader = appSettingsOptionsService.GitHubClientServiceSettings.GitHubUserAgentHeader;
            mGitHubClient = new(new ProductHeaderValue(gitHubUserAgentHeader));

            mLogger.LogInformation("=== GitHubCilentService. Start ===");
        }

        #endregion

        #region Public fields

        private readonly GitHubClient mGitHubClient;
        public GitHubClient GitHubClient => mGitHubClient;

        #endregion
    }
}
