using BetStrike.Bets.Api.Data;
using BetStrike.Bets.Api.Models;
using BetStrike.Bets.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Bets.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApostasController : ControllerBase
    {
        private readonly DbConnectionFactory _connectionFactory;

        public ApostasController(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // POST api/apostas
        [HttpPost]
        public IActionResult CriarAposta([FromBody] CriarApostaRequest request)
        {
            if (request == null)
                return BadRequest("Corpo da requisição é obrigatório.");

            if (string.IsNullOrWhiteSpace(request.Codigo_Jogo))
                return BadRequest("Codigo Jogo é obrigatório.");

            if (string.IsNullOrWhiteSpace(request.Tipo_Aposta))
                return BadRequest("Tipo Aposta é obrigatório.");

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                int novaApostaId;
                int jogoId = 0;
                string tipoAposta = request.Tipo_Aposta;

                // 1) Inserir aposta na BD Apostas
                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = "spInserirAposta";
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@Codigo_Jogo", request.Codigo_Jogo));
                    command.Parameters.Add(new SqlParameter("@UtilizadorId", request.UtilizadorId));
                    command.Parameters.Add(new SqlParameter("@Tipo_Aposta", request.Tipo_Aposta));
                    command.Parameters.Add(new SqlParameter("@Montante", request.Montante));
                    command.Parameters.Add(new SqlParameter("@Odd_Momento", request.Odd_Momento));

                    var result = command.ExecuteScalar();
                    novaApostaId = Convert.ToInt32(result);
                }

                // Buscar JogoId para enviar ao RabbitMQ
                using (var cmdJogo = connection.CreateCommand())
                {
                    cmdJogo.Transaction = transaction;
                    cmdJogo.CommandText = "SELECT JogoId FROM Aposta WHERE Id = @Id";
                    cmdJogo.Parameters.Add(new SqlParameter("@Id", novaApostaId));
                    jogoId = Convert.ToInt32(cmdJogo.ExecuteScalar());
                }

                // 2) Debitar saldo na BD Pagamentos
                using (var cmdPag = connection.CreateCommand())
                {
                    cmdPag.Transaction = transaction;
                    cmdPag.CommandText = "Pagamentos.dbo.spDebitarAposta";
                    cmdPag.CommandType = CommandType.StoredProcedure;
                    cmdPag.Parameters.Add(new SqlParameter("@ApostaId", novaApostaId));
                    cmdPag.Parameters.Add(new SqlParameter("@UtilizadorId", request.UtilizadorId));
                    cmdPag.Parameters.Add(new SqlParameter("@Valor", request.Montante));

                    cmdPag.ExecuteNonQuery();
                }

                // Commit da transação
                transaction.Commit();

                // ✅ ENVIAR PARA RABBITMQ APÓS COMMIT
                var mensagem = new ApostaBackgroundMessage
                {
                    ApostaId = novaApostaId,
                    JogoId = jogoId,
                    TipoAposta = tipoAposta,
                    Montante = request.Montante,
                    OddMomento = request.Odd_Momento,
                    Estado = 1, // Pendente
                    DataHoraRegisto = DateTime.Now
                };

                var producer = new ApostasBackgroundProducer();
                producer.EnviarAposta(mensagem);

                return CreatedAtAction(nameof(ObterApostaPorId),
                    new { id = novaApostaId },
                    new { Id = novaApostaId, Mensagem = "Aposta criada e saldo debitado." });
            }
            catch (SqlException ex)
            {
                transaction.Rollback();
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                transaction.Rollback();
                return StatusCode(500, "Erro ao criar aposta.");
            }
        }

        // GET api/apostas/{id}
        [HttpGet("{id:int}")]
        public IActionResult ObterApostaPorId(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "spObterApostaPorId";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@ApostaId", id));

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return NotFound();

            var aposta = new
            {
                Id = reader.GetInt32(0),
                Codigo_Jogo = reader.GetString(1),
                UtilizadorId = reader.GetInt32(2),
                Tipo_Aposta = reader.GetString(3),
                Montante = reader.GetDecimal(4),
                Odd_Momento = reader.GetDecimal(5),
                Estado = reader.GetInt32(6),
                DataHora_Registo = reader.GetDateTime(7)
            };

            return Ok(aposta);
        }

        // GET api/apostas/utilizador/5
        [HttpGet("utilizador/{utilizadorId:int}")]
        public IActionResult ObterApostasPorUtilizador(int utilizadorId)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "spObterApostasPorUtilizador";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@UtilizadorId", utilizadorId));

            using var reader = command.ExecuteReader();

            var apostas = new List<object>();

            while (reader.Read())
            {
                var aposta = new
                {
                    Id = reader.GetInt32(0),
                    Codigo_Jogo = reader.GetString(1),
                    UtilizadorId = reader.GetInt32(2),
                    Tipo_Aposta = reader.GetString(3),
                    Montante = reader.GetDecimal(4),
                    Odd_Momento = reader.GetDecimal(5),
                    Estado = reader.GetInt32(6),
                    DataHora_Registo = reader.GetDateTime(7)
                };

                apostas.Add(aposta);
            }

            if (apostas.Count == 0)
                return NotFound($"Nenhuma aposta encontrada para o utilizador {utilizadorId}.");

            return Ok(apostas);
        }


        // DELETE api/apostas/{id}
        [HttpDelete("{id:int}")]
        public IActionResult CancelarAposta(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "spCancelarAposta";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@ApostaId", id));

            try
            {
                command.ExecuteNonQuery();
                return NoContent(); // 204
            }
            catch (SqlException ex)
            {
                // Mensagens da RAISERROR na SP vêm em ex.Message
                return BadRequest(ex.Message);
            }
        }


    }
}