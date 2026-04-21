using System.Data;
using BetStrike.Bets.Api.Data;
using BetStrike.Bets.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

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

            // Agora vamos usar uma transação, porque vamos mexer em Apostas e Pagamentos
            using var transaction = connection.BeginTransaction();

            try
            {
                int novaApostaId;

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

                    // Se a tua SP já devolve o Id por SELECT SCOPE_IDENTITY(),
                    // podes continuar a usar ExecuteScalar. Se devolve por OUTPUT, adapta.
                    var result = command.ExecuteScalar();
                    novaApostaId = Convert.ToInt32(result);
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

                // Se chegarmos aqui, correu tudo bem -> commit
                transaction.Commit();

                return CreatedAtAction(nameof(ObterApostaPorId),
                    new { id = novaApostaId },
                    new { Id = novaApostaId, Mensagem = "Aposta criada e saldo debitado." });
            }
            catch (SqlException ex)
            {
                // Se falhar ou a SP de Pagamentos lançar 'Saldo insuficiente', fazemos rollback
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