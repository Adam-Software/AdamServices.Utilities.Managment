using Managment.Interface;
using Managment.Interface.Common;
using Managment.Interface.Common.JsonModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
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
        private readonly IJsonTempFilesWorkerService mTempFileWorkerService;

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
            mTempFileWorkerService = serviceProvider.GetService<IJsonTempFilesWorkerService>();

            mGitHubClient = serviceProvider.GetRequiredService<IGitHubCilentService>().GitHubClient;

            string downloadDirrectory = Path.Combine(mAppSettingsOptionsService.WorkingDirrectory, CommonFilesAndDirectoriesNames.DownloadDirrectory);
            mDownloadDirrectory = new DirectoryInfo(downloadDirrectory).FullName;

            string buildDirrectory = Path.Combine(mAppSettingsOptionsService.WorkingDirrectory, CommonFilesAndDirectoriesNames.BuildDirrectory);
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

        //public async Task DownloadUpdate()
        //{
            //await ReadServiceUpdateFile();
            //await DownloadZipFromRepositoryAsync();
            //await ExtractSourceFiles();
            //await CopySourceFilesToBuildDirrectory();
        //}

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
            mLogger.LogInformation("=== Download Zip From Repository Start ===");

            ServiceRepositories serviceRepositories = await mTempFileWorkerService.ReadTempFileAsync();

            try
            {
                foreach (RepositoriesBaseInfo serviceBaseInfo in serviceRepositories.ServiceFilesContent)
                {
                    byte[] archiveBytes = await mGitHubClient.Repository.Content.GetArchive(serviceBaseInfo.RepositoriesOwner, serviceBaseInfo.RepositoriesName, ArchiveFormat.Zipball);

                    string fileName = $"{serviceBaseInfo.RepositoriesName}.zip";
                    string downloadPath = Path.Combine(mDownloadDirrectory, fileName);

                    await File.WriteAllBytesAsync(downloadPath, archiveBytes);

                    mLogger.LogInformation("{repositoriesName} saved as {downloadPath}", serviceBaseInfo.RepositoriesName, downloadPath);
                }
            }
            catch (Exception ex)
            {
                mLogger.LogError("{error}", ex.Message);
            }

            mLogger.LogInformation("=== Download Zip From Repository Finished ===");
        }

        private async Task ExtractSourceFiles()
        {
            mLogger.LogInformation("=== Extract Source Files Started ===");

            try
            {
                ServiceRepositories serviceRepositories = await mTempFileWorkerService.ReadTempFileAsync();

                foreach (RepositoriesBaseInfo serviceBaseInfo in serviceRepositories.ServiceFilesContent)
                {
                    string filename = $"{serviceBaseInfo.RepositoriesName}.zip";
                    string zipArchiveFilePath = Path.Combine(mDownloadDirrectory, filename);
                    
                    string extractPath = Path.Combine(mDownloadDirrectory, serviceBaseInfo.RepositoriesName);

                    ZipArchive zipArchive = ZipFile.OpenRead(zipArchiveFilePath);
                    zipArchive.ExtractToDirectory(extractPath, true);
                    zipArchive.Dispose();

                    mLogger.LogInformation("{zipArchiveFilePath} extracted to dirrectory {extractPath}", zipArchiveFilePath, extractPath);
                }
            }
            catch(Exception ex)
            {
                mLogger.LogError("{error}", ex.Message);
            }

            mLogger.LogInformation("=== Extract Source Files Finihed ===");
        }

        private async Task CopySourceFilesToBuildDirrectory()
        {
            mLogger.LogInformation("=== Copy Source Files To Build Dirrectory Started ===");

            try
            {
                ServiceRepositories serviceRepositories = await mTempFileWorkerService.ReadTempFileAsync();

                foreach (RepositoriesBaseInfo serviceBaseInfo in serviceRepositories.ServiceFilesContent)
                {
                    string sourceDirrectory = Path.Combine(mDownloadDirrectory, serviceBaseInfo.RepositoriesName);
                    string sourcePath = new DirectoryInfo(sourceDirrectory).GetDirectories().FirstOrDefault().FullName;

                    string destonationPath = Path.Combine(mBuildDirrectory, serviceBaseInfo.RepositoriesName);
                    
                    DirectoryUtilites.CopyDirectory(sourcePath, destonationPath, true);

                    mLogger.LogInformation("Copy {source} to {destonation}", sourcePath, destonationPath);
                }
            }
            catch(Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }
            
            mLogger.LogInformation("=== Copy Source Files To Build Dirrectory Finish ===");
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
