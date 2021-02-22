using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            return Host.CreateDefaultBuilder().ConfigureServices((hostBuilderContext, services) =>
            {
                Mabna.Communication.Tcp.DependencyInjection.Microsoft.Startup.ConfigureServices(services);
                services.AddHostedService<Worker>();
            });
        }
    }
}
