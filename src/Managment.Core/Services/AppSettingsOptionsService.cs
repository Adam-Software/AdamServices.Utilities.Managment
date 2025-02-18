using Managment.Interface;
using Managment.Interface.AppSettingsOptionsServiceDependency;
using System.Collections.Generic;

namespace Managment.Core.Services
{
    public class AppSettingsOptionsService : IAppSettingsOptionsService
    {
        //public string CheckUpdateUrl { get; set; }
        //public string JsonRepositoryPath {  get; set; }
        public List<LocationsServiceRepositoryModel> LocationsServiceRepositoryList {  get; set; }
    }
}
