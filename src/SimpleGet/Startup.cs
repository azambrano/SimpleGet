using System;
using SimpleGet.AWS.Extensions;
using SimpleGet.Configuration;
using SimpleGet.Core.Configuration;
using SimpleGet.Core.Entities;
using SimpleGet.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleGet.DataBase.Mongo;

namespace SimpleGet
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureSimpleGet(Configuration, httpServices: true);

            // In production, the UI files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "SimpleGet.UI/build";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseStatusCodePages();
            }

            // Run migrations if necessary.
            var options = Configuration.Get<SimpleGetOptions>();

            app.UsePathBase(options.PathBase);
            app.UseForwardedHeaders();
            app.UseSpaStaticFiles();

            app.UseCors(ConfigureCorsOptions.CorsPolicy);

            app.UseMvc(routes =>
            {
                routes
                    .MapServiceIndexRoutes()
                    .MapPackagePublishRoutes()
                    .MapSymbolRoutes()
                    .MapSearchRoutes()
                    .MapRegistrationRoutes()
                    .MapPackageContentRoutes();
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "../SimpleGet.UI";

                if (env.IsDevelopment())
                {
                    spa.UseReactDevelopmentServer(npmScript: "start");
                }
            });
        }
    }
}
