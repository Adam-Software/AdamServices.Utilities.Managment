using Managment.Interface;
using Managment.Interface.Common;
using Managment.Interface.Common.JsonModels;
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
        private readonly IJsonRepositoryService mJsonRepositoryService;

        #endregion

        #region Var

        private readonly GitHubClient mGitHubClient;
        
        private readonly List<ServiceRepositoryModel> mSettingsServiceRepositories;
        private readonly List<ServiceRepositoryModel> mServiceRepositories;
        private readonly string mDownloadInfoFilesNamePath;
        private readonly string mDownloadRepositoriesFilesNamePath;
        private readonly string mDownloadPath;

        #endregion

        #region Events

        public event DownloadAndCheckUpdateStartedEventHandler RaiseDownloadAndCheckUpdateStartedEvent;
        public event DownloadAndCheckUpdateFinishedEventHandler RaiseDownloadAndCheckUpdateFinishedEvent;

        #endregion

        #region ~

        public UpdateService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<UpdateService>>();
            mJsonRepositoryService = serviceProvider.GetRequiredService<IJsonRepositoryService>();
            IAppSettingsOptionsService appSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();


            IGitHubCilentService gitHubClientService = serviceProvider.GetRequiredService<IGitHubCilentService>();
            mGitHubClient = gitHubClientService.GitHubClient;

            mSettingsServiceRepositories = new List<ServiceRepositoryModel>(appSettingsOptionsService.UpdateServiceSettings.ServicesRepositories);
            mServiceRepositories = [];

            mDownloadPath = appSettingsOptionsService.UpdateServiceSettings.RepositoriesDownloadPath;
            mDownloadInfoFilesNamePath = appSettingsOptionsService.UpdateServiceSettings.DownloadInfoFilesNamePath;
            mDownloadRepositoriesFilesNamePath = appSettingsOptionsService.UpdateServiceSettings.DownloadRepositoriesFilesNamePath;

            mLogger.LogInformation("=== UpdateService. Start ===");
        }

        #endregion

        #region Public methods

        public async Task DownloadAndCheckUpdateInfoFiles()
        {
            OnRaiseDownloadAndCheckUpdateStartedEvent();

            DirectoryUtilites.CreateOrClearDirectory(mDownloadPath);

            await DownloadRepositoriesListAsync();
            await CheckAndSaveRepositoriesListAsync();
            await DownloadRepositoriesInfoAsync();

            OnRaiseDownloadAndCheckUpdateFinishedEvent();
        }

        #endregion

        #region Private methods

        private async Task DownloadRepositoriesListAsync()
        {
            mLogger.LogInformation("=== Step 1. Download Repositories List ===");
            
            int i = 0;
            List<ServiceRepositoryModel> tempServiceRepositories = new(mSettingsServiceRepositories);

            foreach (ServiceRepositoryModel serviceRepository in tempServiceRepositories)
            {
                i++;
                bool savedWithException = false;
                mLogger.LogInformation("{counter}. Download repositories list from RepositoriesName:{RepositoriesName} RepositoriesOwner:{RepositoriesOwner} FileName:{ServicesListFilePath}", i, serviceRepository.RepositoriesName, serviceRepository.RepositoriesOwner, serviceRepository.ServicesListFilePath);
                string fileName = $"{serviceRepository.RepositoriesOwner.ToLower()}.{serviceRepository.RepositoriesName.ToLower()}.repositories.json";
     
                try
                {
                    byte[] fileContent = await mGitHubClient.Repository.Content.GetRawContent(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, serviceRepository.ServicesListFilePath);
                    string serviceRepositoriesList = System.Text.Encoding.Default.GetString(fileContent);    
                    await mJsonRepositoryService.SaveRawJsonFilesAsync(serviceRepositoriesList, mDownloadPath, fileName);
                }
                catch (NotFoundException) 
                {
                    
                    mLogger.LogError("{counter}. The file or repository not found and removed from repositories list", i);
                    mSettingsServiceRepositories.Remove(serviceRepository);

                    savedWithException = true;
                }
                catch (Exception ex)
                {
                    mLogger.LogError("{counter}. {exception}", i, ex);
                    mSettingsServiceRepositories.Remove(serviceRepository);

                    savedWithException = true;
                }
                finally
                {
                    if (!savedWithException)
                        mLogger.LogInformation("{counter}. {filePath} saved!", i, $"{mDownloadPath}{Path.DirectorySeparatorChar}{fileName}");
                }   
            }

            mLogger.LogInformation("=== Step 1. Download Repositories Finished ===");
        }

        private async Task CheckAndSaveRepositoriesListAsync()
        {
            mLogger.LogInformation("=== Step 2. Check Repositories Info Files ===");

            List<string> repositoryInfoFilesName = [];

            foreach (ServiceRepositoryModel serviceRepository in mSettingsServiceRepositories)
            {
                string fileName = $"{serviceRepository.RepositoriesOwner.ToLower()}.{serviceRepository.RepositoriesName.ToLower()}.repositories.json";
                
                List<ServiceRepositoryModel> repositories = [];
                bool readWithException = false;

                try
                {
                    repositories = await mJsonRepositoryService.ReadJsonFileAsync<List<ServiceRepositoryModel>>(mDownloadPath, fileName);
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
                        foreach (ServiceRepositoryModel repository in repositories)
                        {
                            mLogger.LogInformation("RepositoriesName: {RepositoriesName} RepositoriesOwner: {RepositoriesOwner} read and added to download list", repository.RepositoriesName, repository.RepositoriesOwner);
                            mServiceRepositories.Add(repository);    
                        }

                        repositoryInfoFilesName.Add(fileName);
                    }
                }
            }

            if (repositoryInfoFilesName.Count > 0) 
            {
                await mJsonRepositoryService.SerializeAndSaveJsonFilesAsync(repositoryInfoFilesName, mDownloadPath, mDownloadRepositoriesFilesNamePath);
                mLogger.LogInformation("Create repeository file name {FilesNamePath}", mDownloadRepositoriesFilesNamePath);
            }

            mLogger.LogInformation("=== Step 2. Check Repositories Info Files Finished ===");
        }

        private async Task DownloadRepositoriesInfoAsync()
        {
            mLogger.LogInformation("=== Step 3. Download Service Info Files ===");

            List<string> serviceInfoFilesName = [];
            int i = 0;

            foreach (ServiceRepositoryModel serviceRepository in mServiceRepositories)
            {
                i++;

                bool savedWithException = false;
                string filePath = "service_info.json";

                if (!string.IsNullOrEmpty(serviceRepository.ServicesListFilePath))
                    filePath = serviceRepository.ServicesListFilePath;

                mLogger.LogInformation("{counter}. Start download service info file from RepositoriesName:{RepositoriesName} RepositoriesOwner:{RepositoriesOwner} FileName:{ServicesListFilePath}", i, serviceRepository.RepositoriesName, serviceRepository.RepositoriesOwner, filePath);
                string fileName = $"{serviceRepository.RepositoriesOwner.ToLower()}.{serviceRepository.RepositoriesName.ToLower()}.info.json";
                
                try
                {
                    byte[] fileContent = await mGitHubClient.Repository.Content.GetRawContent(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, filePath);
                    string serviceRepositoriesList = System.Text.Encoding.Default.GetString(fileContent);
                    await mJsonRepositoryService.SaveRawJsonFilesAsync(serviceRepositoriesList, mDownloadPath, fileName);
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
                        serviceInfoFilesName.Add(fileName);
                    }
                }    
            }

            if (serviceInfoFilesName.Count > 0)
            {
                await mJsonRepositoryService.SerializeAndSaveJsonFilesAsync(serviceInfoFilesName, mDownloadPath, mDownloadInfoFilesNamePath);
                mLogger.LogInformation("Create service info file name {FilesNamePath}", mDownloadInfoFilesNamePath);
            }

            mLogger.LogInformation("=== Step 3. Download Service Info Files Finished ===");
        }

        public void Dispose()
        {
            mLogger.LogInformation("=== UpdateService. Dispose ===");
            DirectoryUtilites.CreateOrClearDirectory(mDownloadPath);
            mServiceRepositories.Clear();
            mSettingsServiceRepositories.Clear();
        }

        #endregion

        #region OnRaise events

        protected virtual  void OnRaiseDownloadAndCheckUpdateStartedEvent()
        {
            DownloadAndCheckUpdateStartedEventHandler raiseEvents = RaiseDownloadAndCheckUpdateStartedEvent;
            raiseEvents?.Invoke(this);
        }

        protected virtual void OnRaiseDownloadAndCheckUpdateFinishedEvent()
        {
            DownloadAndCheckUpdateFinishedEventHandler raiseEvents = RaiseDownloadAndCheckUpdateFinishedEvent;
            raiseEvents?.Invoke(this);
        }

        #endregion
    }
}
