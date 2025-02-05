using Managment.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Managment.Services.Common
{
    public sealed class CheckingUpdateService : ICheckingUpdateService
    {

        #region Services

        private readonly ILogger<CheckingUpdateService> mLogger;
        private readonly IAppSettingsOptionsService mAppSettingsOptionsService;

        #endregion

        #region ~
        public CheckingUpdateService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<CheckingUpdateService>>();
            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();

            mLogger.LogInformation("Service run");
        }

        public void PrintCheckUrl()
        {
            mLogger.LogInformation("Check update urls is {urls}", mAppSettingsOptionsService.CheckUpdateUrl);
        }

        #endregion

    }
}
