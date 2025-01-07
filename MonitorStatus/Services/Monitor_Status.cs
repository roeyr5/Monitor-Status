using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using MonitorStatus.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MonitorStatus.Services
{
    public class Monitor_Status : IMonitorStatus
    {
        private static CancellationTokenSource source = new();
        private readonly KafkaSettings _kafkaSettings;
        private readonly IHubContext<MonitorHub> _hubContext;

        public Monitor_Status(IOptions<KafkaSettings> kafkaSettings, IHubContext<MonitorHub> hubContext)
        {
            _kafkaSettings = kafkaSettings.Value;
            _hubContext = hubContext;
        }

        public Task<OperationResult> Start()
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                GroupId = _kafkaSettings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(_kafkaSettings.Topic);
            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string active = "Active";
            string unactive = "Unactive";
            Task.Run(async() =>
            {
                while (!source.Token.IsCancellationRequested)
                {
                    var result = consumer.Consume();
                    consumer.Commit(result);

                    int packets = Int32.Parse(result.Message.Value);
                    if(packets > 0)
                    {
                        await _hubContext.Clients.All.SendAsync("MonitorActive", result.Partition, active);
                    }
                    else
                    {
                        await _hubContext.Clients.All.SendAsync("MonitorActive", result.Partition, unactive);
                    }

                }
            });
            return Task.FromResult(OperationResult.Success);
        }

        public Task<OperationResult> Stop()
        {

            return Task.FromResult(OperationResult.Success);
        }
        public void StartCheck()
        {
            if (source.Token.IsCancellationRequested)
            {
                source = new();
            }
        }

    }
}
