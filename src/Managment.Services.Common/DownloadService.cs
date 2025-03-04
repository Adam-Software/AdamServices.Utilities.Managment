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

        #region Services

        private readonly ILogger<DownloadService> mLogger;
        private readonly IAppSettingsOptionsService mAppSettingsOptionsService;
        private readonly IJsonRepositoryService mJsonRepositoryService;

        #endregion

        #region Var

        private readonly GitHubClient mGitHubClient;
        private readonly List<ServiceRepositoryModel> mServiceRepositories;
        private readonly string mDownloadFolderPath = "download";
        private readonly string mBuildFolderPath = "build";

        #endregion

        #region Const

        private const string cDownloadZipFilesListName = "download-zip-files-list.json";
        private const string cExtractZipFilesFoldersFileName = "extract-zip-files-folders.json";

        #endregion

        #region ~

        public DownloadService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<DownloadService>>();
            mJsonRepositoryService = serviceProvider.GetRequiredService<IJsonRepositoryService>();

            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();

            IGitHubCilentService gitHubClientService = serviceProvider.GetRequiredService<IGitHubCilentService>();
            mGitHubClient = gitHubClientService.GitHubClient;

            mDownloadFolderPath = mAppSettingsOptionsService.DownloadServiceSettings.DownloadPath;
            mBuildFolderPath = mAppSettingsOptionsService.DownloadServiceSettings.BuildPath;

            mServiceRepositories = [];
            mLogger.LogInformation("=== DownloadService. Start ===");
        }

        #endregion

        #region Public methods

        public async Task DownloadSourceToBuildFolders()
        {
            await ReadServiceRepositoryFile();
            await DownloadZipFromRepositoryAsync();
            await ExtractSourceFiles();
            await MoveSourceFilesToBuildFolder();
        }

        #endregion

        #region Private methods

        private async Task ReadServiceRepositoryFile()
        {
            mLogger.LogInformation("=== Step 1. Read Service Repository Start ===");

            int i = 0;

            try
            {
                List<string> repositories = await mJsonRepositoryService.ReadJsonFileAsync<List<string>>(mAppSettingsOptionsService.UpdateServiceSettings.RepositoriesDownloadPath, mAppSettingsOptionsService.UpdateServiceSettings.DownloadRepositoriesFilesNamePath);
                
                foreach (string repository in repositories)
                {
                    List<ServiceRepositoryModel> serviceRepositories = await mJsonRepositoryService.ReadJsonFileAsync<List<ServiceRepositoryModel>>(mAppSettingsOptionsService.UpdateServiceSettings.RepositoriesDownloadPath, repository);

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
            finally
            {
                i = 0;
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
                DirectoryUtilites.CreateOrClearDirectory(mDownloadFolderPath);

                foreach (ServiceRepositoryModel serviceRepository in mServiceRepositories)
                {
                    i++;

                    byte[] archiveBytes = await mGitHubClient.Repository.Content.GetArchive(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, ArchiveFormat.Zipball);
                    mLogger.LogInformation("{counter}. {repositoriesName} downloading", i, serviceRepository.RepositoriesName);

                    string filename = $"{serviceRepository.RepositoriesName}.zip";
                    string downloadPath = Path.Combine(mDownloadFolderPath, filename);

                    await File.WriteAllBytesAsync(downloadPath, archiveBytes);

                    mLogger.LogInformation("{counter}. {repositoriesName} saved as {downloadPath}", i, serviceRepository.RepositoriesName, downloadPath);
                    downloadArchiveFilesName.Add(downloadPath);
                }

                await mJsonRepositoryService.SerializeAndSaveJsonFilesAsync(downloadArchiveFilesName, mDownloadFolderPath, cDownloadZipFilesListName);
            }
            catch (Exception ex)
            {
                mLogger.LogError("{error}", ex.Message);
            }
            finally
            {
                i = 0;
            }

            mLogger.LogInformation("=== Step 2. Download Zip From Repository Finished ===");
        }

        private async Task ExtractSourceFiles()
        {
            mLogger.LogInformation("=== Step 3. Extract Source Files Started ===");
            int i = 0;

            try
            {
                List<string> zipArchiveFilePaths = await mJsonRepositoryService.ReadJsonFileAsync<List<string>>(mDownloadFolderPath, cDownloadZipFilesListName);
                List<string> extractArchiveFolders = [];
                
                foreach (string zipArchiveFilePath in zipArchiveFilePaths)
                {
                    i++;

                    var extractDirectoryName = Path.GetFileNameWithoutExtension(zipArchiveFilePath);
                    var extractPath = Path.Combine(mDownloadFolderPath, extractDirectoryName);

                    ZipArchive zipArchive = ZipFile.OpenRead(zipArchiveFilePath);
                    zipArchive.ExtractToDirectory(extractPath, true);

                    extractArchiveFolders.Add(extractPath);
                    mLogger.LogInformation("{counter}. {zipArchiveFilePath} extracted to dirrectory {extractPath}", i, zipArchiveFilePath, extractPath);
                }

                await mJsonRepositoryService.SerializeAndSaveJsonFilesAsync(extractArchiveFolders, mDownloadFolderPath, cExtractZipFilesFoldersFileName);
            }
            catch(Exception ex)
            {
                mLogger.LogError("{error}", ex.Message);
            }
            finally
            {
                i = 0;
            }

            mLogger.LogInformation("=== Step 3. Extract Source Files Finihed ===");
        }

        private async Task MoveSourceFilesToBuildFolder()
        {
            mLogger.LogInformation("=== Step 4. Move Source Files To Build Folder Started ===");
            int i = 0;

            try
            {
                var sourceFolderPaths = await mJsonRepositoryService.ReadJsonFileAsync<List<string>>(mDownloadFolderPath, cExtractZipFilesFoldersFileName);
                DirectoryUtilites.CreateOrClearDirectory(mBuildFolderPath);

                foreach (var sourceFolderPath in sourceFolderPaths)
                {
                    i++;

                    string source = new DirectoryInfo(sourceFolderPath).GetDirectories().FirstOrDefault().FullName;
                    string destonation = Path.Combine(mBuildFolderPath, Path.GetFileName(sourceFolderPath));

                    DirectoryUtilites.CopyDirectory(source, destonation, true);
                    mLogger.LogInformation("{counter}. Copy {source} to {destonation}", i, source, destonation);
                }
            }
            catch(Exception ex) 
            {
                mLogger.LogError("{error}", ex.Message);
            }
            finally
            {
                i = 0;
            }

            mLogger.LogInformation("=== Step 4. Move Source Files To Build Folder Finish ===");
        }

        #endregion
    }
}
