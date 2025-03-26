using Managment.Interface;
using Managment.Interface.Common;
using Managment.Interface.Common.JsonModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using File = System.IO.File;

namespace Managment.Services.Common
{
    public class DownloadService : IDownloadService
    {
        #region Events

        public event DownloadSourceStartedEventHandler RaiseDownloadSourceStartedEvent;
        public event DownloadSourceFinishedEventHandler RaiseDownloadSourceFinishedEvent;

        #endregion

        #region Services

        private readonly ILogger<DownloadService> mLogger;
        private readonly IAppSettingsOptionsService mAppSettingsOptionsService;

        #endregion

        #region Var

        private readonly GitHubClient mGitHubClient;
        private readonly string mDownloadDirrectory;
        private readonly string mBuildDirrectory;

        #endregion

        #region Const


        #endregion

        #region ~

        public DownloadService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<DownloadService>>();
            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();

            mGitHubClient = serviceProvider.GetRequiredService<IGitHubCilentService>().GitHubClient;

            string downloadDirrectory = Path.Combine(mAppSettingsOptionsService.WorkingDirrectory, CommonFilesAndDirectoriesNames.DownloadDirrectory);
            mDownloadDirrectory = new DirectoryInfo(downloadDirrectory).FullName;

            var buildDirrectory = Path.Combine(mAppSettingsOptionsService.WorkingDirrectory, CommonFilesAndDirectoriesNames.BuildDirrectory);
            mBuildDirrectory = new DirectoryInfo(buildDirrectory).FullName;

            mLogger.LogInformation("=== DownloadService. Start ===");
        }

        #endregion

        #region Public methods

        public async Task DownloadSource()
        {
            OnRaiseDownloadSourceStartedEvent();

            DirectoryUtilites.CreateOrClearDirectory(mBuildDirrectory);
            DirectoryUtilites.CreateOrClearDirectory(mDownloadDirrectory);

            await DownloadZipFromRepositoryAsync();
            await ExtractSourceFiles();
            await CopySourceFilesToBuildDirrectory();

            OnRaiseDownloadSourceFinishedEvent();
        }

        public async Task DownloadUpdate()
        {
            //await ReadServiceUpdateFile();
            //await DownloadZipFromRepositoryAsync();
            //await ExtractSourceFiles();
            //await CopySourceFilesToBuildDirrectory();
        }

        public void Dispose()
        {
            mLogger.LogInformation("=== DownloadService. Dispose ===");

            try
            {
                DirectoryUtilites.CreateOrClearDirectory(mDownloadDirrectory);
                DirectoryUtilites.CreateOrClearDirectory(mBuildDirrectory);
            }
            catch(Exception ex)
            {
                mLogger.LogError("{error}", ex.Message);
            }
            
        }

        #endregion

        #region Private methods

        private async Task DownloadZipFromRepositoryAsync()
        {
            mLogger.LogInformation("=== Step 2. Download Zip From Repository Start ===");

            ServiceRepositories temp = await JsonUtilites.ReadJsonFileAsync<ServiceRepositories>(mAppSettingsOptionsService.WorkingDirrectory, CommonFilesAndDirectoriesNames.TempInfoJsonFile);
            
            int i = 0;

            try
            {
                foreach (RepositoriesBaseInfo serviceRepository in temp.ServiceFilesContent)
                {
                    i++;

                    byte[] archiveBytes = await mGitHubClient.Repository.Content.GetArchive(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, ArchiveFormat.Zipball);
                    mLogger.LogInformation("{counter}. {repositoriesName} downloading", i, serviceRepository.RepositoriesName);

                    string filename = $"{serviceRepository.RepositoriesName}.zip";
                    string downloadPath = Path.Combine(mDownloadDirrectory, filename);

                    await File.WriteAllBytesAsync(downloadPath, archiveBytes);

                    mLogger.LogInformation("{counter}. {repositoriesName} saved as {downloadPath}", i, serviceRepository.RepositoriesName, downloadPath);
                }
            }
            catch (Exception ex)
            {
                mLogger.LogError("{error}", ex.Message);
            }

            mLogger.LogInformation("=== Step 2. Download Zip From Repository Finished ===");
        }

        private async Task ExtractSourceFiles()
        {
            mLogger.LogInformation("=== Step 3. Extract Source Files Started ===");
            int i = 0;

            try
            {
                ServiceRepositories temp = await JsonUtilites.ReadJsonFileAsync<ServiceRepositories>(mAppSettingsOptionsService.WorkingDirrectory, CommonFilesAndDirectoriesNames.TempInfoJsonFile);

                foreach (RepositoriesBaseInfo serviceRepository in temp.ServiceFilesContent)
                {
                    i++;

                    string filename = $"{serviceRepository.RepositoriesName}.zip";
                    var zipArchiveFilePath = Path.Combine(mDownloadDirrectory, filename);
                    var extractPath = Path.Combine(mDownloadDirrectory, serviceRepository.RepositoriesName);

                    ZipArchive zipArchive = ZipFile.OpenRead(zipArchiveFilePath);
                    zipArchive.ExtractToDirectory(extractPath, true);
                    zipArchive.Dispose();

                    mLogger.LogInformation("{counter}. {zipArchiveFilePath} extracted to dirrectory {extractPath}", i, zipArchiveFilePath, extractPath);
                }
            }
            catch(Exception ex)
            {
                mLogger.LogError("{error}", ex.Message);
            }

            mLogger.LogInformation("=== Step 3. Extract Source Files Finihed ===");
        }

        private async Task CopySourceFilesToBuildDirrectory()
        {
            mLogger.LogInformation("=== Step 4. Copy Source Files To Build Dirrectory Started ===");
            int i = 0;

            try
            {
                ServiceRepositories temp = await JsonUtilites.ReadJsonFileAsync<ServiceRepositories>(mAppSettingsOptionsService.WorkingDirrectory, CommonFilesAndDirectoriesNames.TempInfoJsonFile);

                foreach (RepositoriesBaseInfo serviceRepository in temp.ServiceFilesContent)
                {
                    i++;

                    var sourceFolderPath = Path.Combine(mDownloadDirrectory, serviceRepository.RepositoriesName);
                    string dirrectoryName = serviceRepository.RepositoriesName;
                    string source = new DirectoryInfo(sourceFolderPath).GetDirectories().FirstOrDefault().FullName;
                    string destonation = Path.Combine(mBuildDirrectory, dirrectoryName);

                    DirectoryUtilites.CopyDirectory(source, destonation, true);

                    mLogger.LogInformation("{counter}. Copy {source} to {destonation}", i, source, destonation);
                }
            }
            catch(Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }
            
            mLogger.LogInformation("=== Step 4. Copy Source Files To Build Dirrectory Finish ===");
        }

        #endregion

        #region OnRaise events

        protected virtual void OnRaiseDownloadSourceStartedEvent()
        {
            DownloadSourceStartedEventHandler raiseEvent = RaiseDownloadSourceStartedEvent;
            raiseEvent?.Invoke(this);
        }

        protected virtual void OnRaiseDownloadSourceFinishedEvent()
        {
            DownloadSourceFinishedEventHandler raiseEvent = RaiseDownloadSourceFinishedEvent;
            raiseEvent?.Invoke(this);
        }

        #endregion
    }
}
