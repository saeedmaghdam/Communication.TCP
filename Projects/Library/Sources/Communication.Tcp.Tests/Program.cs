using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.IO;

namespace Communication.Tcp.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            return Host.CreateDefaultBuilder()
                .UseSerilog((hostBuilderContext, loggerConfiguration) =>
                {
                    loggerConfiguration.ReadFrom.Configuration(configuration)
                        .Enrich.FromLogContext()
                        .Enrich.With<ActivityEnricher>()
                        .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                        .WriteTo.Seq(configuration.GetSection("SeqUrl").Value);
                }).ConfigureServices((hostBuilderContext, services) =>
                {
                    Mabna.Communication.Tcp.DependencyInjection.Microsoft.Startup.ConfigureServices(services);
                    services.AddHostedService<Worker>();
                });
        }
    }
}
