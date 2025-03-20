using System.Collections.Generic;

namespace Managment.Interface.Common.JsonModels
{
    public class ServiceRepositories
    {
        public List<ServiceRepositoryContent> ServiceRepositoriesContent { get; set; } = [];   
    }

    public class ServiceRepositoryContent: ServiceModelBase
    {
        public List<ServiceFileContent> ServiceFilesContent { get; set; } = [];
    }

    public class ServiceFileContent : ServiceModelBase
    {
        public ServiceInfoModel ServiceInfoFileContent { get; set; } = new();
    }
}
