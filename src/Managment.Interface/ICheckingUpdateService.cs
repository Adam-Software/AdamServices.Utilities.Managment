using Managment.Interface.CheckingUpdateServiceDependency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Managment.Interface
{
    public delegate void UpdateUrlsListEventHandler(object sender);
    public interface ICheckingUpdateService
    {
        #region Events

        public event UpdateUrlsListEventHandler RaiseUpdateUrlsListEvent;

        #endregion

        #region Fields

        public List<ServiceUrlModel> UpdateUrls { get; }

        #endregion

        #region Methods

        public void DownloadRepositoriesList();
        //public Task CheckAndSaveUpdateListsAsync();
        //public Task<List<ServiceInfoModel>> ReadServiceUpdateListsAsync();
        //public Task<List<ServiceNameWithUrl>> ReadServiceNameWithUrlListAsync();

        #endregion
    }
}
