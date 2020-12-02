using System;
using System.Threading;
using System.Threading.Tasks;

using Confluent.Kafka;
using Newtonsoft.Json.Linq;

namespace Tutag.Helpers
{
    /// <summary>
    /// KafkaConnection makes the single executable producer consumer model easier
    /// </summary>
    class KafkaConnection<K, V> : IDisposable
    {
        private class NewtonsoftJsonSerializer<O> : Confluent.Kafka.ISerializer<O>
        {
            public byte[] Serialize(O data, SerializationContext context)
            {
                return System.Text.Encoding.UTF8.GetBytes(JToken.FromObject(data).ToString());
            }
        }

        private class NewtonsoftJsonDeserializer<O> : Confluent.Kafka.IDeserializer<O>
        {
            public O Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
            {
                if (isNull) return default(O);
                var text = System.Text.Encoding.UTF8.GetString(data);
                return JToken.Parse(text).ToObject<O>();
            }
        }

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
                })
                    .SetValueSerializer(new NewtonsoftJsonSerializer<V>())
                    .Build();

            Consumer = new ConsumerBuilder<K, V>(
                new ConsumerConfig
                {
                    BootstrapServers = _bootstrapServers,
                    GroupId = System.Guid.NewGuid().ToString(),
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false,
                })
                    .SetValueDeserializer(new NewtonsoftJsonDeserializer<V>())

                .Build();
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
