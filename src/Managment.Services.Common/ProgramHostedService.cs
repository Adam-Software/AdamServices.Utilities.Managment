using Managment.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Managment.Services.Common
{
    public sealed class ProgramHostedService : IHostedService, IHostedLifecycleService, IDisposable
    {
        #region Services

        private readonly ILogger<ProgramHostedService> mLogger;
        private readonly IUpdateService mUpdateService;
        private readonly IDownloadService mDownloadService;
        private readonly IDotnetService mDotnetService;
        private readonly IHostApplicationLifetime mAppLifetime;

        private readonly ArgumentsParserService mAppArguments;

        #endregion

        #region Var

        private readonly Task mCompletedTask = Task.CompletedTask;
        private readonly CancellationTokenSource mCancellationSource = new();
        private bool mIsDisposed = false;

        #endregion

        #region ~

        public ProgramHostedService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<ProgramHostedService>>();

            mUpdateService = serviceProvider.GetRequiredService<IUpdateService>();
            mDownloadService = serviceProvider.GetRequiredService<IDownloadService>();
            mDotnetService = serviceProvider.GetRequiredService<IDotnetService>();
            mAppLifetime = serviceProvider.GetService<IHostApplicationLifetime>();

            mAppArguments = serviceProvider.GetService<ArgumentsParserService>();

            Register();
            Subscribe();
        }

        #endregion

        #region Subscribe/Unsubscribe and register

        private void Register()
        {
            mAppLifetime.ApplicationStarted.Register(OnStarted);
            mAppLifetime.ApplicationStopping.Register(OnStopping);
            mAppLifetime.ApplicationStopped.Register(OnStopped);
        }

        private void Subscribe()
        {
            mUpdateService.RaiseCheckUpdateStartedEvent += RaiseCheckUpdateStartedEvent;
            mUpdateService.RaiseCheckUpdateFinishedEvent += RaiseCheckUpdateFinishedEvent;

            mUpdateService.RaiseCheckUpdatesPublishedProjectStartedEvent += RaiseCheckUpdatesPublishedProjectStartedEvent;
            mUpdateService.RaiseCheckUpdatesPublishedProjectFinishedEvent += RaiseCheckUpdatesPublishedProjectFinishedEvent;

            mDownloadService.RaiseDownloadSourceStartedEvent += RaiseDownloadSourceStartedEvent;
            mDownloadService.RaiseDownloadSourceFinishedEvent += RaiseDownloadSourceFinishedEvent;

            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        }

        private void Unsubscribe()
        {
            mUpdateService.RaiseCheckUpdateStartedEvent -= RaiseCheckUpdateStartedEvent;
            mUpdateService.RaiseCheckUpdateFinishedEvent -= RaiseCheckUpdateFinishedEvent;

            mUpdateService.RaiseCheckUpdatesPublishedProjectStartedEvent -= RaiseCheckUpdatesPublishedProjectStartedEvent;
            mUpdateService.RaiseCheckUpdatesPublishedProjectFinishedEvent -= RaiseCheckUpdatesPublishedProjectFinishedEvent;

            mDownloadService.RaiseDownloadSourceStartedEvent -= RaiseDownloadSourceStartedEvent;
            mDownloadService.RaiseDownloadSourceFinishedEvent -= RaiseDownloadSourceFinishedEvent;

            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        }

        #endregion

        #region Events

        private void RaiseCheckUpdateStartedEvent(object sender)
        {
            mLogger.LogTrace("=== Raise DownloadAndCheckUpdateStarted Event ===");
        }

        private void RaiseCheckUpdateFinishedEvent(object sender)
        {
            mLogger.LogTrace("=== Raise DownloadAndCheckUpdateStarted Event ===");
        }

        private void RaiseCheckUpdatesPublishedProjectStartedEvent(object sender)
        {
            mLogger.LogTrace("=== Raise CheckUpdatesPublishedProjectStarted Event ===");
        }

        private void RaiseCheckUpdatesPublishedProjectFinishedEvent(object sender)
        {
            mLogger.LogTrace("=== Raise CheckUpdatesPublishedProjectFinished Event ===");
        }

        private void RaiseDownloadSourceStartedEvent(object sender)
        {
            mLogger.LogTrace("=== Raise DownloadSourceStarted Event ===");
        }

        private void RaiseDownloadSourceFinishedEvent(object sender)
        {
            mLogger.LogTrace("=== Raise DownloadSourceFinished Event ===");
        }


        private void ProcessExit(object sender, EventArgs e)
        {
            mAppLifetime.StopApplication();
        }

        #endregion

        public Task StartingAsync(CancellationToken cancellationToken)
        {
            mLogger.LogTrace("1. StartingAsync has been called.");
            
            return mCompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            mLogger.LogTrace("2. StartAsync has been called.");

            return mCompletedTask;
        }

        public Task StartedAsync(CancellationToken cancellationToken)
        {
            mLogger.LogTrace("3. StartedAsync has been called.");

            if (!mAppArguments.ValidateParameters())
            {
                mLogger.LogWarning("The arguments are specified incorrectly, or not specified at all. I don't know what to do better");
                mLogger.LogWarning("I don't know what to do better. You need to specify one thing.");
                mLogger.LogWarning("Arguments are specified:");
                mLogger.LogWarning("Install mode: {install}", mAppArguments.Install);
                mLogger.LogWarning("Update mode: {update}", mAppArguments.Update);
                mLogger.LogWarning("Update mode: {run}", mAppArguments.Run);

                mAppLifetime.StopApplication();
                return Task.CompletedTask;
            }

            if (mAppArguments.Update)
            {
                mLogger.LogInformation("The application is running in update mode");

                mUpdateService.CheckUpdatesPublishedProject().Wait(CancellationToken.None);

                return Task.CompletedTask;
            }

            if (mAppArguments.Install)
            {
                mLogger.LogInformation("The application is running in installation mode");

                mUpdateService.DownloadUpdateFileAsync().Wait(CancellationToken.None);
                mDownloadService.DownloadSource().Wait(CancellationToken.None);
                mDotnetService.PublishAsync(mCancellationSource.Token).Wait(CancellationToken.None);
                
                return Task.CompletedTask;
            }

            if (mAppArguments.Run)
            {
                mLogger.LogInformation("The application is running in run mode");
                
                mDotnetService.RunAsync(mCancellationSource.Token);
                
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            mLogger.LogTrace("4. OnStarted has been called.");

        }

        private void OnStopping()
        {
            mLogger.LogTrace("5. OnStopping has been called.");
            Dispose();
        }


        public Task StoppingAsync(CancellationToken cancellationToken)
        {
            mLogger.LogTrace("6. StoppingAsync has been called.");
            return mCompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            mLogger.LogTrace("7. StopAsync has been called.");
            return mCompletedTask;
        }

        public Task StoppedAsync(CancellationToken cancellationToken)
        {
            mLogger.LogTrace("8. StoppedAsync has been called.");
            return mCompletedTask;
        }

        private void OnStopped()
        {
            mLogger.LogTrace("9. OnStopped has been called.");
        }

        public void Dispose(bool disposing)
        {
            if (mIsDisposed) return;

            if (disposing)
            {
                mCancellationSource.Cancel();

                Unsubscribe();

                mDotnetService.Dispose();
                mUpdateService.Dispose();
                mDownloadService.Dispose();
            }

            mIsDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
