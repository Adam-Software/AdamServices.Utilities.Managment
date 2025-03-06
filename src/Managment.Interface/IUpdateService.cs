using System;
using System.Threading.Tasks;

namespace Managment.Interface
{
    public delegate void CheckUpdateStartedEventHandler(object sender);
    public delegate void CheckUpdateFinishedEventHandler(object sender);
    public interface IUpdateService : IDisposable
    {
        #region Events

        public event CheckUpdateStartedEventHandler RaiseCheckUpdateStartedEvent;
        public event CheckUpdateFinishedEventHandler RaiseCheckUpdateFinishedEvent;

        #endregion

        #region Methods

        public Task CheckUpdates();

        #endregion
    }
}
