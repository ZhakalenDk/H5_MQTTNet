using MQTTnet.Client;
using System.Text;
using System.Text.Json;

namespace H5_MQTTNet.Console
{
    public static class Extensions
    {
        public static TObject DumpToConsole<TObject>(this TObject obj)
        {
            var output = "NULL";
            if (obj != null)
                output = JsonSerializer.Serialize(obj, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            System.Console.WriteLine($"[{obj?.GetType().Name}]:\r\n{output}");

            return obj;
        }

        public static void DumpToConsole(this MqttApplicationMessageReceivedEventArgs args)
        {
            System.Console.WriteLine(args.GetPayload());
        }

        public static string GetPayload(this MqttApplicationMessageReceivedEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args), "Args can't be null");
            var payloadBytes = args.ApplicationMessage.PayloadSegment.Array;

            if (payloadBytes == null)
                throw new NullReferenceException("PayloadBytes can't be null");

            var payload = Encoding.Default.GetString(payloadBytes!);

            return payload;
        }
    }
}
