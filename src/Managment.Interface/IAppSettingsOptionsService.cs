using Managment.Interface.AppSettingsOptionsServiceDependency;
using System.Collections.Generic;

namespace Managment.Interface
{
    public interface IAppSettingsOptionsService
    {
        public string RepositoryListDownloadPath { get; set; }
        public List<ServiceRepositoryModel> LocationsServiceRepositoryList { get; set; }
    }
}
