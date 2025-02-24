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
    public class UpdateService : IUpdateService
    {
        #region Services

        private readonly ILogger<UpdateService> mLogger;
        private readonly IAppSettingsOptionsService mAppSettingsOptionsService;

        #endregion

        #region Var

        private readonly string mRepositoryListPath = "jsonRepository";
        private readonly GitHubClient mGitHubClient;
        private readonly JsonRepository mJsonRepository;
        private readonly List<ServiceRepositoryModel> mServiceRepositories;


        #endregion

        #region Const

        private const string cProductHeaderValue = "Adam.Services.Managment";

        #endregion

        #region Events

        public event UpdateUrlsListEventHandler RaiseUpdateUrlsListEvent;

        #endregion

        #region ~

        public UpdateService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<UpdateService>>();
            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();

            mRepositoryListPath = mAppSettingsOptionsService.RepositoryListDownloadPath;
            mGitHubClient = new(new ProductHeaderValue(cProductHeaderValue));
            mJsonRepository = new(mRepositoryListPath);

            mServiceRepositories = [];
            mLogger.LogInformation("Service started");
        }

        #endregion

        #region Public methods

        public async Task DownloadRepositoriesListAsync()
        {
            mLogger.LogInformation("=== Step 1. Download Repositories List ===");
            
            int i = 0;
            List<ServiceRepositoryModel> tempRepositoryList = new(mAppSettingsOptionsService.LocationsServiceRepositoryList);

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
                    await mJsonRepository.SaveRawJsonFilesAsync(serviceRepositoriesList, fileName);
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

            mLogger.LogInformation("=== Step 1. Download Repositories Finished ===");
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
                            mServiceRepositories.Add(repository);
                        }
                    }
                }
            }

            mLogger.LogInformation("=== Step 2. Check Repositories Info Files Finished ===");
        }

        public async Task DownloadRepositoriesInfoAsync()
        {
            mLogger.LogInformation("=== Step 3. Download Service Info Files ===");

            List<string> serviceInfoFiles = [];
            int i = 0;

            foreach (ServiceRepositoryModel serviceRepository in mServiceRepositories)
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
                    await mJsonRepository.SaveRawJsonFilesAsync(serviceRepositoriesList, fileName);

                }
                catch (Exception ex) 
                {
                    savedWithException = true;
                    mLogger.LogError("{exception}", ex);
                }
                finally
                {
                    if (!savedWithException)
                    {
                        mLogger.LogInformation("{сounter}. Download service info file and save as {filename} ", i, fileName);
                        serviceInfoFiles.Add(fileName);
                    }
                }

                if (serviceInfoFiles.Count > 0)
                {
                    await mJsonRepository.SerializeAndSaveJsonFilesAsync(serviceInfoFiles, "service.info.files.json");
                    mLogger.LogInformation("{сounter}. Download service info file {filename} added to download list {downloadList}", i, fileName, "service.info.files.json");
                }
            }

            mLogger.LogInformation("=== Step 3. Download Service Info Files Finished ===");
        }

        #endregion

        #region Public fields

        

        #endregion

        #region Private methods

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
