using CommandLine;
using Managment.Core.Services;
using Managment.Interface;
using Managment.Services.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Threading.Tasks;

namespace Managment
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.Sources.Clear();
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    })

                    .ConfigureServices((context, services) =>
                    {
                        _ = Parser.Default.ParseArguments<AppArguments>(args)
                            .WithParsed(appArgs =>
                            {
                                services.AddSingleton<IAppArguments>(appArgs);
                            });
                 
                        AppSettingsOptionsService options = new();
                        context.Configuration.GetRequiredSection("AppSettingsOptions").Bind(options);

                        services.AddSingleton<IAppSettingsOptionsService>(options);

                        services.AddLogging(loggingBuilder =>
                        {
                            Logger logger = new LoggerConfiguration()
                                    .ReadFrom.Configuration(context.Configuration)
                                    .CreateLogger();

                            loggingBuilder.ClearProviders();
                            loggingBuilder.AddSerilog(logger, dispose: true);
                        });

                        services.Configure<HostOptions>(option =>
                        {
                            option.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
                            option.ShutdownTimeout = TimeSpan.FromSeconds(5);
                        });

                        services.AddSingleton<IJsonTempFilesWorkerService, JsonTempFilesWorkerService>();
                        services.AddSingleton<IGitHubCilentService, GitHubCilentService>();
                        services.AddSingleton<IUpdateService, UpdateService>();
                        services.AddSingleton<IDownloadService, DownloadService>();
                        services.AddSingleton<IDotnetService, DotnetService>();

                        services.AddHostedService<ProgramHostedService>();
                    })

                    .Build();

            await host.RunAsync();
            
                
        }
    }
}
