using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using BetStrike.Bets.Api.Models;

namespace BetStrike.Bets.Api.Services
{
    public class ApostasBackgroundProducer
    {
        private readonly ConnectionFactory _factory;
        private const string QueueName = "apostas_background";

        public ApostasBackgroundProducer()
        {
            _factory = new ConnectionFactory()
            {
                HostName = "localhost"
            };
        }

        public void EnviarAposta(ApostaBackgroundMessage msg)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var json = JsonSerializer.Serialize(msg);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(
                    exchange: "",
                    routingKey: QueueName,
                    basicProperties: properties,
                    body: body);

                Console.WriteLine($"[Producer] Aposta {msg.ApostaId} enviada para fila");
            }
        }
    }
}