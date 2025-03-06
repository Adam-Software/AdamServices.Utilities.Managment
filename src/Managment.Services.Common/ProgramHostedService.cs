using Managment.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Managment.Services.Common
{
    public sealed class ProgramHostedService : IHostedService, IHostedLifecycleService
    {
        #region Services

        private readonly ILogger<ProgramHostedService> mLogger;
        private readonly IAppSettingsOptionsService mAppSettingsOptionsService;
        private readonly IUpdateService mUpdateService;
        private readonly IAppArguments mAppArguments;
        private readonly IDownloadService mDownloadService;
        private readonly IBuildService mBuildService;

        #endregion

        #region Var

        private readonly Task mCompletedTask = Task.CompletedTask;

        #endregion

        #region ~

        public ProgramHostedService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<ProgramHostedService>>();

            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();
            mAppArguments = serviceProvider.GetRequiredService<IAppArguments>();
            mUpdateService = serviceProvider.GetRequiredService<IUpdateService>();
            mDownloadService = serviceProvider.GetRequiredService<IDownloadService>();
            mBuildService = serviceProvider.GetRequiredService<IBuildService>();


            var appLifetime = serviceProvider.GetService<IHostApplicationLifetime>();

            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);

            Subscribe();
        }

        #endregion

        #region Subscribe/Unsubscribe

        private void Subscribe()
        {
            mUpdateService.RaiseCheckUpdateStartedEvent += RaiseCheckUpdateStartedEvent;
            mUpdateService.RaiseCheckUpdateFinishedEvent += RaiseCheckUpdateFinishedEvent;
            mDownloadService.RaiseDownloadSourceStartedEvent += RaiseDownloadSourceStartedEvent;
            mDownloadService.RaiseDownloadSourceFinishedEvent += RaiseDownloadSourceFinishedEvent;
        }

        private void Unsubscribe()
        {
            mUpdateService.RaiseCheckUpdateStartedEvent -= RaiseCheckUpdateStartedEvent;
            mUpdateService.RaiseCheckUpdateFinishedEvent -= RaiseCheckUpdateFinishedEvent;
            mDownloadService.RaiseDownloadSourceStartedEvent -= RaiseDownloadSourceStartedEvent;
            mDownloadService.RaiseDownloadSourceFinishedEvent -= RaiseDownloadSourceFinishedEvent;
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

        private void RaiseDownloadSourceStartedEvent(object sender)
        {
            mLogger.LogTrace("=== Raise DownloadSourceStarted Event ===");
        }

        private void RaiseDownloadSourceFinishedEvent(object sender)
        {
            mLogger.LogTrace("=== Raise DownloadSourceFinished Event ===");
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
            
            return mCompletedTask;
        }

        private async void OnStarted()
        {
            mLogger.LogTrace("4. OnStarted has been called.");

            if ((mAppArguments.Install & mAppArguments.Update) || (!mAppArguments.Install & !mAppArguments.Update))
            {
                mLogger.LogWarning("The arguments are specified incorrectly, or not specified at all. I don't know what to do better");
                mLogger.LogWarning("I don't know what to do better. You need to specify one thing.");
                mLogger.LogWarning("Arguments are specified:");
                mLogger.LogWarning("Install mode: {install}", mAppArguments.Install);
                mLogger.LogWarning("Update mode: {update}", mAppArguments.Update);
                return;
            }

            if (mAppArguments.Update)
            {
                mLogger.LogInformation("The application is running in update mode");
            }

            if (mAppArguments.Install)
            {
                mLogger.LogInformation("The application is running in installation mode");

                //await mUpdateService.CheckUpdates();
                //await mDownloadService.DownloadSource();
                await mBuildService.PublishAsync();
            }
        }

        private void OnStopping()
        {
            Unsubscribe();
            mUpdateService.Dispose();
            mDownloadService.Dispose();

            mLogger.LogTrace("5. OnStopping has been called.");
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
    }
}
