using Confluent.Kafka;
using System;
using System.Text.Json;

Console.WriteLine("[Streaming] Iniciando consumidor de jogos finalizados...");

var config = new ConsumerConfig
{
    BootstrapServers = "localhost:9092",
    GroupId = "betstrike-stats-consumer",
    AutoOffsetReset = AutoOffsetReset.Earliest
};

using var consumer = new ConsumerBuilder<string, string>(config).Build();

consumer.Subscribe("jogos-finalizados");

try
{
    while (true)
    {
        var cr = consumer.Consume();

        Console.WriteLine($"[Streaming] Evento recebido. Key={cr.Message.Key}");

        var evento = JsonSerializer.Deserialize<JogoFinalizadoEvent>(cr.Message.Value);

        if (evento != null)
        {
            string descricaoEstado = evento.Estado switch
            {
                3 => "finalizado",
                4 => "cancelado",
                5 => "adiado",
                _ => $"estado {evento.Estado}"
            };

            Console.WriteLine(
                $"[Streaming] Jogo {descricaoEstado}: {evento.Codigo_Jogo} " +
                $"- {evento.GolosCasa} x {evento.GolosFora} " +
                $"em {evento.DataHoraFinalizacao:O}");
        }
    }
}
catch (OperationCanceledException)
{
    consumer.Close();
}

public class JogoFinalizadoEvent
{
    public int JogoId { get; set; }
    public string Codigo_Jogo { get; set; } = string.Empty;
    public int GolosCasa { get; set; }
    public int GolosFora { get; set; }
    public int Estado { get; set; }
    public DateTime DataHoraFinalizacao { get; set; }
}