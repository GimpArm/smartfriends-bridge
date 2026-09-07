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
builder.Services.AddSingleton<Session>();
builder.Services.AddSingleton<TypeTemplateEngine>();
builder.Services.AddSingleton<MqttClient>();
builder.Services.AddHostedService(x => x.GetRequiredService<Session>());
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