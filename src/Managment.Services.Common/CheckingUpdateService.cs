using Managment.Interface;
using Managment.Interface.AppSettingsOptionsServiceDependency;
using Managment.Interface.CheckingUpdateServiceDependency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly GitHubClient mGitHubClient;
        private readonly JsonRepository mJsonRepository;

        #endregion

        #region const

        private const string cProductHeaderValue = "Adam.Services.Managment";

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
            mGitHubClient = new(new ProductHeaderValue(cProductHeaderValue));
            mJsonRepository = new(mRepositoryListPath);

            ServiceRepositories = [];
            mLogger.LogInformation("Service run");
        }

        #endregion

        #region Public methods

        public async Task DownloadRepositoriesListAsync()
        {
            mLogger.LogInformation("=== Step 1. Download Repositories List ===");
            
            int i = 0;

            var tempRepositoryList = new List<ServiceRepositoryModel>(mAppSettingsOptionsService.LocationsServiceRepositoryList);
            foreach (ServiceRepositoryModel serviceRepository in tempRepositoryList)
            {
                i++;
                bool savedWithException = false;
                mLogger.LogInformation("{Counter}. Download repositories list from RepositoriesName:{RepositoriesName} RepositoriesOwner:{RepositoriesOwner} FileName:{ServicesListFilePath}", i, serviceRepository.RepositoriesName, serviceRepository.RepositoriesOwner, serviceRepository.ServicesListFilePath);
                string fileName = $"{serviceRepository.RepositoriesOwner.ToLower()}.{serviceRepository.RepositoriesName.ToLower()}.repositories.json";
     
                try
                {
                    byte[] fileContent = await mGitHubClient.Repository.Content.GetRawContent(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, serviceRepository.ServicesListFilePath);
                    string serviceRepositoriesList = System.Text.Encoding.Default.GetString(fileContent);    
                    await mJsonRepository.SaveJsonFilesAsync(serviceRepositoriesList, fileName);
                }
                catch (NotFoundException) 
                {
                    
                    mLogger.LogError("{counter}. The file or repository not found and removed from repositories list", i);
                    mAppSettingsOptionsService.LocationsServiceRepositoryList.Remove(serviceRepository);

                    savedWithException = true;
                }
                catch (Exception ex)
                {
                    mLogger.LogError("{counter}. {exception}", i, ex);
                    mAppSettingsOptionsService.LocationsServiceRepositoryList.Remove(serviceRepository);

                    savedWithException = true;
                }
                finally
                {
                    if (!savedWithException)
                        mLogger.LogInformation("{counter}. {filePath} saved!", i, $"{mRepositoryListPath}{Path.DirectorySeparatorChar}{fileName}");
                }   
            }
        }

        public async Task CheckRepositoriesListAsync()
        {
            mLogger.LogInformation("=== Step 2. Check Repositories Info Files ===");
            
            foreach (ServiceRepositoryModel serviceRepository in mAppSettingsOptionsService.LocationsServiceRepositoryList)
            {
                string fileName = $"{serviceRepository.RepositoriesOwner.ToLower()}.{serviceRepository.RepositoriesName.ToLower()}.repositories.json";
                List<ServiceRepositoryModel> repositories = [];
                bool readWithException = false;

                try
                {
                    repositories = await mJsonRepository.ReadJsonFileAsync<List<ServiceRepositoryModel>>(fileName);
                }
                catch (FileNotFoundException)
                {
                    mLogger.LogError("The file {filename} was not found", fileName);
                    readWithException = true;
                }
                catch (Exception ex)
                {
                    mLogger.LogError("{exception}", ex);
                    readWithException = true;
                }
                finally
                {
                    if (!readWithException)
                    {
                        foreach (var repository in repositories)
                        {
                            mLogger.LogTrace("RepositoriesName: {RepositoriesName} RepositoriesOwner: {RepositoriesOwner} read and added to download list", repository.RepositoriesName, repository.RepositoriesOwner);
                            ServiceRepositories.Add(repository);
                        }
                    }
                }
            }
        }

        public async Task DownloadRepositoriesInfoAsync()
        {
            mLogger.LogInformation("=== Step 3. Download Service Info Files ===");

            int i = 0;
            foreach (ServiceRepositoryModel serviceRepository in ServiceRepositories)
            {
                i++;

                bool savedWithException = false;
                string filePath = "service_info.json";

                if (!string.IsNullOrEmpty(serviceRepository.ServicesListFilePath))
                    filePath = serviceRepository.ServicesListFilePath;

                mLogger.LogInformation("{Counter}. Start download service info file from RepositoriesName:{RepositoriesName} RepositoriesOwner:{RepositoriesOwner} FileName:{ServicesListFilePath}", i, serviceRepository.RepositoriesName, serviceRepository.RepositoriesOwner, filePath);
                string fileName = $"{serviceRepository.RepositoriesOwner.ToLower()}.{serviceRepository.RepositoriesName.ToLower()}.service.info.json";
                try
                {
                    byte[] fileContent = await mGitHubClient.Repository.Content.GetRawContent(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, filePath);
                    string serviceRepositoriesList = System.Text.Encoding.Default.GetString(fileContent);
                    await mJsonRepository.SaveJsonFilesAsync(serviceRepositoriesList, fileName);

                }
                catch (Exception ex) 
                {
                    savedWithException = true;
                    mLogger.LogError("{exception}", ex);
                }
                finally
                {
                    if (!savedWithException)
                        mLogger.LogInformation("{сounter}. Download service info file and save as {filename} ", i, fileName);
                }
            }
        }

        #endregion

        #region Public fields

        public List<ServiceRepositoryModel> ServiceRepositories { get;  set; } 

        //public List<ServiceUrlModel> UpdateUrls => [];

        #endregion

        #region Private methods

        /*private List<ServiceUrlModel> RawUrlParser(List<ServiceUrlModel> rawUpdateUrls)
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
         }*/

        /*private static List<ServiceUrlModel> CheckUniquensUrls(List<ServiceUrlModel> urls) 
        {
            return urls.GroupBy(model => model.ServiceUrl)
                       .Select(group => group.First())
                       .ToList();
        }*/

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
