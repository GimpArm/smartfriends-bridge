using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartFriends.Api;
using SmartFriends.Api.Models;
using SmartFriends.Mqtt.Models;

namespace SmartFriends.Mqtt
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory())
                        .AddEnvironmentVariables("ASPNETCORE_")
                        .AddCommandLine(args);
                })
                .ConfigureAppConfiguration((hostContext, configApp) =>
                {
                    configApp.SetBasePath(Directory.GetCurrentDirectory())
                        .AddEnvironmentVariables("ASPNETCORE_")
                        .AddJsonFile("appsettings.json", true)
                        .AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging()
                        .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information)
                        .Configure<Configuration>(hostContext.Configuration.GetSection("SmartFriends"))
                        .Configure<MqttConfiguration>(hostContext.Configuration.GetSection("Mqtt"))
                        .AddSingleton(x => new Session(x.GetService<IOptions<Configuration>>()?.Value, x.GetService<ILogger<Session>>()))
                        .AddSingleton(x => new TypeTemplateEngine(x.GetService<IOptions<MqttConfiguration>>()?.Value))
                        .AddSingleton(x => new MqttClient(x.GetService<IOptions<MqttConfiguration>>()?.Value, x.GetService<ILogger<MqttClient>>(), x.GetService<TypeTemplateEngine>()))
                        .AddHostedService<ApplicationService>();
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConsole()
                        .AddDebug();
                })
                .Build();

            await host.RunAsync();
        }
    }
}