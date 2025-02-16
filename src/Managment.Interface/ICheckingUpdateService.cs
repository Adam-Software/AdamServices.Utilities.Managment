using Managment.Interface.CheckingUpdateServiceDependency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Managment.Interface
{
    public interface ICheckingUpdateService
    {
        public void PrintCheckUpdateUrl();
        public Task CheckAndSaveUpdateListsAsync();
        public Task<List<ServiceInfoModel>> ReadServiceUpdateListsAsync();
        public Task<List<ServiceNameWithUrl>> ReadServiceNameWithUrlListAsync();
    }
}
