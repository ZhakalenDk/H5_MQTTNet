using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Server;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace H5_MQTTNet.Console
{
    public class MyMQTTClient : IDisposable
    {
        private MqttFactory _factory;
        private IMqttClient? _client;
        private Action<string>? _logger;

        public MyMQTTClient()
        {
            _factory = new MqttFactory();
            _client = _factory.CreateMqttClient();
        }

        public MyMQTTClient(Action<string> logger) : this()
        {
            _logger = logger;
        }

        public void Dispose()
        {
            _factory = null!;
            _client?.DisconnectAsync(new MqttClientDisconnectOptionsBuilder()
                .WithReason(MqttClientDisconnectOptionsReason.NormalDisconnection)
                .Build());
            _client = null;
            _logger = null;

            GC.SuppressFinalize(this);
        }

        public async Task<MqttClientConnectResultCode> Connect(string broker)
        {
            if (_client == null)
                throw new NullReferenceException("Client can't be null");

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(broker)
                .Build();

            _logger?.Invoke($"Connecting to: {broker}...");

            var result = await _client.ConnectAsync(mqttClientOptions, CancellationToken.None);

            _logger?.Invoke($"Connection status: {result.ResultCode}");

            return result.ResultCode;
        }
        public async Task<MqttClientConnectResultCode> ConnectWithTLS(string broker, string username, string password)
        {
            if (_client == null)
                throw new NullReferenceException("Client can't be null");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                throw new InvalidDataException("Username and password must be specified when using TLS");

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, 8883)
                .WithCredentials(username, password)
                .WithTls()
                .Build();

            _logger?.Invoke($"Connecting to: {broker} with TLS...");

            using var timeout = new CancellationTokenSource(5000);
            var result = await _client.ConnectAsync(mqttClientOptions, timeout.Token);

            _logger?.Invoke($"Connection status: {result.ResultCode}");

            return result.ResultCode;
        }

        public void HandleMessage(Action<string> handler)
        {
            if (_client == null)
                throw new NullReferenceException("Client can't be null");

            _client.ApplicationMessageReceivedAsync += (e) =>
            {
                _logger?.Invoke($"Message recieved:");

                handler(e.GetPayload());

                return Task.CompletedTask;
            };
        }

        public async Task SubAsync(string topic)
        {
            if (_factory == null)
                throw new NullReferenceException("Factory can't be null");

            if (_client == null)
                throw new NullReferenceException("Client can't be null");

            var mqttSubscribeOptions = _factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter((f) =>
                {
                    f.WithTopic(topic);
                })
            .Build();

            _logger?.Invoke($"Subscribing to: {topic}");
            var result = await _client.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
            _logger?.Invoke($"Sub status: {result.Items.First().ResultCode}");
        }

        public async Task PubAsync(string topic, string payload, Action<string>? logger = null)
        {
            if (topic == null)
                throw new NullReferenceException("Topic can't be null");

            if (payload == null)
                throw new NullReferenceException("Payload can't be null");

            var payloadBytes = Encoding.Default.GetBytes(payload);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payloadBytes)
                .Build();

            _logger?.Invoke($"Publishing message: {Environment.NewLine}{payload}");
            var result = await _client!.PublishAsync(message, CancellationToken.None);
            _logger?.Invoke($"Message status: {((result.IsSuccess) ? ("Sucess") : ("Failure"))}");
        }
    }
}