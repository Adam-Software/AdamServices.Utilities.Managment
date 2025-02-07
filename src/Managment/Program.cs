using Managment.Core.Services;
using Managment.Core.SpectreConsole.Commands;
using Managment.Core.SpectreConsole.Infrastructure;
using Managment.Interface;
using Managment.Services.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Serilog;
using Serilog.Core;
using Spectre.Console.Cli;
using System;
using System.Threading.Tasks;

namespace Managment
{
    internal class Program
    {
        static int Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.Sources.Clear();
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    })
                    
                    .ConfigureServices((context, services) =>
                    {
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

                        services.AddSingleton<ICheckingUpdateService, CheckingUpdateService>();
                        
                        services.AddCommandLine<DefaultCommand>(config =>
                        {
                            
                            //config.SetApplicationName("Adam Service Managment");
                            //config.SetApplicationVersion("1.0");
                        });

                        services.AddHostedService<ProgramHostedService>();
                    })

                    .Build();
            
            host.WaitForShutdown();
            return host.Run(args);
            

        }
    }
}
