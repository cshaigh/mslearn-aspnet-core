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
            // Register default configuration providers
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
                .UseContentRoot(Directory.GetCurrentDirectory())

                // Register a configuration provider for the Azure App Configuration store
                .ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var settings = configBuilder.Build();

                    // Register configuration provider
                    configBuilder.AddAzureAppConfiguration(configOptions =>
                        configOptions

                            // Authenticate to Azure App Configuration with AppConfig:Endpoint Connection String
                            .Connect(settings["AppConfig:Endpoint"])

                            // Enable feature flags support
                            .UseFeatureFlags());
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
