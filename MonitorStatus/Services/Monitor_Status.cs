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

        private readonly Dictionary<string, Dictionary<int, int>> messageCounts = new();

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

            Task.Run(() =>
            {
                while (!source.Token.IsCancellationRequested)
                {
                    var result = consumer.Consume();
                    consumer.Commit(result);

                    string topic = result.Topic;
                    int partition = result.Partition.Value;

                    if (!messageCounts.ContainsKey(topic))
                    {
                        messageCounts[topic] = new Dictionary<int, int>();
                    }

                    if (!messageCounts[topic].ContainsKey(partition))
                    {
                        messageCounts[topic][partition] = 0;
                    }

                    messageCounts[topic][partition]++;
                }
            });

            Task.Run(async () =>
            {
                while (!source.Token.IsCancellationRequested)
                {
                    await Task.Delay(3000); 
                    await BroadcastUpdate();
                }
            });

            return Task.FromResult(OperationResult.Success);
        }

        private async Task BroadcastUpdate()
        {
            await _hubContext.Clients.All.SendAsync("UpdateMessageCounts", messageCounts);
            //ResetValues();
        }
        private void ResetValues()
        {
            foreach (var outkey in messageCounts)
            {
                foreach (var key in outkey.Value.Keys.ToList())
                {
                    outkey.Value[key] = 0;
                }
            }
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
