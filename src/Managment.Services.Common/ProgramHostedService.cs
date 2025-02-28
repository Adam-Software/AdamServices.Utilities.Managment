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
        private readonly IUpdateService mCheckingUpdateService;
        private readonly IAppArguments mAppArguments;
        private readonly IDownloadService mDownloadService;

        #endregion

        #region Var

        private readonly Task mCompletedTask = Task.CompletedTask;

        #endregion

        #region ~

        public ProgramHostedService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<ProgramHostedService>>();

            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();
            mCheckingUpdateService = serviceProvider.GetRequiredService<IUpdateService>();
            mAppArguments = serviceProvider.GetRequiredService<IAppArguments>();
            mDownloadService = serviceProvider.GetRequiredService<IDownloadService>();

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
            mCheckingUpdateService.RaiseDownloadAndCheckUpdateStartedEvent += RaiseDownloadAndCheckUpdateStartedEvent;
            mCheckingUpdateService.RaiseDownloadAndCheckUpdateFinishedEvent += RaiseDownloadAndCheckUpdateFinishedEvent;
        }

        private void Unsubscribe()
        {
            mCheckingUpdateService.RaiseDownloadAndCheckUpdateStartedEvent -= RaiseDownloadAndCheckUpdateStartedEvent;
            mCheckingUpdateService.RaiseDownloadAndCheckUpdateFinishedEvent -= RaiseDownloadAndCheckUpdateFinishedEvent;
        }

        #endregion

        #region Events

        private void RaiseDownloadAndCheckUpdateStartedEvent(object sender)
        {
            mLogger.LogTrace("=== Raise DownloadAndCheckUpdateStarted Event ===");
        }

        private void RaiseDownloadAndCheckUpdateFinishedEvent(object sender)
        {
            mLogger.LogTrace("=== Raise DownloadAndCheckUpdateStarted Event ===");
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

                await mCheckingUpdateService.DownloadAndCheckUpdateInfoFiles();
                await mDownloadService.DownloadSourceToBuildFolders();
            }
        }

        private void OnStopping()
        {
            Unsubscribe();
            mCheckingUpdateService.Dispose();

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
