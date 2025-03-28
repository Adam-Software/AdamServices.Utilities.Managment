using System;
using System.Threading.Tasks;

namespace Managment.Interface
{
    public delegate void CheckUpdateStartedEventHandler(object sender);
    public delegate void CheckUpdateFinishedEventHandler(object sender);

    public delegate void CheckUpdatesPublishedProjectStartedEventHandler(object sender);
    public delegate void CheckUpdatesPublishedProjectFinishedEventHandler(object sender);

    public interface IUpdateService : IDisposable
    {
        #region Events

        public event CheckUpdateStartedEventHandler RaiseCheckUpdateStartedEvent;
        public event CheckUpdateFinishedEventHandler RaiseCheckUpdateFinishedEvent;

        public event CheckUpdatesPublishedProjectStartedEventHandler RaiseCheckUpdatesPublishedProjectStartedEvent;
        public event CheckUpdatesPublishedProjectFinishedEventHandler RaiseCheckUpdatesPublishedProjectFinishedEvent;


        #endregion

        #region Methods

        public Task DownloadUpdateFileAsync();
        public Task CheckUpdatesPublishedProject();

        #endregion
    }
}
