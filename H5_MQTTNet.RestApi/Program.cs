using H5_MQTTNet.RestApi.Services.DataContainers;
using H5_MQTTNet.RestApi.Services.Influx;
using H5_MQTTNet.RestApi.Services.MQTT;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<InfluxService>();
builder.Services.AddSingleton((provider) =>
{
    return new MyMQTTClient(Console.WriteLine);
});
builder.HookMQTTWorker();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("ReadTest", async ([FromQuery] string bucket, InfluxService service) =>
{
    var data = await service.ReadAsync(bucket);

    return data;
});

app.MapGet("Climate", (InfluxService service) =>
{
    var data = service.Read<Climate>("OiskiBucket");

    return data;
});

app.MapPost("window", async ([FromBody] string status, MyMQTTClient client, IConfiguration configuration) =>
{
    int statusInt = ((status.ToUpperInvariant() == "ON") ? (180) : (0));

    var topicData = configuration
    .GetSection("Pubs")
    .GetSection("HomeWindow")
    .Get<TopicData>();

    await client.PubAsync(topicData.Topic!, statusInt.ToString(), topicData.QoS);
});

app.MapPost("LED", async ([FromBody] string status, MyMQTTClient client, IConfiguration configuration) =>
{
    int statusInt = ((status.ToUpperInvariant() == "ON") ? (1) : (0));

    var topicData = configuration
    .GetSection("Pubs")
    .GetSection("HomeLED")
    .Get<TopicData>();

    await client.PubAsync(topicData.Topic!, statusInt.ToString(), topicData.QoS);
});

app.MapPost("WriteTest", async ([FromBody] double data, InfluxService service) =>
{
    await service.WriteAsync("Test", new KeyValuePair<string, string>("Location", "WebAPI"), new[]
    {
        new KeyValuePair<string, string>("Temperature", data.ToString()),
        new KeyValuePair<string, string> ("Humidity", data.ToString())
    });
});

app.MapPost("PublishTest", async ([FromBody] Climate climateData, MyMQTTClient client) =>
{
    var climateJson = JsonConvert.SerializeObject(climateData);

    await client.PubAsync("home/climate", climateJson);
});

app.Run();