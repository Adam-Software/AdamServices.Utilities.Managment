using Managment.Interface.Common.JsonModels;
using System.Collections.Generic;

namespace Managment.Interface.AppSettingsOptionsServiceDependency
{
    public class UpdateServiceSettings
    {
        public string RepositoriesDownloadPath { get; set; } = "repositories";
        public string DownloadInfoFilesNamePath { get; set; } = "download-info-files-name-list.json";
        public string DownloadRepositoriesFilesNamePath { get; set; } = "download-repositories-files-name-list.json";
        public List<ServiceRepositoryModel> ServicesRepositories { get; set; }
    }
}
