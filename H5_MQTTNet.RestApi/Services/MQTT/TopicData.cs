using MQTTnet.Protocol;

namespace H5_MQTTNet.RestApi.Services.MQTT
{
    public class TopicData
    {
        public string? Topic { get; set; }
        public MqttQualityOfServiceLevel QoS { get; set; }
    }
}
