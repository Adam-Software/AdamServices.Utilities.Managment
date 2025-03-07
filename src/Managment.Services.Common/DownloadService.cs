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
        private readonly List<ServiceRepositoryModel> mServiceRepositories = [];
        private readonly string mSourceDownloadPath = "download";
        private readonly string mSourceBuildPath = "build";

        #endregion

        #region Const

        private const string cDownloadZipFilesListName = "download-zip-files-list.json";
        private const string cExtractZipFilesFoldersFileName = "extract-zip-files-folders.json";

        #endregion

        #region ~

        public DownloadService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<DownloadService>>();
            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();
            IGitHubCilentService gitHubClientService = serviceProvider.GetRequiredService<IGitHubCilentService>();

            mGitHubClient = gitHubClientService.GitHubClient;
            mSourceDownloadPath = mAppSettingsOptionsService.DownloadServiceSettings.SourceDownloadPath;
            mSourceBuildPath = mAppSettingsOptionsService.DownloadServiceSettings.SourceBuildPath;

            mLogger.LogInformation("=== DownloadService. Start ===");
        }

        #endregion

        #region Public methods

        public async Task DownloadSource()
        {
            OnRaiseDownloadSourceStartedEvent();

            DirectoryUtilites.CreateOrClearDirectory(mSourceBuildPath);
            DirectoryUtilites.CreateOrClearDirectory(mSourceDownloadPath);

            await ReadServiceRepositoryFile();
            await DownloadZipFromRepositoryAsync();
            await ExtractSourceFiles();
            await CopySourceFilesToBuildFolder();

            OnRaiseDownloadSourceFinishedEvent();
        }

        public void Dispose()
        {
            mLogger.LogInformation("=== DownloadService. Dispose ===");

            DirectoryUtilites.CreateOrClearDirectory(mSourceDownloadPath);
            DirectoryUtilites.CreateOrClearDirectory(mSourceBuildPath);

            mServiceRepositories.Clear();
        }

        #endregion

        #region Private methods

        private async Task ReadServiceRepositoryFile()
        {
            mLogger.LogInformation("=== Step 1. Read Service Repository Start ===");

            int i = 0;

            try
            {
                List<string> repositories = await JsonUtilites.ReadJsonFileAsync<List<string>>(mAppSettingsOptionsService.UpdateServiceSettings.RepositoriesDownloadPath, ServiceFileNames.DownloadRepositoriesFilesNamePath);
                
                foreach (string repository in repositories)
                {
                    List<ServiceRepositoryModel> serviceRepositories = await JsonUtilites.ReadJsonFileAsync<List<ServiceRepositoryModel>>(mAppSettingsOptionsService.UpdateServiceSettings.RepositoriesDownloadPath, repository);

                    foreach (var serviceRepository in serviceRepositories)
                    {
                        i++;
                        mServiceRepositories.Add(serviceRepository);
                        mLogger.LogInformation("{cointer}. {RepositoriesName} added to the list of repositories for download", i, serviceRepository.RepositoriesName);
                    }
                }
            }
            catch(Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }

            mLogger.LogInformation("=== Step 1. Read Service Repository Finish ===");
        }


        private async Task DownloadZipFromRepositoryAsync()
        {
            mLogger.LogInformation("=== Step 2. Download Zip From Repository Start ===");

            List<string> downloadArchiveFilesName = [];
            int i = 0;

            try
            {
                foreach (ServiceRepositoryModel serviceRepository in mServiceRepositories)
                {
                    i++;

                    byte[] archiveBytes = await mGitHubClient.Repository.Content.GetArchive(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, ArchiveFormat.Zipball);
                    mLogger.LogInformation("{counter}. {repositoriesName} downloading", i, serviceRepository.RepositoriesName);

                    string filename = $"{serviceRepository.RepositoriesName}.zip";
                    string downloadPath = Path.Combine(mSourceDownloadPath, filename);

                    await File.WriteAllBytesAsync(downloadPath, archiveBytes);

                    mLogger.LogInformation("{counter}. {repositoriesName} saved as {downloadPath}", i, serviceRepository.RepositoriesName, downloadPath);
                    downloadArchiveFilesName.Add(downloadPath);
                }

                await JsonUtilites.SerializeAndSaveJsonFilesAsync(downloadArchiveFilesName, mSourceDownloadPath, cDownloadZipFilesListName);
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
                List<string> zipArchiveFilePaths = await JsonUtilites.ReadJsonFileAsync<List<string>>(mSourceDownloadPath, cDownloadZipFilesListName);
                List<string> extractArchiveFolders = [];
                
                foreach (string zipArchiveFilePath in zipArchiveFilePaths)
                {
                    i++;

                    var extractDirectoryName = Path.GetFileNameWithoutExtension(zipArchiveFilePath);
                    var extractPath = Path.Combine(mSourceDownloadPath, extractDirectoryName);

                    ZipArchive zipArchive = ZipFile.OpenRead(zipArchiveFilePath);
                    zipArchive.ExtractToDirectory(extractPath, true);

                    extractArchiveFolders.Add(extractPath);
                    mLogger.LogInformation("{counter}. {zipArchiveFilePath} extracted to dirrectory {extractPath}", i, zipArchiveFilePath, extractPath);
                }

                await JsonUtilites.SerializeAndSaveJsonFilesAsync(extractArchiveFolders, mSourceDownloadPath, cExtractZipFilesFoldersFileName);
            }
            catch(Exception ex)
            {
                mLogger.LogError("{error}", ex.Message);
            }

            mLogger.LogInformation("=== Step 3. Extract Source Files Finihed ===");
        }

        private async Task CopySourceFilesToBuildFolder()
        {
            mLogger.LogInformation("=== Step 4. Copy Source Files To Build Folder Started ===");
            List<string> buildTargetPaths = [];
            int i = 0;

            try
            {
                var sourceFolderPaths = await JsonUtilites.ReadJsonFileAsync<List<string>>(mSourceDownloadPath, cExtractZipFilesFoldersFileName);

                foreach (var sourceFolderPath in sourceFolderPaths)
                {
                    i++;

                    string dirrectoryName = Path.GetFileName(sourceFolderPath);
                    string source = new DirectoryInfo(sourceFolderPath).GetDirectories().FirstOrDefault().FullName;
                    string destonation = Path.Combine(mSourceBuildPath, dirrectoryName);

                    DirectoryUtilites.CopyDirectory(source, destonation, true);
                    buildTargetPaths.Add(dirrectoryName);
                    mLogger.LogInformation("{counter}. Copy {source} to {destonation}", i, source, destonation);
                }
            }
            catch(Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }
            
            await JsonUtilites.SerializeAndSaveJsonFilesAsync(buildTargetPaths, mSourceBuildPath, ServiceFileNames.BuildTargetPathList);
            mLogger.LogInformation("=== Step 4. Copy Source Files To Build Folder Finish ===");
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
