using Managment.Interface.Common.JsonModels;
using System.Collections.Generic;

namespace Managment.Interface.AppSettingsOptionsServiceDependency
{
    public class UpdateServiceSettings
    {
        public string RepositoriesDownloadPath { get; set; } = "repositories";
        public List<ServiceRepositoryModel> ServicesRepositories { get; set; }
    }
}
