using BetStrike.Bets.Api.Data;
using BetStrike.Bets.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Net.Http;

namespace BetStrike.Bets.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JogosController : ControllerBase
    {
        private readonly DbConnectionFactory _connectionFactory;
        private readonly IHttpClientFactory _httpClientFactory;

        public JogosController(DbConnectionFactory connectionFactory, IHttpClientFactory httpClientFactory)
        {
            _connectionFactory = connectionFactory;
            _httpClientFactory = httpClientFactory;
        }

        // POST api/jogos/sincronizar-resultado/{codigoJogo}
        [HttpPost("sincronizar-e-resolver/{codigoJogo}")]
        public async Task<IActionResult> SincronizarEResolver(string codigoJogo)
        {
            // 1) Buscar dados do jogo na Plataforma de Resultados
            var client = _httpClientFactory.CreateClient("ResultadosApi");

            JogoResultadosDto jogoResultados;
            try
            {
                jogoResultados = await client
                    .GetFromJsonAsync<JogoResultadosDto>($"api/jogos/{codigoJogo}");

                if (jogoResultados == null)
                    return NotFound($"Jogo {codigoJogo} não encontrado na Plataforma de Resultados.");
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, $"Erro ao contactar Plataforma de Resultados: {ex.Message}");
            }

            // 2) Validar estado vindo da Results.Api (aceitar 3, 4, 5)
            if (jogoResultados.Estado is not (3 or 4 or 5))
            {
                return BadRequest(
                    "Só é possível sincronizar e resolver apostas para jogos em estado Finalizado (3), Cancelado (4) ou Adiado (5).");
            }

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 3) Atualizar estado do jogo na BD Apostas
                using (var cmdEstado = connection.CreateCommand())
                {
                    cmdEstado.Transaction = transaction;
                    cmdEstado.CommandText = "spAtualizarEstadoJogo";
                    cmdEstado.CommandType = CommandType.StoredProcedure;
                    cmdEstado.Parameters.Add(new SqlParameter("@Codigo_Jogo", codigoJogo));
                    cmdEstado.Parameters.Add(new SqlParameter("@NovoEstado", jogoResultados.Estado));
                    cmdEstado.ExecuteNonQuery();
                }

                // 4) Se jogo estiver Finalizado (3), garantir Resultado
                if (jogoResultados.Estado == 3)
                {
                    using (var cmdResultado = connection.CreateCommand())
                    {
                        cmdResultado.Transaction = transaction;
                        cmdResultado.CommandText = "spInserirResultado";
                        cmdResultado.CommandType = CommandType.StoredProcedure;
                        cmdResultado.Parameters.Add(new SqlParameter("@Codigo_Jogo", codigoJogo));
                        cmdResultado.Parameters.Add(new SqlParameter("@Golos_Casa", jogoResultados.Golos_Casa));
                        cmdResultado.Parameters.Add(new SqlParameter("@Golos_Fora", jogoResultados.Golos_Fora));
                        cmdResultado.ExecuteNonQuery();
                    }
                }

                // 5) Resolver apostas (sp já trata diferentemente 3 vs 4/5)
                using (var cmdResolver = connection.CreateCommand())
                {
                    cmdResolver.Transaction = transaction;
                    cmdResolver.CommandText = "spResolverApostasDoJogo";
                    cmdResolver.CommandType = CommandType.StoredProcedure;
                    cmdResolver.Parameters.Add(new SqlParameter("@Codigo_Jogo", codigoJogo));
                    cmdResolver.ExecuteNonQuery();
                }

                transaction.Commit();

                return Ok(new
                {
                    Codigo_Jogo = codigoJogo,
                    Estado_Resultados = jogoResultados.Estado,
                    jogoResultados.Golos_Casa,
                    jogoResultados.Golos_Fora,
                    Mensagem = jogoResultados.Estado switch
                    {
                        3 => "Jogo finalizado: resultado sincronizado e apostas resolvidas.",
                        4 => "Jogo cancelado: apostas pendentes anuladas e reembolsadas.",
                        5 => "Jogo adiado: apostas pendentes anuladas e reembolsadas.",
                        _ => "Operação concluída."
                    }
                });
            }
            catch (SqlException ex)
            {
                transaction.Rollback();
                return BadRequest(ex.Message);
            }
            catch
            {
                transaction.Rollback();
                return StatusCode(500, "Erro interno ao sincronizar resultado e resolver apostas.");
            }
        }

        // POST api/jogos
        // Insere jogo vindo da Plataforma de Resultados
        [HttpPost]
        public IActionResult InserirJogo([FromBody] CriarJogoRequest request)
        {
            if (request == null)
                return BadRequest("Corpo da requisição é obrigatório.");

            if (string.IsNullOrWhiteSpace(request.Codigo_Jogo))
                return BadRequest("Codigo_Jogo é obrigatório.");

            if (request.Estado < 1 || request.Estado > 5)
                return BadRequest("Estado inválido (1..5).");

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "spInserirJogo";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add(new SqlParameter("@Codigo_Jogo", request.Codigo_Jogo));
            command.Parameters.Add(new SqlParameter("@DataHora_Inicio", request.DataHora_Inicio));
            command.Parameters.Add(new SqlParameter("@Equipa_Casa", request.Equipa_Casa));
            command.Parameters.Add(new SqlParameter("@Equipa_Fora", request.Equipa_Fora));
            command.Parameters.Add(new SqlParameter("@Tipo_Competicao", request.Tipo_Competicao));
            command.Parameters.Add(new SqlParameter("@Estado", request.Estado));

            try
            {
                var result = command.ExecuteScalar();
                var novoId = Convert.ToInt32(result);

                return CreatedAtAction(nameof(ObterJogoPorCodigo),
                    new { codigo = request.Codigo_Jogo },
                    new { Id = novoId, request.Codigo_Jogo });
            }
            catch (SqlException ex)
            {
                
                return BadRequest(ex.Message);
            }
        }

        // GET api/jogos/{codigo}
        [HttpGet("{codigo}")]
        public IActionResult ObterJogoPorCodigo(string codigo)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "spObterJogoPorCodigo";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@Codigo_Jogo", codigo));

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return NotFound();

            var jogo = new
            {
                Id = reader.GetInt32(0),
                Codigo_Jogo = reader.GetString(1),
                DataHora_Inicio = reader.GetDateTime(2),
                Equipa_Casa = reader.GetString(3),
                Equipa_Fora = reader.GetString(4),
                Tipo_Competicao = reader.GetString(5),
                Estado = reader.GetInt32(6)
            };

            return Ok(jogo);
        }

        // GET api/jogosDisponiveis
        [HttpGet("disponiveis")]
        public IActionResult ObterJogosDisponiveis()
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "spObterJogosDisponiveis";
            command.CommandType = CommandType.StoredProcedure;
            using var reader = command.ExecuteReader();
            var jogos = new List<object>();
            while (reader.Read())
            {
                jogos.Add(new
                {
                    Id = reader.GetInt32(0),
                    Codigo_Jogo = reader.GetString(1),
                    DataHora_Inicio = reader.GetDateTime(2),
                    Equipa_Casa = reader.GetString(3),
                    Equipa_Fora = reader.GetString(4),
                    Tipo_Competicao = reader.GetString(5),
                    Estado = reader.GetInt32(6)
                });
            }
            return Ok(jogos);
        }



        // PUT api/jogos/{codigo}
        [HttpPut("{codigo}")]
        public IActionResult AtualizarEstadoJogo(string codigo, [FromBody] AtualizarJogoRequest request)
        {
            if (request == null)
                return BadRequest("Corpo da requisição é obrigatório.");

            if (request.Estado < 1 || request.Estado > 5)
                return BadRequest("Estado inválido (1..5).");

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            // 1) Atualizar estado do jogo via SP
            using (var cmdAtualizar = connection.CreateCommand())
            {
                cmdAtualizar.CommandText = "spAtualizarEstadoJogo";
                cmdAtualizar.CommandType = CommandType.StoredProcedure;
                cmdAtualizar.Parameters.Add(new SqlParameter("@Codigo_Jogo", codigo));
                cmdAtualizar.Parameters.Add(new SqlParameter("@NovoEstado", request.Estado));

                try
                {
                    cmdAtualizar.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    return BadRequest(ex.Message);
                }
            }

            // 2) Se estado for Finalizado (3), Cancelado (4) ou Adiado (5),
            //    chamar SP que resolve / anula apostas pendentes
            if (request.Estado is 3 or 4 or 5)
            {
                using var cmdResolver = connection.CreateCommand();
                cmdResolver.CommandText = "spResolverApostasDoJogo";
                cmdResolver.CommandType = CommandType.StoredProcedure;
                cmdResolver.Parameters.Add(new SqlParameter("@Codigo_Jogo", codigo));

                try
                {
                    cmdResolver.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    // Se der erro aqui, já atualizámos o estado do jogo; devolvemos 400 com detalhe
                    return BadRequest($"Estado atualizado, mas ocorreu erro ao resolver apostas: {ex.Message}");
                }
            }

            // 3) Devolver o jogo atualizado (reutilizando o GET por código)
            return ObterJogoPorCodigo(codigo);
        }

       

    }
}