

using Managment.Interface.Common.JsonModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Managment.Interface
{
    public interface IJsonTempFilesWorkerService
    {
        public Task<ServiceRepositories> ReadTempFileAsync();
        public Task SaveTempFileAsync(ServiceRepositories content);
        public Task<Dictionary<string, ServiceInfoModel>> ReadPublishedProjectFileInfoAsync();
        public Task<ServiceInfoModel> ReadPublishedProjectFileInfoAsync(string repositoriesName);
        public T SerializeJson<T>(byte[] content) where T : class, new();
    }
}
