using Managment.Interface;
using Managment.Interface.AppSettingsOptionsServiceDependency;
using Managment.Interface.CheckingUpdateServiceDependency;
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
        IAppSettingsOptionsService mAppSettingsOptionsService;

        #endregion

        #region Var

        private readonly GitHubClient mGitHubClient;
        private readonly List<ServiceRepositoryModel> mServiceRepositories;

        #endregion

        #region ~

        public DownloadService(IServiceProvider serviceProvider) 
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<DownloadService>>();
            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();

            IGitHubCilentService gitHubClientService = serviceProvider.GetRequiredService<IGitHubCilentService>();
            mGitHubClient = gitHubClientService.GitHubClient;

            mServiceRepositories = [];
            mLogger.LogInformation("=== DownloadService. Start ===");
        }

        #endregion

        public async Task ReadServiceRepository()
        {
            JsonRepository mJsonRepository = new(mAppSettingsOptionsService.UpdateServiceSettings.ServicesRepositoriesInfoDownloadPath);

            List<string> repositories = await mJsonRepository.ReadJsonFileAsync<List<string>>(mAppSettingsOptionsService.UpdateServiceSettings.DownloadRepositoriesFilesNamePath);

            foreach (string repository in repositories)
            {
                List<ServiceRepositoryModel> serviceRepository = await mJsonRepository.ReadJsonFileAsync<List<ServiceRepositoryModel>>(repository);

                foreach (var repo in serviceRepository)
                {
                    mServiceRepositories.Add(repo);
                }
            }

            await DownloadZipFromSourceAsync();
        }


        public async Task DownloadZipFromSourceAsync()
        {
            mLogger.LogInformation("=== Step 1. Download Zip From Repository ===");
            
            //List<string> serviceInfoFilesName = [];
            int i = 0;

            foreach (ServiceRepositoryModel serviceRepository in mServiceRepositories)
            {
                i++;

                bool savedWithException = false;
                //string filePath = "service_info.json";

                //if (!string.IsNullOrEmpty(serviceRepository.ServicesListFilePath))
                //    filePath = serviceRepository.ServicesListFilePath;

                //mLogger.LogInformation("{counter}. Start download service info file from RepositoriesName:{RepositoriesName} RepositoriesOwner:{RepositoriesOwner} FileName:{ServicesListFilePath}", i, serviceRepository.RepositoriesName, serviceRepository.RepositoriesOwner, filePath);
                //string fileName = $"{serviceRepository.RepositoriesOwner.ToLower()}.{serviceRepository.RepositoriesName.ToLower()}.info.json";

                try
                {
                    byte[] archiveBytes = await mGitHubClient.Repository.Content.GetArchive(serviceRepository.RepositoriesOwner, serviceRepository.RepositoriesName, ArchiveFormat.Zipball);
                    mLogger.LogInformation("{counter}{repositoriesName} downloading", i, serviceRepository.RepositoriesName);
                    await File.WriteAllBytesAsync($"{serviceRepository.RepositoriesName}.zip", archiveBytes);
                    mLogger.LogInformation("{counter}{repositoriesName} saved", i, serviceRepository.RepositoriesName);
                    
                    var zipTest = ZipFile.OpenRead($"{serviceRepository.RepositoriesName}.zip");
                    zipTest.ExtractToDirectory($"{serviceRepository.RepositoriesName}", true);
                    
                    mLogger.LogInformation("{counter}{repositoriesName} extracted", i, serviceRepository.RepositoriesName);

                    //string serviceRepositoriesList = System.Text.Encoding.Default.GetString(fileContent);
                    //await mJsonRepository.SaveRawJsonFilesAsync(serviceRepositoriesList, fileName);
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
                        //mLogger.LogInformation("{сounter}. Download service info file and save as {filename} ", i, fileName);
                        //serviceInfoFilesName.Add(fileName);
                    }
                }
            }


            mLogger.LogInformation("=== Step 3. Download Service Info Files Finished ===");
        }
    }
}
