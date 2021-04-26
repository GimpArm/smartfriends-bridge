using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SmartFriends.Api;
using SmartFriends.Api.JsonConvertes;
using SmartFriends.Api.Models;
using SmartFriends.Mqtt;
using SmartFriends.Mqtt.Models;

namespace SmartFriends.Host
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Configuration>(Configuration.GetSection("SmartFriends"));
            services.Configure<MqttConfiguration>(Configuration.GetSection("Mqtt"));
            services.AddSingleton(x => new Session(x.GetService<IOptions<Configuration>>().Value, x.GetService<ILogger<Session>>()));
            services.AddSingleton(x => new TypeTemplateEngine(x.GetService<IOptions<MqttConfiguration>>()?.Value));
            services.AddSingleton(x => new MqttClient(x.GetService<IOptions<MqttConfiguration>>()?.Value, x.GetService<ILogger<MqttClient>>(), x.GetService<TypeTemplateEngine>()));
            services.AddHostedService(x => x.GetService<Session>());
            services.AddHostedService<MqttClientService>();
            services.AddControllers().AddNewtonsoftJson(x =>
            {
                x.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                x.SerializerSettings.Formatting = Formatting.Indented;
                x.SerializerSettings.Converters = new List<JsonConverter> {new FuzzyValueConverter()};
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
