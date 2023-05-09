using InfluxDB.Client;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Linq;
using System.Text;

namespace H5_MQTTNet.RestApi.Services.Influx
{
    public class InfluxService
    {
        private readonly ILogger<InfluxService> _logger;
        private readonly string _bucketName;
        private readonly string _orgId;
        private readonly string _url;
        private readonly string _token;

        public InfluxService(ILogger<InfluxService> logger, IConfiguration configuration)
        {
            _bucketName = configuration["Influx:BucketName"];
            _orgId = configuration["Influx:OrgId"];
            _url = configuration["Influx:Url"];
            _token = configuration["Influx:Token"];

            _logger = logger;
        }

        public async Task WriteAsync(string table, KeyValuePair<string, string> tag, params KeyValuePair<string, string>[] data)
        {
            _logger.LogInformation($"Writing data to: {_bucketName}");
            using var client = new InfluxDBClient(_url, _token);

            var writeApi = client.GetWriteApiAsync();

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                builder.Append($"{data[i].Key}={data[i].Value}{((i == data.Length - 1) ? (string.Empty) : (","))}");
            }

            await writeApi.WriteRecordAsync($"{table},{tag.Key}={tag.Value} {builder}", InfluxDB.Client.Api.Domain.WritePrecision.Ns, _bucketName, _orgId);
            _logger.LogInformation("Data Written");
        }

        public async Task<Dictionary<string, List<string>>> ReadAsync(string bucket)
        {
            var data = new Dictionary<string, List<string>>();
            var flux = $"from(bucket:\"{bucket}\") |> range (start: 0)";

            using var client = new InfluxDBClient(_url, _token);

            var fluxTables = await client.GetQueryApi()
                .QueryAsync(flux, _orgId);
            fluxTables.ForEach(fluxTable =>
                {
                    var table = fluxTable;
                    var fluxRecords = fluxTable.Records;
                    fluxRecords.ForEach(fluxRecord =>
                    {
                        if (!data.ContainsKey(fluxRecord.GetField()))
                            data.Add(fluxRecord.GetField(), new List<string>());

                        data[fluxRecord.GetField()].Add(fluxRecord.GetValue().ToString()!);
                    });
                });

            return data;
        }

        public List<TType> Read<TType>(string bucket)
        {
            using var client = new InfluxDBClient(_url, _token);
            var queryApi = client.GetQueryApiSync();

            var query = from s in InfluxDBQueryable<TType>.Queryable(_bucketName, _orgId, queryApi) select s;

            return query.ToList();
        }
    }
}
