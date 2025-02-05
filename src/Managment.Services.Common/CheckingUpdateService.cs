using Managment.Interface;
using Managment.Interface.CheckingUpdateServiceDependency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace Managment.Services.Common
{
    public sealed class CheckingUpdateService : ICheckingUpdateService
    {

        #region Services

        private readonly ILogger<CheckingUpdateService> mLogger;
        private readonly IAppSettingsOptionsService mAppSettingsOptionsService;

        #endregion

        #region Var


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

        public async void CheckUpdate()
        {
            using HttpClient client = new();

            try
            {
                string jsonResponse = await client.GetStringAsync(mAppSettingsOptionsService.CheckUpdateUrl);

                List<ServiceUrlModel> serviceUrls = JsonSerializer.Deserialize<List<ServiceUrlModel>>(jsonResponse);

                if (serviceUrls.Count == 0) 
                {
                    mLogger.LogWarning("service addresses were not found");
                    return;
                }

                foreach (ServiceUrlModel serviceUrl in serviceUrls)
                {
                    mLogger.LogInformation("Service url for service {name} is {url}", serviceUrl.ServiceName, serviceUrl.ServiceUrl);
                } 
            }
            catch (Exception ex) 
            {
                mLogger.LogError("Error message {error}", ex.Message);
            }
            
        }

        #endregion

    }
}
