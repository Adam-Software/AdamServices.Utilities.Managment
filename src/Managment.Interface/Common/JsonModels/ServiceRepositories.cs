using System.Collections.Generic;

namespace Managment.Interface.Common.JsonModels
{
    public class ServiceRepositories : RepositoriesBaseInfo
    {
        public List<ServiceFileContent> ServiceFilesContent { get; set; } = [];
    }

    public class ServiceFileContent : RepositoriesBaseInfo
    {
        public ServiceInfoModel ServiceInfoFileContent { get; set; } = new();
        public bool NeedUpdate { get; set; } = false;
        public bool NeedInstall { get; set; } = false;
    }
}
