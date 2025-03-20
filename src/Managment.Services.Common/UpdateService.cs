using Managment.Interface;
using Managment.Interface.Common;
using Managment.Interface.Common.JsonModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Managment.Services.Common
{
    public class UpdateService : IUpdateService
    {
        #region Events

        public event CheckUpdateStartedEventHandler RaiseCheckUpdateStartedEvent;
        public event CheckUpdateFinishedEventHandler RaiseCheckUpdateFinishedEvent;

        #endregion

        #region Services

        private readonly ILogger<UpdateService> mLogger;

        #endregion

        #region Var

        private readonly GitHubClient mGitHubClient;
        
        private readonly List<ServiceModelBase> mSettingsServiceRepositories;
        private readonly List<ServiceModelBase> mServiceRepositories;
        private readonly string mDownloadPath = "download";
        private readonly string mPublishPath = "publish";

        private ServiceRepositories mTempModel;

        #endregion

        #region ~

        public UpdateService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<UpdateService>>();

            IAppSettingsOptionsService appSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();
            IGitHubCilentService gitHubClientService = serviceProvider.GetRequiredService<IGitHubCilentService>();

            mGitHubClient = gitHubClientService.GitHubClient;

            mSettingsServiceRepositories = new List<ServiceModelBase>(appSettingsOptionsService.UpdateServiceSettings.ServicesRepositories);
            mServiceRepositories = [];

            mDownloadPath = appSettingsOptionsService.UpdateServiceSettings.RepositoriesDownloadPath;
            mPublishPath = appSettingsOptionsService.DotnetServiceSettings.PublishPath;

            mLogger.LogInformation("=== UpdateService. Start ===");
        }

        #endregion

        #region Public methods

        public async Task DownloadUpdateInfoFiles()
        {
            OnRaiseCheckUpdateStartedEvent();

            DirectoryUtilites.CreateOrClearDirectory(mDownloadPath);

            await DownloadRepositoriesListAsync();
            //await CheckAndSaveRepositoriesListAsync();
            await DownloadRepositoriesInfoAsync();

            OnRaiseCheckUpdateFinishedEvent();
        }

        public async Task CheckUpdatesForInstalledProject()
        {
            //await DownloadUpdateInfoFiles();
            //await EqualInstalledAndDownloadVersion();

        }

        public void Dispose()
        {
            mLogger.LogInformation("=== UpdateService. Dispose ===");
            //DirectoryUtilites.CreateOrClearDirectory(mDownloadPath);
            mServiceRepositories.Clear();
            mSettingsServiceRepositories.Clear();
        }

        #endregion

        #region Private methods

        private async Task DownloadRepositoriesListAsync()
        {
            mLogger.LogInformation("=== Step 1. Download Repositories List ===");
            
            int i = 0;
            List<ServiceModelBase> tempServiceRepositories = new(mSettingsServiceRepositories);
            mTempModel = new();

            foreach (ServiceModelBase serviceRepository in tempServiceRepositories)
            {
                i++;
                bool savedWithException = false;
                mLogger.LogInformation("{counter}. Download repositories list from RepositoriesName:{RepositoriesName} RepositoriesOwner:{RepositoriesOwner} FileName:{ServicesListFilePath}", i, serviceRepository.RepositoriesName, serviceRepository.RepositoriesOwner, serviceRepository.ServicesListFilePath);
     
                try
                {
                    byte[] fileContent = await mGitHubClient.Repository.Content.GetRawContent(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, serviceRepository.ServicesListFilePath);
                    List<ServiceFileContent> json = JsonUtilites.SerializeJson<List<ServiceFileContent>>(fileContent);
                    
                    var tempData = new ServiceRepositoryContent
                    {
                        RepositoriesName = serviceRepository.RepositoriesName,
                        RepositoriesOwner = serviceRepository.RepositoriesOwner,
                        ServicesListFilePath = serviceRepository.ServicesListFilePath,
                        ServiceFilesContent = json
                    };

                    mTempModel.ServiceRepositoriesContent.Add(tempData);

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
                    //if (!savedWithException)
                    //    mLogger.LogInformation("{counter}. {filePath} saved!", i, $"{mDownloadPath}{Path.DirectorySeparatorChar}{fileName}");
                }
            }

            await JsonUtilites.SerializeAndSaveJsonFilesAsync(mTempModel, mDownloadPath, "temp.json");

            mLogger.LogInformation("=== Step 1. Download Repositories Finished ===");
        }

        private async Task CheckAndSaveRepositoriesListAsync()
        {
            mLogger.LogInformation("=== Step 2. Check Repositories Info Files ===");

            List<string> repositoryInfoFilesName = [];

            foreach (ServiceModelBase serviceRepository in mSettingsServiceRepositories)
            {
                string fileName = $"{serviceRepository.RepositoriesOwner.ToLower()}.{serviceRepository.RepositoriesName.ToLower()}.repositories.json";
                
                List<ServiceModelBase> repositories = [];
                bool readWithException = false;

                try
                {
                    repositories = await JsonUtilites.ReadJsonFileAsync<List<ServiceModelBase>>(mDownloadPath, fileName);
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
                        foreach (ServiceModelBase repository in repositories)
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
                await JsonUtilites.SerializeAndSaveJsonFilesAsync(repositoryInfoFilesName, mDownloadPath, ServiceFileNames.DownloadRepositoriesFileName);
                mLogger.LogInformation("Create repeository file name {FilesNamePath}", ServiceFileNames.DownloadRepositoriesFileName);
            }

            mLogger.LogInformation("=== Step 2. Check Repositories Info Files Finished ===");
        }

        private async Task DownloadRepositoriesInfoAsync()
        {
            mLogger.LogInformation("=== Step 3. Download Service Info Files ===");

            //List<string> serviceInfoFilesName = [];
            int i = 0;

            ServiceRepositories tempModel = await JsonUtilites.ReadJsonFileAsync<ServiceRepositories>(mDownloadPath, "temp.json");

            bool savedWithException = false;

            foreach (ServiceRepositoryContent settingsServicesRepositories in tempModel.ServiceRepositoriesContent)
            {
                foreach (ServiceFileContent servicesListFileContent in settingsServicesRepositories.ServiceFilesContent)
                {
                    i++;

                    try
                    {
                        string filePath = "service_info.json";

                        if (!string.IsNullOrEmpty(servicesListFileContent.ServicesListFilePath))
                            filePath = servicesListFileContent.ServicesListFilePath;

                        byte[] fileContent = await mGitHubClient.Repository.Content.GetRawContent(servicesListFileContent.RepositoriesOwner, servicesListFileContent.RepositoriesName, filePath);
                        ServiceInfoModel json = JsonUtilites.SerializeJson<ServiceInfoModel>(fileContent);

                        servicesListFileContent.ServiceInfoFileContent = json;

                        //mLogger.LogInformation("{counter}. Start download service info file from RepositoriesName:{RepositoriesName} RepositoriesOwner:{RepositoriesOwner} FileName:{ServicesListFilePath}", i, settingsRepository.RepositoriesName, settingsRepository.RepositoriesOwner, filePath);


                        //string serviceRepositoriesList = System.Text.Encoding.Default.GetString(fileContent);
                        //await JsonUtilites.SaveRawJsonFilesAsync(serviceRepositoriesList, mDownloadPath, fileName);
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
                            mLogger.LogInformation("{сounter}. Download and save service info file", i);
                            //serviceInfoFilesName.Add(fileName);
                        }
                    }
                }
            }

            await JsonUtilites.SerializeAndSaveJsonFilesAsync(tempModel, mDownloadPath, "temp.json");
            mLogger.LogInformation("Create service info file name {FilesNamePath}", ServiceFileNames.DownloadInfoFileName);

            //if (serviceInfoFilesName.Count > 0)
            //{

            //}

            mLogger.LogInformation("=== Step 3. Download Service Info Files Finished ===");
        }

        private async Task EqualInstalledAndDownloadVersion()
        {
            List<ServiceInfoModel> installedInfoList = [];
            Dictionary<string, ServiceInfoModel> downloadInfoList = [];
            List<string> updateRequiredService = [];

            try
            {
                List<string> projectExecPaths = await JsonUtilites.ReadJsonFileAsync<List<string>>(mPublishPath, ServiceFileNames.ServiceExecPathsFileName);

                foreach (string projectExecPath in projectExecPaths)
                {
                    string projectDirrectory = Path.GetDirectoryName(projectExecPath);
                    ServiceInfoModel installedInfo = await JsonUtilites.ReadJsonFileAsync<ServiceInfoModel>(projectDirrectory, "service_info.json");

                    mLogger.LogInformation("Publish project {projectName} is version {projectVersion}", installedInfo.Services.Name, installedInfo.Services.Version);
                    installedInfoList.Add(installedInfo);
                }

                List<string> downloadInfoFilePaths = await JsonUtilites.ReadJsonFileAsync<List<string>>(mDownloadPath, ServiceFileNames.DownloadInfoFileName);

                foreach (string downloadInfoFilePath in downloadInfoFilePaths)
                {
                    var downloadInfo = await JsonUtilites.ReadJsonFileAsync<ServiceInfoModel>(mDownloadPath, downloadInfoFilePath);

                    mLogger.LogInformation("Download project {projectName} is version {projectVersion}", downloadInfo.Services.Name, downloadInfo.Services.Version);
                    downloadInfoList.Add(downloadInfoFilePath, downloadInfo);
                }

                foreach (KeyValuePair<string, ServiceInfoModel> downloadInfo in downloadInfoList)
                {
                    string installedVersion = installedInfoList.Where(x => x.Services.Name.Equals(downloadInfo.Value.Services.Name)).Select(x => x.Services.Version).FirstOrDefault();

                    if (!string.IsNullOrEmpty(installedVersion))
                    {
                        mLogger.LogInformation("For {projectName} installed version is {installedVersion} remoteVersion is {downloadVersion}", downloadInfo.Value.Services.Name, installedVersion, downloadInfo.Value.Services.Version);
                        int compareResult = StringUtilites.CompareVersions(installedVersion, downloadInfo.Value.Services.Version);

                        if (compareResult == -1)
                        {
                            mLogger.LogInformation("The installed version is smaller than the downloaded version. Need update!");
                            updateRequiredService.Add(downloadInfo.Key);
                        }

                        if (compareResult == 0)
                        {
                            mLogger.LogInformation("The installed version is equals than the downloaded version. No updates required!");
                        }

                        if (compareResult == 1)
                        {
                            mLogger.LogInformation("The installed version is higher than the downloaded version. No updates required");
                        }
                    }
                    else
                    {
                        mLogger.LogInformation("No information file found for the project {projectName}. Need install project!", downloadInfo.Value.Services.Name);
                        updateRequiredService.Add(downloadInfo.Key);
                    }
                }

                if(updateRequiredService.Count > 0) 
                    await JsonUtilites.SerializeAndSaveJsonFilesAsync(updateRequiredService, mDownloadPath, ServiceFileNames.UpdateRequiredServicesFileName);
            }
            catch (Exception ex) 
            { 
                mLogger.LogInformation("{error}", ex.Message);
            }
        }


        #endregion

        #region OnRaise events

        protected virtual  void OnRaiseCheckUpdateStartedEvent()
        {
            CheckUpdateStartedEventHandler raiseEvent = RaiseCheckUpdateStartedEvent;
            raiseEvent?.Invoke(this);
        }

        protected virtual void OnRaiseCheckUpdateFinishedEvent()
        {
            CheckUpdateFinishedEventHandler raiseEvent = RaiseCheckUpdateFinishedEvent;
            raiseEvent?.Invoke(this);
        }

        #endregion
    }
}
