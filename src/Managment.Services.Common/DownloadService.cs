using Managment.Interface;
using Managment.Interface.AppSettingsOptionsServiceDependency;
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

        #endregion

        private const string cDownloadFolderPath = "download";
        private const string cBuildFolderPath = "build";

        #region ~

        public DownloadService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<DownloadService>>();
            mJsonRepositoryService = serviceProvider.GetRequiredService<IJsonRepositoryService>();

            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();

            IGitHubCilentService gitHubClientService = serviceProvider.GetRequiredService<IGitHubCilentService>();
            mGitHubClient = gitHubClientService.GitHubClient;

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

            List<string> repositories = await mJsonRepositoryService.ReadJsonFileAsync<List<string>>(mAppSettingsOptionsService.UpdateServiceSettings.DownloadRepositoriesFilesNamePath);

            foreach (string repository in repositories)
            {
                List<ServiceRepositoryModel> serviceRepository = await mJsonRepositoryService.ReadJsonFileAsync<List<ServiceRepositoryModel>>(repository);

                foreach (var repo in serviceRepository)
                {
                    mServiceRepositories.Add(repo);
                }
            }

            mLogger.LogInformation("=== Step 1. Read Service Repository Finish ===");
        }


        private async Task DownloadZipFromRepositoryAsync()
        {
            mLogger.LogInformation("=== Step 2. Download Zip From Repository Start ===");

            DirectoryUtilites.CreateOrClearRepositoryDirectory(cDownloadFolderPath);
            
            List<string> downloadArchiveFilesName = [];
            int i = 0;

            foreach (ServiceRepositoryModel serviceRepository in mServiceRepositories)
            {
                i++;

                try
                {
                    byte[] archiveBytes = await mGitHubClient.Repository.Content.GetArchive(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, ArchiveFormat.Zipball);
                    mLogger.LogInformation("{counter}{repositoriesName} downloading", i, serviceRepository.RepositoriesName);
                    
                    
                    string filename = $"{serviceRepository.RepositoriesName}.zip";
                    string downloadPath = Path.Combine(cDownloadFolderPath, filename);

                    await File.WriteAllBytesAsync(downloadPath, archiveBytes);

                    mLogger.LogInformation("{counter}. {repositoriesName} saved as {downloadPath}.zip", i, serviceRepository.RepositoriesName, downloadPath);
                    downloadArchiveFilesName.Add(downloadPath);
                }
                catch (Exception ex)
                {
                    mLogger.LogError("{exception}", ex);
                }
                
            }

            await mJsonRepositoryService.SerializeAndSaveJsonFilesAsync(downloadArchiveFilesName, cDownloadFolderPath, "download-zip-files-list.json");

            mLogger.LogInformation("=== Step 2. Download Zip From Repository Finished ===");
        }

        private async Task ExtractSourceFiles()
        {
            mLogger.LogInformation("=== Step 3. Extract Source Files Started ===");
            
            var zipArchiveFilePaths = await mJsonRepositoryService.ReadJsonFileAsync<List<string>>($"{cDownloadFolderPath}", "download-zip-files-list.json");
            List<string> extractArchiveFolders = [];

            foreach (var zipArchiveFilePath in zipArchiveFilePaths)
            {
                var extractDirectoryName = Path.GetFileNameWithoutExtension(zipArchiveFilePath);
                var extractPath = Path.Combine (cDownloadFolderPath, extractDirectoryName);

                ZipArchive zipArchive = ZipFile.OpenRead(zipArchiveFilePath);
                zipArchive.ExtractToDirectory(extractPath, true);
                extractArchiveFolders.Add(extractPath);
            }

            await mJsonRepositoryService.SerializeAndSaveJsonFilesAsync(extractArchiveFolders, cDownloadFolderPath, "extract-zip-files-folders.json");

            mLogger.LogInformation("=== Step 3. Extract Source Files Finihed ===");
        }

        private async Task MoveSourceFilesToBuildFolder()
        {
            mLogger.LogInformation("=== Step 4. Move Source Files To Build Folder Started ===");

            
            var sourceFolderPaths = await mJsonRepositoryService.ReadJsonFileAsync<List<string>>(cDownloadFolderPath, "extract-zip-files-folders.json");

            foreach (var sourceFolderPath in sourceFolderPaths) 
            {
                var source = new DirectoryInfo(sourceFolderPath).GetDirectories().FirstOrDefault();   
                var destonation = Path.Combine(cBuildFolderPath, Path.GetFileName(sourceFolderPath));

                try
                {
                    DirectoryUtilites.CopyDirectory(source.FullName, destonation, true);
                }
                catch(Exception ex) 
                {
                    mLogger.LogError(ex.Message);
                }
            }

            mLogger.LogInformation("=== Step 4. Move Source Files To Build Folder Finish ===");
        }

        #endregion
    }
}
