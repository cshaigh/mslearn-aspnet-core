using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace eShopOnContainers.WebSPA
{
    public class Program
    {
        public static Task Main(string[] args) =>
            CreateHostBuilder(args).Build().RunAsync();

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
                .UseContentRoot(Directory.GetCurrentDirectory())

                // Register a configuration provider for the Azure App Configuration store
                .ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = configBuilder.Build();

                    if (settings.GetValue<bool>("UseFeatureManagement") &&
                        !string.IsNullOrEmpty(settings["AppConfig:Endpoint"]))
                    {
                        // Register configuration provider
                        configBuilder.AddAzureAppConfiguration(configOptions =>
                        {
                            var cacheTime = TimeSpan.FromSeconds(5);

                            configOptions

                                // Authenticate to Azure App Configuration with AppConfig:Endpoint Connection String (stored as AppConfig__Endpoint helm env variable)
                                .Connect(settings["AppConfig:Endpoint"])

                                // Enable feature flags support
                                .UseFeatureFlags(flagOptions =>
                                {
                                    // Cache feature flags for cacheTime (5 seconds)
                                    // NB: default cache expiry is 30 seconds
                                    flagOptions.CacheExpirationInterval = cacheTime;
                                })

                                // Configure refresh options for specific App Configuration keys
                                .ConfigureRefresh(refreshOptions =>
                                {
                                    // Cache FeatureManagement:Coupons feature flag key for cacheTime (5 seconds)
                                    // NB: default cache expiry is 30 seconds
                                    // ...although perhaps earlier override for all feature flags (5 seconds) will be used?
                                    refreshOptions
                                        .Register("FeatureManagement:Coupons", refreshAll: true)
                                        .SetCacheExpiration(cacheTime);
                                });
                        });
                    }                    
                })

                .ConfigureLogging((hostingContext, logBuilder) =>
                {
                    logBuilder.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logBuilder.AddConsole();
                    logBuilder.AddDebug();
                    logBuilder.AddAzureWebAppDiagnostics();
                })

                .UseSerilog((builderContext, config) =>
                {
                    config
                        .MinimumLevel.Information()
                        .Enrich.FromLogContext()
                        .WriteTo.Seq("http://seq")
                        .ReadFrom.Configuration(builderContext.Configuration)
                        .WriteTo.Console();
                });
    }
}
