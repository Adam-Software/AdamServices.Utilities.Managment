using System;
using System.Threading.Tasks;

namespace Managment.Interface
{
    public delegate void DownloadSourceStartedEventHandler(object sender);
    public delegate void DownloadSourceFinishedEventHandler(object sender);

    public interface IDownloadService : IDisposable
    {
        public event DownloadSourceStartedEventHandler RaiseDownloadSourceStartedEvent;
        public event DownloadSourceFinishedEventHandler RaiseDownloadSourceFinishedEvent;

        public Task DownloadSource();
        public Task DownloadRelease();
        //public Task DownloadUpdate();
    }
}
