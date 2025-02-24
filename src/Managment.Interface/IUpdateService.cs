using System;
using System.Threading.Tasks;

namespace Managment.Interface
{
    public delegate void DownloadAndCheckUpdateStartedEventHandler(object sender);
    public delegate void DownloadAndCheckUpdateFinishedEventHandler(object sender);
    public interface IUpdateService : IDisposable
    {
        #region Events

        public event DownloadAndCheckUpdateStartedEventHandler RaiseDownloadAndCheckUpdateStartedEvent;
        public event DownloadAndCheckUpdateFinishedEventHandler RaiseDownloadAndCheckUpdateFinishedEvent;

        #endregion

        #region Methods

        public Task DownloadAndCheckUpdateInfoFiles();

        #endregion
    }
}
