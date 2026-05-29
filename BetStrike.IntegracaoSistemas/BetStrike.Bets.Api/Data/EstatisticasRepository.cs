using System.Data;
using Dapper;
using BetStrike.Bets.Api.Models;

namespace BetStrike.Bets.Api.Data
{
    public class EstatisticasRepository
    {
        private readonly DbConnectionFactory _dbFactory;

        public EstatisticasRepository(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task AtualizarEstatisticas(ApostaBackgroundMessage mensagem)
        {
            using var connection = _dbFactory.CreateConnection();

            // Verifica se já existe estatística para o jogo
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

            await connection.ExecuteAsync(sql, new
            {
                JogoId = mensagem.JogoId,
                TipoAposta = mensagem.TipoAposta,
                Montante = mensagem.Montante
            });
        }

        public async Task<EstatisticasJogo?> ObterPorJogoId(int jogoId)
        {
            using var connection = _dbFactory.CreateConnection();

            var sql = "SELECT * FROM EstatisticasJogo WHERE JogoId = @JogoId";

            return await connection.QueryFirstOrDefaultAsync<EstatisticasJogo>(sql, new { JogoId = jogoId });
        }
    }
}