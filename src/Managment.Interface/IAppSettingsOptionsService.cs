using Managment.Interface.AppSettingsOptionsServiceDependency;
using System.Collections.Generic;

namespace Managment.Interface
{
    public interface IAppSettingsOptionsService
    {
        //public string CheckUpdateUrl { get; set; }
        //public string JsonRepositoryPath { get; set; }

        public List<LocationsServiceRepositoryModel> LocationsServiceRepositoryList { get; set; }
    }
}
