using Managment.Interface;
using Managment.Interface.CheckingUpdateServiceDependency;
using Managment.Interface.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        private readonly string mRepositoryListPath = "jsonRepository";
        const string cUniqueUrlListFileName = "uniqueUpdateUrls.json";
        const string cUrlListFileName = "updateUrls.json";
        const string cUrlWithNameListFileName = "repositoryUrlWithName.json";

        #endregion

        #region Events

        public event UpdateUrlsListEventHandler RaiseUpdateUrlsListEvent;

        #endregion

        #region ~

        public CheckingUpdateService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<CheckingUpdateService>>();
            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();

            mRepositoryListPath = mAppSettingsOptionsService.RepositoryListDownloadPath;
            mLogger.LogInformation("Service run");
        }

        #endregion

        #region Public methods

        public async void DownloadRepositoriesList()
        {
            mLogger.LogInformation("Check update repository list");
            GitHubClient client = new(new ProductHeaderValue("vertigra.updater"));
            JsonRepository jsonRepository = new(mRepositoryListPath);

            int i = 0;
            foreach (var serviceRepository in mAppSettingsOptionsService.LocationsServiceRepositoryList) 
            {
                i++;
                mLogger.LogInformation("{Counter}. {RepositoriesName} {RepositoriesOwner} {ServicesListFilePath}", i, serviceRepository.RepositoriesName, serviceRepository.RepositoriesOwner, serviceRepository.ServicesListFilePath);
                byte[] fileContent = await client.Repository.Content.GetRawContent(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, serviceRepository.ServicesListFilePath);
                string serviceRepositoriesList = System.Text.Encoding.Default.GetString(fileContent);
                string fileName = $"{serviceRepository.RepositoriesOwner.ToLower()}.{serviceRepository.RepositoriesName.ToLower()}.repositories.json";
                bool isSaved = await jsonRepository.SaveJsonFilesAsync(serviceRepositoriesList, fileName);
                
                if (isSaved)
                {
                    mLogger.LogInformation("{filePath} saved!", $"{mRepositoryListPath}{Path.DirectorySeparatorChar}{fileName}");
                    return;
                }
                
                mLogger.LogError("{filePath} saved!", $"{mRepositoryListPath}Path.DirectorySeparatorChar{fileName}");
            }
        }

        /*public async Task CheckAndSaveUpdateListsAsync()
        {
            JsonRepository jsonRepository = new(mRepositoryPath);
            List<ServiceNameWithUrl> serviceNameWithUrl = [];


            try
            {
                jsonRepository.CreateOrClearRepositoryDirectory();

                var checkUpdateUrl = mAppSettingsOptionsService.CheckUpdateUrl.ConvertGitHubLinkToRaw();
                await jsonRepository.SaveJsonResponseAsync(cUrlListFileName, checkUpdateUrl);

                List<ServiceUrlModel> rawUpdateUrls = await jsonRepository.ReadJsonFileAsync<List<ServiceUrlModel>>(cUrlListFileName);
                List<ServiceUrlModel> convertUrls = RawUrlParser(rawUpdateUrls);
                List<ServiceUrlModel> uniquensUrls = CheckUniquensUrls(convertUrls);

                if (uniquensUrls.Count == 0)
                {
                    mLogger.LogWarning("Service addresses were not found");
                    return;
                }

                jsonRepository.SerializeAndSaveServiceUrls(uniquensUrls, cUniqueUrlListFileName);

                foreach (ServiceUrlModel url in uniquensUrls)
                {
                    var fileName = $"{url.ServiceUrl.ConvertToRepositoryName()}.json";
                    await jsonRepository.SaveJsonResponseAsync(fileName, url.ServiceUrl);
                    mLogger.LogInformation("Json for service name in config {name} with url {url} saved with name {fileName}", url.ServiceName, url.ServiceUrl, url.ServiceUrl.ConvertToRepositoryName());
                    
                    ServiceNameWithUrl nameWithUrl = new()
                    {
                        ServiceInfoJsonUrl = url.ServiceUrl.ConvertRawUrlToGitUrl(),
                        ServiceInfoServiceName = url.ServiceName
                    };

                    serviceNameWithUrl.Add(nameWithUrl);
                    jsonRepository.SerializeAndSaveServiceNameWithUrls(serviceNameWithUrl, cUrlWithNameListFileName);
                }
            }
            catch (Exception ex) 
            {
                mLogger.LogError("Error message {error}", ex.Message);
            }
            finally
            {
               
            }
        }*/

        public async Task<List<ServiceNameWithUrl>> ReadServiceNameWithUrlListAsync()
        {
            List<ServiceNameWithUrl> uniquensUrls = [];
            JsonRepository jsonRepository = new(mRepositoryListPath);

            try
            {
                uniquensUrls = await jsonRepository.ReadJsonFileAsync<List<ServiceNameWithUrl>>(cUrlWithNameListFileName);
                return uniquensUrls;
            }
            catch (Exception ex)
            {
                mLogger.LogError("{error}", ex.Message);
                return uniquensUrls;
            }
        }


        public async Task<List<ServiceInfoModel>> ReadServiceUpdateListsAsync()
        {
            List<ServiceInfoModel> serviceInfos = [];
            JsonRepository jsonRepository = new(mRepositoryListPath);
            
            try
            {
                List<ServiceUrlModel> uniquensUrls = await jsonRepository.ReadJsonFileAsync<List<ServiceUrlModel>>(cUniqueUrlListFileName);

                foreach (ServiceUrlModel url in uniquensUrls)
                {
                    ServiceInfoModel readJson = await jsonRepository.ReadJsonFileAsync<ServiceInfoModel>($"{url.ServiceUrl.ConvertToRepositoryName()}.json");
                    serviceInfos.Add(readJson);   
                }
            }
            catch (Exception ex)
            {
                mLogger.LogError("{error}", ex.Message);
            }
        
            return serviceInfos;
        }

        #endregion

        #region Public fields

        public List<ServiceUrlModel> UpdateUrls => [];
        
        #endregion

        #region Private methods

        private List<ServiceUrlModel> RawUrlParser(List<ServiceUrlModel> rawUpdateUrls)
        {
            List<ServiceUrlModel> updateUrls = [];

            foreach (ServiceUrlModel updateUrl in rawUpdateUrls.Where(x => !string.IsNullOrEmpty(x.ServiceUrl)))
            {
                string serviceName = updateUrl.ServiceName;
                string serviceUrl = string.Empty;

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

        private static List<ServiceUrlModel> CheckUniquensUrls(List<ServiceUrlModel> urls) 
        {
            return urls.GroupBy(model => model.ServiceUrl)
                       .Select(group => group.First())
                       .ToList();
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
