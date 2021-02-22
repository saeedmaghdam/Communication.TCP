using System.Net;
using Mabna.Communication.Tcp.Framework;
using Mabna.Communication.Tcp.TcpClient;
using Mabna.Communication.Tcp.TcpServer;
using Microsoft.Extensions.DependencyInjection;

namespace Mabna.Communication.Tcp.DependencyInjection.Microsoft
{
    public class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ITcpServerBuilder, TcpServerBuilder>();
            services.AddTransient<ITcpServer, TcpServer.TcpServer>();
            services.AddTransient<ITcpClientBuilder, TcpClientBuilder>();
            services.AddSingleton<ITcpClient, TcpClient.TcpClient>();
            services.AddTransient<IPacketParser, PacketProcessor.PacketParser>();
            services.AddTransient<IPacketProcessor, PacketProcessor.PacketProcessor>();
            services.Add(new ServiceDescriptor(typeof(PacketConfig), new PacketConfig(null, null)));
            services.Add(new ServiceDescriptor(typeof(SocketConfig), new SocketConfig(Dns.GetHostEntry(Dns.GetHostName()).AddressList[0], 0)));
            services.Add(new ServiceDescriptor(typeof(IPAddress), Dns.GetHostEntry(Dns.GetHostName()).AddressList[0]));
        }
    }
}
