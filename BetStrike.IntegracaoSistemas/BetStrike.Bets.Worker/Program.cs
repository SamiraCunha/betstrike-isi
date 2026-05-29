using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Data.SqlClient;
using Dapper;

Console.WriteLine("[Worker] Iniciando worker de estatísticas...");

var factory = new ConnectionFactory() 
{ 
    HostName = "localhost" 
};

using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare(
        queue: "apostas_background",
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    var consumer = new EventingBasicConsumer(channel);

    consumer.Received += async (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var json = Encoding.UTF8.GetString(body);
        var mensagem = JsonSerializer.Deserialize<ApostaBackgroundMessage>(json)!;

        Console.WriteLine($"[Worker] Processando aposta {mensagem.ApostaId} do jogo {mensagem.JogoId}");

        await AtualizarEstatisticas(mensagem);
    };

    channel.BasicConsume(
        queue: "apostas_background",
        autoAck: true,
        consumer: consumer);

    Console.WriteLine("[Worker] Aguardando mensagens... Pressiona Enter para sair.");
    Console.ReadLine();
}

async Task AtualizarEstatisticas(ApostaBackgroundMessage mensagem)
{
    var connectionString = "Server=DESKTOP-CC87B9U\\MEIBI2026;Database=Apostas;Trusted_Connection=True;TrustServerCertificate=True;";
    
    using var dbConnection = new SqlConnection(connectionString);
    
    var sql = @"
        IF NOT EXISTS (SELECT 1 FROM EstatisticasJogo WHERE JogoId = @JogoId)
        BEGIN
            INSERT INTO EstatisticasJogo (JogoId, Total_Apostado, Numero_Apostas,
                Apostas_Tipo1, Apostas_TipoX, Apostas_Tipo2,
                Valor_Apostas_Tipo1, Valor_Apostas_TipoX, Valor_Apostas_Tipo2,
                DataHora_UltimaAtualizacao)
            VALUES (@JogoId, 0, 0, 0, 0, 0, 0, 0, 0, GETDATE())
        END
        
        UPDATE EstatisticasJogo
        SET 
            Total_Apostado = Total_Apostado + @Montante,
            Numero_Apostas = Numero_Apostas + 1,
            Apostas_Tipo1 = Apostas_Tipo1 + CASE WHEN @TipoAposta = '1' THEN 1 ELSE 0 END,
            Apostas_TipoX = Apostas_TipoX + CASE WHEN @TipoAposta = 'X' THEN 1 ELSE 0 END,
            Apostas_Tipo2 = Apostas_Tipo2 + CASE WHEN @TipoAposta = '2' THEN 1 ELSE 0 END,
            Valor_Apostas_Tipo1 = Valor_Apostas_Tipo1 + CASE WHEN @TipoAposta = '1' THEN @Montante ELSE 0 END,
            Valor_Apostas_TipoX = Valor_Apostas_TipoX + CASE WHEN @TipoAposta = 'X' THEN @Montante ELSE 0 END,
            Valor_Apostas_Tipo2 = Valor_Apostas_Tipo2 + CASE WHEN @TipoAposta = '2' THEN @Montante ELSE 0 END,
            DataHora_UltimaAtualizacao = GETDATE()
        WHERE JogoId = @JogoId";
    
    await dbConnection.ExecuteAsync(sql, new
    {
        JogoId = mensagem.JogoId,
        TipoAposta = mensagem.TipoAposta,
        Montante = mensagem.Montante
    });
    
    Console.WriteLine($"[Worker] Estatísticas atualizadas para jogo {mensagem.JogoId}");
}

public class ApostaBackgroundMessage
{
    public int ApostaId { get; set; }
    public int JogoId { get; set; }
    public string TipoAposta { get; set; } = string.Empty;
    public decimal Montante { get; set; }
    public decimal OddMomento { get; set; }
    public int Estado { get; set; }
    public DateTime DataHoraRegisto { get; set; }
}