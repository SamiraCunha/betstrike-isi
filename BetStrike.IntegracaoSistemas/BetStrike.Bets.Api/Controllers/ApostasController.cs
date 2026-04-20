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

            using var command = connection.CreateCommand();
            command.CommandText = "spInserirAposta";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@Codigo_Jogo", request.Codigo_Jogo));
            command.Parameters.Add(new SqlParameter("@UtilizadorId", request.UtilizadorId));
            command.Parameters.Add(new SqlParameter("@Tipo_Aposta", request.Tipo_Aposta));
            command.Parameters.Add(new SqlParameter("@Montante", request.Montante));
            command.Parameters.Add(new SqlParameter("@Odd_Momento", request.Odd_Momento));

            try
            {
                var result = command.ExecuteScalar();
                var novaApostaId = Convert.ToInt32(result);

                return CreatedAtAction(nameof(ObterApostaPorId),
                    new { id = novaApostaId },
                    new { Id = novaApostaId });
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
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