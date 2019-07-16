using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SimpleGet.Extensions
{
    // See https://github.com/aspnet/MetaPackages/blob/master/src/Microsoft.AspNetCore/WebHost.cs
    public static class IHostBuilderExtensions
    {
        public static IHostBuilder ConfigureSimpleGetConfiguration(this IHostBuilder builder, string[] args)
        {
            return builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddEnvironmentVariables();

                config
                    .SetBasePath(Environment.CurrentDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            });
        }

        public static IHostBuilder ConfigureSimpleGetLogging(this IHostBuilder builder)
        {
            return builder
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConfiguration(context.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                });
        }

        public static IHostBuilder ConfigureSimpleGetServices(this IHostBuilder builder)
        {
            return builder
                .ConfigureServices((context, services) => services.ConfigureSimpleGet(context.Configuration));
        }
    }
}
