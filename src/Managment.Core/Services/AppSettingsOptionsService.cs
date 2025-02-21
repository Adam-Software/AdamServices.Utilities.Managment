using Managment.Interface;
using Managment.Interface.AppSettingsOptionsServiceDependency;
using System.Collections.Generic;

namespace Managment.Core.Services
{
    public class AppSettingsOptionsService : IAppSettingsOptionsService
    {
        public string RepositoryListDownloadPath { get; set; }
        public List<ServiceRepositoryModel> LocationsServiceRepositoryList {  get; set; }
    }
}
