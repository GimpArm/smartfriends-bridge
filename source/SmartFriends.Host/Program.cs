using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SmartFriends.Api;
using SmartFriends.Api.JsonConvertes;
using SmartFriends.Api.Models;
using SmartFriends.Mqtt;
using SmartFriends.Mqtt.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole().AddDebug();

builder.Services.Configure<Configuration>(builder.Configuration.GetSection("SmartFriends"));
builder.Services.Configure<MqttConfiguration>(builder.Configuration.GetSection("Mqtt"));
builder.Services.AddSingleton(x => new Session(x.GetService<IOptions<Configuration>>().Value, x.GetService<ILogger<Session>>()));
builder.Services.AddSingleton(x => new TypeTemplateEngine(x.GetService<IOptions<MqttConfiguration>>()?.Value));
builder.Services.AddSingleton(x => new MqttClient(x.GetService<IOptions<MqttConfiguration>>()?.Value, x.GetService<ILogger<MqttClient>>(), x.GetService<TypeTemplateEngine>()));
builder.Services.AddHostedService(x => x.GetService<Session>());
builder.Services.AddHostedService<MqttClientService>();
builder.Services.AddControllers().AddNewtonsoftJson(x =>
{
    x.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    x.SerializerSettings.Formatting = Formatting.Indented;
    x.SerializerSettings.Converters = new List<JsonConverter> { new FuzzyValueConverter() };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.MapControllers();
app.Run();