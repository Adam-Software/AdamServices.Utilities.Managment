using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using System;

namespace Managment.Core.SpectreConsole.Infrastructure
{
    public static class HostingExtensions
    {
        public static IServiceCollection AddCommandLine(this IServiceCollection services,Action<IConfigurator> configurator)
        {
            var app = new CommandApp(new TypeRegistrar(services));
            app.Configure(configurator);
            services.AddSingleton<ICommandApp>(app);

            return services;
        }

        public static IServiceCollection AddCommandLine<TDefaultCommand>(this IServiceCollection services, Action<IConfigurator> configurator) where TDefaultCommand : class, ICommand
        {
            var app = new CommandApp<TDefaultCommand>(new TypeRegistrar(services));
            app.Configure(configurator);
            services.AddSingleton<ICommandApp>(app);

            return services;
        }

        public static int Run(this IHost host, string[] args)
        {
            ArgumentNullException.ThrowIfNull(host);

            var app = host.Services.GetService<ICommandApp>();

            if (app == null)
            {
                throw new InvalidOperationException("Command application has not been configured.");
            }

            return app.Run(args);
        }
    }
}
