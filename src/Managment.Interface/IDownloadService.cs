using System;
using System.Threading.Tasks;

namespace Managment.Interface
{
    public interface IDownloadService : IDisposable
    {
        public Task DownloadSourceToBuildFolders();
    }
}
