using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleGet.AWS.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddS3StorageService(this IServiceCollection services, IConfiguration configuration)
        {
            var aws = configuration.GetAWSOptions();
            services.AddSingleton(
              sp =>
              {
                  if (aws.Profile == null && aws.Region == null)
                  {
                      return new AmazonS3Client();
                  }
                  return aws.CreateServiceClient<IAmazonS3>();
              });

            services.AddTransient<S3StorageService>();

            return services;
        }
    }
}
