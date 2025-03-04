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
        private static CancellationTokenSource source = new CancellationTokenSource();
        private readonly KafkaSettings _kafkaSettings;
        private readonly IHubContext<MonitorHub> _hubContext;

        private readonly object messageCountsLock = new object();
        private readonly Dictionary<int, Dictionary<int, int>> messageCounts = new();

        private List<string> subscribedTopics = new List<string>();
        private IConsumer<Ignore, string> consumer;
        private ConsumerConfig config;

        public Monitor_Status(IOptions<KafkaSettings> kafkaSettings, IHubContext<MonitorHub> hubContext)
        {
            _kafkaSettings = kafkaSettings.Value;
            _hubContext = hubContext;

            config = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                GroupId = _kafkaSettings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        }

        public async Task<OperationResult> Subscribe(string topic)
        {

            if (consumer == null)
                return OperationResult.Failed;

            if (!subscribedTopics.Contains(topic))
            {
                subscribedTopics.Add(topic);
                source.Cancel();
                consumer.Subscribe(subscribedTopics);
            }

            Console.WriteLine("Subscribed to new topic : " +topic);
            await Start();

            return OperationResult.Success;
        }

        public Task<OperationResult> Start()
        {
            Console.WriteLine("Starting stream active/unactive comm");
            source = new CancellationTokenSource();

            Task.Run(() =>
            {
                while (!source.Token.IsCancellationRequested)
                {
                    try
                    {
                        Console.WriteLine("test");
                        var result = consumer.Consume();
                        consumer.Commit(result);

                        int topic = 0;
                        Int32.TryParse(result.Topic,out topic);
                        int partition = result.Partition.Value;

                        lock (messageCountsLock)
                        {
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
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    
                        
                }
            },source.Token);

            Task.Run(async () =>
            {
                while (!source.Token.IsCancellationRequested)
                {
                    await BroadcastUpdate();
                    await Task.Delay(3000, source.Token); 
                }
            }, source.Token);



            return Task.FromResult(OperationResult.Success);
        }

        private async Task BroadcastUpdate()
        {
            await _hubContext.Clients.All.SendAsync("UpdateMessageCounts", messageCounts);
            ResetValues();
        }
        private void ResetValues()
        {
            lock (messageCountsLock)
            {
                foreach (var outkey in messageCounts)
                {
                    foreach (var key in outkey.Value.Keys.ToList())
                    {
                        outkey.Value[key] = 0;
                    }
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
