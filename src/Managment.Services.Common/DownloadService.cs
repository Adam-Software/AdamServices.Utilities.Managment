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

        string cDownloadFolderPath = "download";

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

        public async Task ReadServiceRepositoryFile()
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

            await DownloadZipFromRepositoryAsync();
        }


        public async Task DownloadZipFromRepositoryAsync()
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
                    
                    string downloadPath = Path.Combine(cDownloadFolderPath, serviceRepository.RepositoriesName);
                    await File.WriteAllBytesAsync($"{downloadPath}.zip", archiveBytes);

                    mLogger.LogInformation("{counter}. {repositoriesName} saved as {downloadPath}.zip", i, serviceRepository.RepositoriesName, downloadPath);
                    downloadArchiveFilesName.Add(serviceRepository.RepositoriesName);
                }
                catch (Exception ex)
                {
                    mLogger.LogError("{exception}", ex);
                }
                
            }

            await mJsonRepositoryService.SerializeAndSaveJsonFilesAsync(downloadArchiveFilesName, cDownloadFolderPath, "download-zip-files-list.json");

            mLogger.LogInformation("=== Step 2. Download Zip From Repository Finished ===");

            await ExtractSourceFiles();
        }

        private async Task ExtractSourceFiles()
        {
            mLogger.LogInformation("=== Step 3. Extract Source Files Started ===");

            List<string> downloadArchiveFilesName = [];

            foreach (ServiceRepositoryModel serviceRepository in mServiceRepositories)
            {
                var zipArchivePath = Path.Combine(cDownloadFolderPath , serviceRepository.RepositoriesName);
                ZipArchive zipArchive = ZipFile.OpenRead($"{zipArchivePath}.zip");

                var extractPath = Path.Combine(zipArchivePath, serviceRepository.RepositoriesName);
                zipArchive.ExtractToDirectory(extractPath, true);
                var entries = zipArchive.Entries;
                
                foreach(var entry in entries)
                {
                    downloadArchiveFilesName.Add(entry.FullName);
                    mLogger.LogTrace("{path}", entry.FullName);
                }


                //mLogger.LogInformation("{counter}{repositoriesName} extracted", i, serviceRepository.RepositoriesName);

                //string serviceRepositoriesList = System.Text.Encoding.Default.GetString(fileContent);
                //await mJsonRepository.SaveRawJsonFilesAsync(serviceRepositoriesList, fileName);
            }

            await mJsonRepositoryService.SerializeAndSaveJsonFilesAsync(downloadArchiveFilesName, cDownloadFolderPath, "extract-zip-files-list.json");

            mLogger.LogInformation("=== Step 3. Extract Source Files Finihed ===");
        }





    }
}
