using Managment.Interface;
using Managment.Interface.Common;
using Managment.Interface.Common.JsonModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;


namespace Managment.Services.Common
{
    public class UpdateService : IUpdateService
    {
        #region Events

        public event CheckUpdateStartedEventHandler RaiseCheckUpdateStartedEvent;
        public event CheckUpdateFinishedEventHandler RaiseCheckUpdateFinishedEvent;
        public event CheckUpdatesPublishedProjectStartedEventHandler RaiseCheckUpdatesPublishedProjectStartedEvent;
        public event CheckUpdatesPublishedProjectFinishedEventHandler RaiseCheckUpdatesPublishedProjectFinishedEvent;

        #endregion

        #region Services

        private readonly ILogger<UpdateService> mLogger;
        private readonly IAppSettingsOptionsService mAppSettingsOptionsService;
        private readonly IJsonTempFilesWorkerService mTempFileWorkerService;

        #endregion

        #region Var

        private readonly GitHubClient mGitHubClient;

        #endregion

        #region ~

        public UpdateService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<UpdateService>>();
            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();
            mTempFileWorkerService = serviceProvider.GetService<IJsonTempFilesWorkerService>();

            mGitHubClient = serviceProvider.GetRequiredService<IGitHubCilentService>().GitHubClient;

            mLogger.LogInformation("=== UpdateService. Start ===");
        }

        #endregion

        #region Public methods

        public async Task DownloadUpdateFileAsync()
        {
            OnRaiseCheckUpdateStartedEvent();

            await DownloadRepositoriesListAsync();
            await DownloadRepositoriesInfoAsync();

            OnRaiseCheckUpdateFinishedEvent();
        }

        public async Task CheckUpdatesPublishedProject()
        {
            OnRaiseCheckUpdatesPublishedProjectStartedEvent();

            await DownloadUpdateFileAsync();
            await EqualPublishedAndRemoteVersionAsync();

            OnRaiseCheckUpdatesPublishedProjectFinishedEvent();
        }

        public void Dispose()
        {
            mLogger.LogInformation("=== UpdateService. Dispose ===");
        }

        #endregion

        #region Private methods

        private async Task DownloadRepositoriesListAsync()
        {
            mLogger.LogInformation("=== Download Repositories List Start ===");
            
            List<RepositoriesBaseInfo> tempServiceRepositories = new(mAppSettingsOptionsService.UpdateServiceSettings.ServicesRepositories);
            ServiceRepositories serviceRepositories = new();

            foreach (RepositoriesBaseInfo serviceRepository in tempServiceRepositories)
            { 
                mLogger.LogInformation("Download repositories list from RepositoriesName:{RepositoriesName} RepositoriesOwner:{RepositoriesOwner} FileName:{ServicesListFilePath}", 
                     serviceRepository.RepositoriesName, serviceRepository.RepositoriesOwner, serviceRepository.ServiceFilePath);
     
                try
                {
                    byte[] fileContent = await mGitHubClient.Repository.Content.GetRawContent(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, serviceRepository.ServiceFilePath);
                    List<ServiceFileContent> json = mTempFileWorkerService.SerializeJson<List<ServiceFileContent>>(fileContent);

                    serviceRepositories.RepositoriesName = serviceRepository.RepositoriesName;
                    serviceRepositories.RepositoriesOwner = serviceRepository.RepositoriesOwner;
                    serviceRepositories.ServiceFilePath = serviceRepository.ServiceFilePath;
                    serviceRepositories.ServiceFilesContent = json;
                }
                catch (NotFoundException) 
                {
                    mLogger.LogError("Not found repository RepositoriesName:{RepositoriesName} RepositoriesOwner:{RepositoriesOwner} FileName:{ServicesListFilePath}",
                        serviceRepository.RepositoriesName, serviceRepository.RepositoriesOwner, serviceRepository.ServiceFilePath);
                }
                catch (HttpRequestException)
                {
                    mLogger.LogError("Not found repository RepositoriesName:{RepositoriesName} RepositoriesOwner:{RepositoriesOwner} FileName:{ServicesListFilePath}",
                        serviceRepository.RepositoriesName, serviceRepository.RepositoriesOwner, serviceRepository.ServiceFilePath);
                }
                catch (Exception ex)
                {
                    mLogger.LogError("{error}", ex);
                }
            }

            await mTempFileWorkerService.SaveTempFileAsync(serviceRepositories);

            mLogger.LogInformation("=== Download Repositories Finished ===");
        }

        private async Task DownloadRepositoriesInfoAsync()
        {
            mLogger.LogInformation("=== Download Service Info Files ===");
            
            ServiceRepositories serviceRepositories = await mTempFileWorkerService.ReadTempFileAsync();

            foreach (ServiceFileContent servicesListFileContent in serviceRepositories.ServiceFilesContent)
            {
                try
                {
                    string serviceFilePath = CommonFilesAndDirectoriesNames.DefaultServiceInfoName;

                    if (!string.IsNullOrEmpty(servicesListFileContent.ServiceFilePath))
                        serviceFilePath = servicesListFileContent.ServiceFilePath;

                    byte[] fileContent = await mGitHubClient.Repository.Content.GetRawContent(servicesListFileContent.RepositoriesOwner, servicesListFileContent.RepositoriesName, serviceFilePath);

                    ServiceInfoModel json = mTempFileWorkerService.SerializeJson<ServiceInfoModel>(fileContent);
                    servicesListFileContent.ServiceInfoFileContent = json;

                    mLogger.LogInformation("Download service info file from RepositoriesName:{RepositoriesName} RepositoriesOwner:{RepositoriesOwner} FileName:{ServicesListFilePath}", 
                        servicesListFileContent.RepositoriesName, servicesListFileContent.RepositoriesOwner, serviceFilePath);
                }
                catch (Exception ex)
                {
                    mLogger.LogError("{exception}", ex);
                }
            }
            
            await mTempFileWorkerService.SaveTempFileAsync(serviceRepositories);

            mLogger.LogInformation("=== Download Service Info Files Finished ===");
        }

        private async Task EqualPublishedAndRemoteVersionAsync()
        {
            mLogger.LogInformation("=== Equal Installed And Remote Version Start ===");

            try
            {
                ServiceRepositories serviceRepositories = await mTempFileWorkerService.ReadTempFileAsync();
                await CompareVersionAsync(serviceRepositories);
            }
            catch (FileNotFoundException ex)
            {
                mLogger.LogError("{error}", ex.Message);
                mLogger.LogError("Try download or update remote info files");
            }
            catch (Exception ex)
            {
                mLogger.LogError("{error}", ex);
            }

            mLogger.LogInformation("=== Equal Installed And Remote Version Finish ===");
        }

        private async Task CompareVersionAsync(ServiceRepositories serviceRepositories)
        {
            foreach (ServiceFileContent remoteServiceRepositories in serviceRepositories.ServiceFilesContent)
            {
                try
                {
                    ServiceInfoModel installedFileInfo = await mTempFileWorkerService.ReadPublishedProjectFileInfoAsync(remoteServiceRepositories.RepositoriesName);
                    ServiceInfoModel remoteFileInfo = remoteServiceRepositories.ServiceInfoFileContent;

                    mLogger.LogInformation("Installed {projectName} project version {projectVersion}", installedFileInfo.Services.Name, installedFileInfo.Services.Version);
                    mLogger.LogInformation("Remote {projectName} project version {projectVersion}", remoteFileInfo.Services.Name, remoteFileInfo.Services.Version);

                    int compareResult = StringUtilites.CompareVersions(installedFileInfo.Services.Version, remoteFileInfo.Services.Version);

                    if (compareResult == -1)
                    {
                        mLogger.LogInformation("The installed version is smaller than the remote version. Need update!");
                        remoteServiceRepositories.NeedUpdate = true;
                    }

                    if (compareResult == 0)
                    {
                        mLogger.LogInformation("The installed version is equals than the remote version. No updates required!");
                    }

                    if (compareResult == 1)
                    {
                        mLogger.LogInformation("The installed version is higher than the remote version. Need downgrade!");
                        remoteServiceRepositories.NeedUpdate = true;
                    }
                }
                catch(FileNotFoundException ex)
                {
                    mLogger.LogError("No information file found for the project {projectName}. Need install project!", remoteServiceRepositories.ServiceInfoFileContent.Services.Name);
                    mLogger.LogError("{error}", ex.Message);

                    remoteServiceRepositories.NeedInstall = true;
                }
                catch (Exception ex)
                {
                    mLogger.LogInformation("{error}", ex.Message);
                }

                await mTempFileWorkerService.SaveTempFileAsync(serviceRepositories);
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
        private void OnRaiseCheckUpdatesPublishedProjectStartedEvent()
        {
            CheckUpdatesPublishedProjectStartedEventHandler raiseEvent = RaiseCheckUpdatesPublishedProjectStartedEvent;
            raiseEvent?.Invoke(this);
        }

        private void OnRaiseCheckUpdatesPublishedProjectFinishedEvent()
        {
            CheckUpdatesPublishedProjectFinishedEventHandler raiseEvent = RaiseCheckUpdatesPublishedProjectFinishedEvent;
            raiseEvent?.Invoke(this);
        }

        #endregion
    }
}
