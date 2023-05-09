using H5_MQTTNet.RestApi.Services.DataContainers;
using H5_MQTTNet.RestApi.Services.Influx;
using MQTTnet.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Security.Policy;

namespace H5_MQTTNet.RestApi.Services.MQTT
{
    public class MQTTClientWorker : BackgroundService
    {
        private ILogger<MQTTClientWorker> _logger;
        private MyMQTTClient _client;
        private InfluxService _service;
        private readonly string _broker;
        private readonly string _username;
        private readonly string _password;
        private readonly List<TopicData> _subs;

        public MQTTClientWorker(ILogger<MQTTClientWorker> logger, IConfiguration configuration, MyMQTTClient client, InfluxService service)
        {
            _logger = logger;

            _broker = configuration["HiveMQ:Broker"];
            _username = configuration["HiveMQ:Credentials:Username"];
            _password = configuration["HiveMQ:Credentials:Password"];
            _subs = configuration
                .GetSection("Subs")
                .Get<List<TopicData>>();

            _client = client;
            _service = service;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var resultCode = await _client.ConnectWithTLS(_broker, _username, _password);
            if (resultCode != MqttClientConnectResultCode.Success)
                _logger.LogInformation(resultCode.ToString());

            foreach (var sub in _subs)
            {
                await _client.SubAsync(sub.Topic!, sub.QoS);
            }

            _client.HandleMessage(async (payload) =>
            {
                _logger.LogInformation("Payload: {Payload}", payload);
                Climate? climate = null;

                try
                {
                    climate = JsonConvert.DeserializeObject<Climate>(payload);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Data couldn't be deserialized");
                }

                if (climate == null)
                    return;

                await _service.WriteAsync("Climate", new("Topic", "home/climate"), new[]
                {
                    new KeyValuePair<string, string> (nameof (climate.Temperature), climate.Temperature.ToString("F", new CultureInfo("en-Us"))),
                    new KeyValuePair<string, string> (nameof (climate.Humidity), climate.Humidity.ToString("F", new CultureInfo("en-Us")))
                });
            });
        }
    }
}
