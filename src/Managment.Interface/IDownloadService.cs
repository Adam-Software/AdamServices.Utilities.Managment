using System.Threading.Tasks;

namespace Managment.Interface
{
    public interface IDownloadService
    {
        public Task DownloadSourceToBuildFolders();
    }
}
