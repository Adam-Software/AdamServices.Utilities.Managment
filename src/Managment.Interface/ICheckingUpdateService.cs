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

        #endregion

        #region Methods

        public Task DownloadRepositoriesListAsync();
        public Task CheckRepositoriesListAsync();
        public Task DownloadRepositoriesInfoAsync();

        #endregion
    }
}
