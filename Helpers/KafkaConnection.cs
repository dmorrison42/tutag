using System;
using System.Threading;
using System.Threading.Tasks;

using Confluent.Kafka;

namespace Tutag.Helpers
{
    /// <summary>
    /// KafkaConnection makes the single executable producer consumer model easier
    /// </summary>
    class KafkaConnection<K, V> : IDisposable
    {
        // TODO: Pull from app config
        private static string _bootstrapServers = "localhost:9092";
        private Task _consumerTask;

        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();
        public IProducer<K, V> Producer { get; }
        public IConsumer<K, V> Consumer { get; }
        public string Topic { get; }

        public KafkaConnection(string topic, string clientId, Action<ConsumeResult<K, V>> callback = null)
        {
            Topic = topic;
            Producer = new ProducerBuilder<K, V>(
                new ProducerConfig
                {
                    BootstrapServers = _bootstrapServers,
                    ClientId = clientId,
                }).Build();

            Consumer = new ConsumerBuilder<K, V>(
                new ConsumerConfig
                {
                    BootstrapServers = _bootstrapServers,
                    GroupId = System.Guid.NewGuid().ToString(),
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false,
                }).Build();
            Consumer.Subscribe(topic);

            _consumerTask = Task.Run(() =>
            {
                if (callback != null)
                {
                    while (!CancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            callback(Consumer.Consume(CancellationTokenSource.Token));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            });
        }

        public void Produce(K key, V value)
        {
            Producer.Produce(Topic, new Message<K, V>()
            {
                Key = key,
                Value = value,
            });
        }

        public Task<DeliveryResult<K, V>> ProduceAsync(K key, V value)
        {
            return Producer.ProduceAsync(Topic, new Message<K, V>()
            {
                Key = key,
                Value = value,
            });
        }


        public void Dispose()
        {
            CancellationTokenSource?.Cancel();
            Producer?.Dispose();
            Consumer?.Dispose();
        }
    }
}
