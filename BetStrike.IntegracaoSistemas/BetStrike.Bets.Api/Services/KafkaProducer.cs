using BetStrike.Bets.Api.Models;
using Confluent.Kafka;
using System.Text.Json;

public interface IKafkaProducer
{
    Task PublicarJogoFinalizadoAsync(JogoFinalizadoEvent evento);
}

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public KafkaProducer(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _topic = configuration["Kafka:TopicJogoFinalizado"] ?? "jogos-finalizados";
    }

    public async Task PublicarJogoFinalizadoAsync(JogoFinalizadoEvent evento)
    {
        var key = evento.Codigo_Jogo;
        var value = JsonSerializer.Serialize(evento);

        await _producer.ProduceAsync(_topic, new Message<string, string>
        {
            Key = key,
            Value = value
        });
    }
}