using Managment.Interface;
using Managment.Interface.CheckingUpdateServiceDependency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Managment.Services.Common
{
    public sealed class ProgramHostedService : IHostedService, IHostedLifecycleService
    {
        #region Services

        private readonly ILogger<ProgramHostedService> mLogger;
        private readonly IAppSettingsOptionsService mAppSettingsOptionsService;
        private readonly ICheckingUpdateService mCheckingUpdateService;
        private readonly IAppArguments mAppArguments;
        private readonly IGitService mGitService;

        #endregion

        #region Var

        private readonly Task mCompletedTask = Task.CompletedTask;

        #endregion

        #region ~

        public ProgramHostedService(IServiceProvider serviceProvider)
        {
            mLogger = serviceProvider.GetRequiredService<ILogger<ProgramHostedService>>();
            mAppSettingsOptionsService = serviceProvider.GetRequiredService<IAppSettingsOptionsService>();
            mCheckingUpdateService = serviceProvider.GetRequiredService<ICheckingUpdateService>();
            mAppArguments = serviceProvider.GetRequiredService<IAppArguments>();
            mGitService = serviceProvider.GetRequiredService<IGitService>();

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
            mCheckingUpdateService.RaiseUpdateUrlsListEvent += RaiseUpdateUrlsListEvent;
        }

        private void Unsubscribe()
        {
            mCheckingUpdateService.RaiseUpdateUrlsListEvent -= RaiseUpdateUrlsListEvent;
        }

        #endregion

        #region Events

        private void RaiseUpdateUrlsListEvent(object sender)
        {
            List<ServiceUrlModel> tempUrls = mCheckingUpdateService.UpdateUrls;

            mLogger.LogTrace("UpdateUrlsListEvent raised");

            mLogger.LogTrace("Finding urls:");

            foreach (ServiceUrlModel url in tempUrls) 
            {
                mLogger.LogTrace("{serviceName}{serviceUrl}", url.ServiceName, url.ServiceUrl);
            }
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

        private void OnStarted()
        {
            mLogger.LogTrace("4. OnStarted has been called.");

            mLogger.LogInformation("Install: {install}", mAppArguments.Install);

            mLogger.LogInformation("Update: {update}", mAppArguments.Update);

            if (mAppArguments.Update)
            {
                mCheckingUpdateService.DownloadRepositoriesList();
                //await mCheckingUpdateService.CheckAndSaveUpdateListsAsync();
                //List<ServiceNameWithUrl> results = await mCheckingUpdateService.ReadServiceNameWithUrlListAsync();


                //mGitService.Clone(results.FirstOrDefault().ServiceInfoJsonUrl);
                /*foreach (var result in results)
                {
                    await mGitService.Clone(result.ServiceInfoJsonUrl);

                    mLogger.LogInformation("{name}", result.ServiceInfoServiceName);
                    mLogger.LogInformation("{version}", result.ServiceInfoJsonUrl);
                }*/
            }
        }

        private void OnStopping()
        {
            Unsubscribe();

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
