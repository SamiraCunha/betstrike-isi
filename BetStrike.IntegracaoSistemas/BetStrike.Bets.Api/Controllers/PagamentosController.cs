using BetStrike.Bets.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Bets.Api.Controllers
{
    [ApiController]
    [Route("api/pagamentos")]
    public class PagamentosController : ControllerBase
    {
        private readonly DbConnectionFactory _connectionFactory;

        public PagamentosController(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        [HttpPost("depositar")]
        public IActionResult Depositar([FromBody] DepositoRequest request)
        {
            if (request == null)
                return BadRequest("Corpo da requisição é obrigatório.");

            if (request.Valor <= 0)
                return BadRequest("O valor de depósito deve ser maior que zero.");

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "Pagamentos.dbo.spDepositarSaldo";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@UtilizadorId", request.UtilizadorId));
            command.Parameters.Add(new SqlParameter("@Valor", request.Valor));

            try
            {
                command.ExecuteNonQuery();
                return NoContent();
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    public class DepositoRequest
    {
        public int UtilizadorId { get; set; }
        public decimal Valor { get; set; }
    }
}