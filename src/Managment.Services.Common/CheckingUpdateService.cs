using Managment.Interface;
using Managment.Interface.CheckingUpdateServiceDependency;
using Managment.Interface.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Managment.Services.Common
{
    public class CheckingUpdateService : ICheckingUpdateService
    {
        #region Services

        private readonly ILogger<CheckingUpdateService> mLogger;
        private readonly IAppSettingsOptionsService mAppSettingsOptionsService;

        #endregion

        #region Var

        private readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        #endregion

        #region Events

        public event UpdateUrlsListEventHandler RaiseUpdateUrlsListEvent;

        #endregion

        #region ~

        public CheckingUpdateService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<CheckingUpdateService>>();
            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();

            mLogger.LogInformation("Service run");
        }

        #endregion

        #region Public methods

        public void PrintCheckUpdateUrl()
        {
            mLogger.LogInformation("Check update urls is {urls}", mAppSettingsOptionsService.CheckUpdateUrl.ConvertGitHubLinkToRaw());
        }

        public Task CheckUpdateAsync()
        {
            return Task.Run(async() => 
            {
                using HttpClient client = new();
                JsonSerializerOptions options = jsonSerializerOptions;

                try
                {
                    var checkUpdateUrl = mAppSettingsOptionsService.CheckUpdateUrl.ConvertGitHubLinkToRaw();
                    string jsonResponse = await client.GetStringAsync(checkUpdateUrl);

                    List<ServiceUrlModel> rawUpdateUrls = JsonSerializer.Deserialize<List<ServiceUrlModel>>(jsonResponse, options);

                    if (rawUpdateUrls.Count == 0)
                    {
                        mLogger.LogWarning("No repository addresses were found in the json source file located at the url {url} ", checkUpdateUrl);
                        return;
                    }

                    List<ServiceUrlModel> updateUrls = RawUrlParser(rawUpdateUrls);

                    if (updateUrls.Count == 0)
                    {
                        mLogger.LogWarning("After parsing the json located at the url {url} the repository addresses were not found.", checkUpdateUrl);
                        return;
                    }

                    UpdateUrls = updateUrls;

                    mLogger.LogInformation($"Findig url list:");

                    foreach (var url in updateUrls)
                    {
                        mLogger.LogInformation("{serviceName} {serviceUrl}", url.ServiceName, url.ServiceUrl);
                    }

                }
                catch (Exception ex)
                {
                    mLogger.LogError("Error message {error}", ex.Message);
                }
            });
        }

        #endregion

        #region Public fields

        private List<ServiceUrlModel> serviceUrls = [];

        public List<ServiceUrlModel> UpdateUrls 
        { 
            get => serviceUrls; 
            private set
            {
                if (value == null)
                    return;

                serviceUrls = value;
                OnRaiseUpdateUrlsListEvent();
            } 
        }

        #endregion

        #region Private methods

        private List<ServiceUrlModel> RawUrlParser(List<ServiceUrlModel> rawUpdateUrls)
        {
            List<ServiceUrlModel> updateUrls = [];

            foreach (ServiceUrlModel updateUrl in rawUpdateUrls.Where(x => !string.IsNullOrEmpty(x.ServiceUrl)))
            {
                var serviceName = updateUrl.ServiceName;
                var serviceUrl = string.Empty;

                try
                {
                    serviceUrl = updateUrl.ServiceUrl.ConvertGitHubLinkToRaw();
                }
                catch (ArgumentException ex)
                {
                    mLogger.LogWarning("Service url for service name {serviceName} in incorrect format. Try convert with default path arguments", serviceName);

                    if (ex.Message == "Invalid GitHubUserContent argument format")
                        mLogger.LogError("{error}", ex.Message);

                    if (ex.Message == "Invalid GitHub argument format")
                        serviceUrl = $"{updateUrl.ServiceUrl}/blob/master/service_info.json".ConvertGitHubLinkToRaw();

                    if (string.IsNullOrEmpty(serviceUrl))
                        mLogger.LogWarning("Convert with default path arguments for service name {serviceName} fails", serviceName);
                }

                if (!string.IsNullOrEmpty(serviceUrl))
                {
                    updateUrls.Add(new ServiceUrlModel 
                    { 
                        ServiceName = serviceName,  
                        ServiceUrl = serviceUrl 
                    });

                    mLogger.LogTrace("Service url for service {name} is {url} added to list", serviceName, serviceUrl);
                }
            }

            return updateUrls;
        }

        #endregion


        #region OnRaise events

        protected virtual  void OnRaiseUpdateUrlsListEvent()
        {
            UpdateUrlsListEventHandler raiseEvents = RaiseUpdateUrlsListEvent;
            raiseEvents?.Invoke(this);
        }

        #endregion
    }
}
