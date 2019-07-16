using SimpleGet.AWS;
using SimpleGet.AWS.Configuration;
using SimpleGet.AWS.Extensions;
using SimpleGet.Core.Authentication;
using SimpleGet.Core.Configuration;
using SimpleGet.Core.Extensions;
using SimpleGet.Core.Indexing;
using SimpleGet.Core.Mirror;
using SimpleGet.Core.Search;
using SimpleGet.Core.Server.Extensions;
using SimpleGet.Core.State;
using SimpleGet.Core.Storage;
using SimpleGet.DataBase.Mongo;
using SimpleGet.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using SimpleGet.Core.Entities;

namespace SimpleGet.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureSimpleGet(this IServiceCollection services, IConfiguration configuration,
            bool httpServices = false)
        {
            services.ConfigureAndValidate<SimpleGetOptions>(configuration);
            services.ConfigureAndValidate<SearchOptions>(configuration.GetSection(nameof(SimpleGetOptions.Search)));
            services.ConfigureAndValidate<MirrorOptions>(configuration.GetSection(nameof(SimpleGetOptions.Mirror)));
            services.ConfigureAndValidate<StorageOptions>(configuration.GetSection(nameof(SimpleGetOptions.Storage)));
            services.ConfigureAndValidate<DatabaseOptions>(configuration.GetSection(nameof(SimpleGetOptions.Database)));
            services.ConfigureAndValidate<FileSystemStorageOptions>(configuration.GetSection(nameof(SimpleGetOptions.Storage)));

            services.ConfigureAws(configuration);
            services.ConfigureDataBase(configuration);

            if (httpServices)
            {
                services.ConfigureHttpServices();
            }

            services.AddTransient<IPackageService, PackageService>();
            services.AddTransient<IPackageIndexingService, PackageIndexingService>();
            services.AddTransient<IPackageDeletionService, PackageDeletionService>();
            services.AddTransient<ISymbolIndexingService, SymbolIndexingService>();
            services.AddSingleton<IFrameworkCompatibilityService, FrameworkCompatibilityService>();
            services.AddMirrorServices();

            services.AddStorageProviders(configuration);
            services.AddSearchProviders();
            services.AddAuthenticationProviders();

            return services;
        }

        public static IServiceCollection ConfigureDataBase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IDataBaseContext, MongoDatabaseContext>();
            return services;
        }

        public static IServiceCollection ConfigureAws(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureAndValidate<S3StorageOptions>(configuration.GetSection(nameof(SimpleGetOptions.Storage)));
            return services;
        }

        public static IServiceCollection AddStorageProviders(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<NullStorageService>();
            services.AddTransient<FileStorageService>();
            services.AddTransient<IPackageStorageService, PackageStorageService>();
            services.AddTransient<ISymbolStorageService, SymbolStorageService>();

            services.AddS3StorageService(configuration);

            services.AddTransient<IStorageService>(provider =>
            {
                var options = provider.GetRequiredService<IOptionsSnapshot<SimpleGetOptions>>();

                switch (options.Value.Storage.Type)
                {
                    case StorageType.FileSystem:
                        return provider.GetRequiredService<FileStorageService>();

                    case StorageType.AwsS3:
                        return provider.GetRequiredService<S3StorageService>();

                    case StorageType.Null:
                        return provider.GetRequiredService<NullStorageService>();

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported storage service: {options.Value.Storage.Type}");
                }
            });

            return services;
        }

        public static IServiceCollection AddSearchProviders(this IServiceCollection services)
        {
            services.AddTransient<ISearchService>(provider =>
            {
                var options = provider.GetRequiredService<IOptionsSnapshot<SearchOptions>>();

                switch (options.Value.Type)
                {
                    case SearchType.Database:
                        return provider.GetRequiredService<DatabaseSearchService>();

                    case SearchType.Null:
                        return provider.GetRequiredService<NullSearchService>();

                    default:
                        throw new InvalidOperationException($"Unsupported search service: {options.Value.Type}");
                }
            });

            services.AddTransient<DatabaseSearchService>();
            services.AddSingleton<NullSearchService>();

            return services;
        }

        /// <summary>
        /// Add the services that mirror an upstream package source.
        /// </summary>
        /// <param name="services">The defined services.</param>
        public static IServiceCollection AddMirrorServices(this IServiceCollection services)
        {
            services.AddTransient<FakeMirrorService>();
            services.AddTransient<MirrorService>();

            services.AddTransient<IMirrorService>(provider =>
            {
                var options = provider.GetRequiredService<IOptionsSnapshot<MirrorOptions>>();

                if (!options.Value.Enabled)
                {
                    return provider.GetRequiredService<FakeMirrorService>();
                }
                else
                {
                    return provider.GetRequiredService<MirrorService>();
                }
            });

            services.AddTransient<IPackageContentClient, PackageContentClient>();
            services.AddTransient<IRegistrationClient, RegistrationClient>();
            services.AddTransient<IServiceIndexClient, ServiceIndexClient>();
            services.AddTransient<IPackageMetadataService, PackageMetadataService>();

            services.AddSingleton<IServiceIndexService>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<MirrorOptions>>();
                var serviceIndexClient = provider.GetRequiredService<IServiceIndexClient>();

                return new ServiceIndexService(
                    options.Value.PackageSource.ToString(),
                    serviceIndexClient);
            });

            services.AddTransient<IPackageDownloader, PackageDownloader>();

            services.AddSingleton(provider =>
            {
                var options = provider.GetRequiredService<IOptions<SimpleGetOptions>>().Value;

                var assembly = Assembly.GetEntryAssembly();
                var assemblyName = assembly.GetName().Name;
                var assemblyVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

                var client = new HttpClient(new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                });

                client.DefaultRequestHeaders.Add("User-Agent", $"{assemblyName}/{assemblyVersion}");
                client.Timeout = TimeSpan.FromSeconds(options.Mirror.PackageDownloadTimeoutSeconds);

                return client;
            });

            services.AddSingleton<DownloadsImporter>();
            services.AddSingleton<IPackageDownloadsSource, PackageDownloadsJsonSource>();

            return services;
        }

        public static IServiceCollection AddAuthenticationProviders(this IServiceCollection services)
        {
            services.AddTransient<IAuthenticationService, ApiKeyAuthenticationService>();

            return services;
        }
    }
}
