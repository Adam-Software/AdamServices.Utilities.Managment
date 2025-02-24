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
using System.Threading.Tasks;

namespace Managment
{
    internal class Program
    {
        static Task Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.Sources.Clear();
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    })
                    
                    .ConfigureServices((context, services) =>
                    {
                        Parser.Default.ParseArguments<AppArguments>(args).WithParsed(appArgs =>
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

                        services.AddSingleton<IUpdateService, UpdateService>();

                        services.AddHostedService<ProgramHostedService>();
                    })

                    .Build();
            
            
            return host.RunAsync();
            

        }
    }
}
