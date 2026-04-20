using Microsoft.AspNetCore.Mvc;
using BetStrike.Results.Api.Data;
using BetStrike.Results.Api.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BetStrike.Results.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JogosController : ControllerBase
    {
        private readonly DbConnectionFactory _connectionFactory;

        public JogosController(DbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }


        // GET: api/jogos
        [HttpGet]
        public IActionResult ListarJogos([FromQuery] DateTime? data, [FromQuery] int? estado)
        {
           using var connection = _connectionFactory.CreateConnection();
            connection.Open();
            
            var sql = @"SELECT Id,Codigo_Jogo, Data_Jogo, Hora_Inicio,
       Equipa_Casa, Equipa_Fora, Golos_Casa, Golos_Fora, Estado
                    FROM Jogos
                    WHERE 1 = 1"; // 1=1 é um truque para facilitar a construção dinâmica da query

            var command = connection.CreateCommand();

            if (data.HasValue)
            {
                sql += " AND Data_Jogo = @Data_Jogo";
                command.Parameters.Add(new SqlParameter("@Data_Jogo", data.Value.Date));
            }

            if (estado.HasValue)
            {
                sql += " AND Estado = @Estado";
                command.Parameters.Add(new SqlParameter("@Estado", estado.Value));
            }

            command.CommandText = sql;

            using var reader = command.ExecuteReader();

            var resultados = new List<Jogo>();

            while (reader.Read())
            {
                var jogo = new Jogo
                {
                    Id = reader.GetInt32(0),
                    Codigo_Jogo = reader.GetString(1),
                    Data_Jogo = reader.GetDateTime(2),
                    Hora_Inicio = (TimeSpan)reader.GetValue(3),
                    Equipa_Casa = reader.GetString(4),
                    Equipa_Fora = reader.GetString(5),
                    Golos_Casa = reader.GetInt32(6),
                    Golos_Fora = reader.GetInt32(7),
                    Estado = reader.GetInt32(8)
                };
                resultados.Add(jogo);
            }
            return Ok(resultados);

        }


        // POST api/<JogosController>
        [HttpPost]
        public IActionResult CriarJogo([FromBody] CriarJogoRequest request)
        {
            //Validações basicas em memoria antes de ir para a base de dados
            if (string.IsNullOrWhiteSpace(request.Codigo_Jogo))
                return BadRequest("O código do jogo é obrigatório.");

            if (request.Estado < 1 || request.Estado > 5)
                return BadRequest("O estado do jogo deve ser um valor entre 1 e 5.");

            if (string.IsNullOrWhiteSpace(request.Equipa_Casa) ||
                    string.IsNullOrWhiteSpace(request.Equipa_Fora))
                return BadRequest("A equipa de casa/fora é obrigatória.");

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"INSERT INTO Jogos (Codigo_Jogo, Data_Jogo, Hora_Inicio, Equipa_Casa, Equipa_Fora, Golos_Casa, Golos_Fora, Estado)
                        OUTPUT INSERTED.Id
                        VALUES (@Codigo_Jogo, @Data_Jogo, @Hora_Inicio, @Equipa_Casa, @Equipa_Fora, @Golos_Casa, @Golos_Fora, @Estado)";
                command.Parameters.Add(new SqlParameter("@Codigo_Jogo", SqlDbType.VarChar) { Value = request.Codigo_Jogo });
                command.Parameters.Add(new SqlParameter("@Data_Jogo", SqlDbType.Date) { Value = request.Data_Jogo });
                command.Parameters.Add(new SqlParameter("@Hora_Inicio", SqlDbType.Time) { Value = request.Hora_Inicio });
                command.Parameters.Add(new SqlParameter("@Equipa_Casa", SqlDbType.VarChar) { Value = request.Equipa_Casa });
                command.Parameters.Add(new SqlParameter("@Equipa_Fora", SqlDbType.VarChar) { Value = request.Equipa_Fora });
                command.Parameters.Add(new SqlParameter("@Golos_Casa", SqlDbType.Int) { Value = request.Golos_Casa });
                command.Parameters.Add(new SqlParameter("@Golos_Fora", SqlDbType.Int) { Value = request.Golos_Fora });
                command.Parameters.Add(new SqlParameter("@Estado", SqlDbType.Int) { Value = request.Estado });

                var newId = (int)command.ExecuteScalar();

                var jogoCriado = new Jogo
                {
                    Id = newId,
                    Codigo_Jogo = request.Codigo_Jogo,
                    Data_Jogo = request.Data_Jogo,
                    Hora_Inicio = request.Hora_Inicio,
                    Equipa_Casa = request.Equipa_Casa,
                    Equipa_Fora = request.Equipa_Fora,
                    Golos_Casa = request.Golos_Casa,
                    Golos_Fora = request.Golos_Fora,
                    Estado = request.Estado
                };
                return CreatedAtAction(nameof(ObterJogoPorCodigo), new { codigo = jogoCriado.Codigo_Jogo }, jogoCriado);
            }
            catch (SqlException ex) when (ex.Number == 2627) // violação de unique index
            {
                return Conflict("Já existe um jogo com esse Codigo_Jogo.");
            }

        }
        // GET api/jogos/{codigo}
        [HttpGet("{codigo}")]

        public IActionResult ObterJogoPorCodigo(string codigo)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"SELECT Id, Codigo_Jogo, Data_Jogo, Hora_Inicio, Equipa_Casa, Equipa_Fora, Golos_Casa, Golos_Fora, Estado
                                        FROM Jogos
                                        WHERE Codigo_Jogo = @Codigo_Jogo;";
                
                command.Parameters.Add(new SqlParameter("@Codigo_Jogo", codigo));
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var jogo = new Jogo
                    {
                        Id = reader.GetInt32(0),
                        Codigo_Jogo = reader.GetString(1),
                        Data_Jogo = reader.GetDateTime(2),
                        Hora_Inicio = (TimeSpan)reader.GetValue(3),
                        Equipa_Casa = reader.GetString(4),
                        Equipa_Fora = reader.GetString(5),
                        Golos_Casa = reader.GetInt32(6),
                        Golos_Fora = reader.GetInt32(7),
                        Estado = reader.GetInt32(8)
                    };
                    return Ok(jogo);
                }
                else
                {
                    return NotFound("Jogo não encontrado.");
                }
            }
            catch (Exception ex)
            {
                // Log do erro aqui
                return StatusCode(500, "Ocorreu um erro ao acessar a base de dados.");
            }
        }

     
        // PUT api/jogos/{codigo}
        [HttpPut("{codigo}")]
        public IActionResult AtualizarJogo(string codigo, [FromBody] AtualizarJogoRequest request)
        {
            if (request == null)
                return BadRequest("Corpo da requisição é obrigatório.");

            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            // 1) Buscar estado atual e golos atuais
            using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
                                    SELECT Id, Golos_Casa, Golos_Fora, Estado
                                    FROM Jogos
                                    WHERE Codigo_Jogo = @Codigo_Jogo;";
            selectCmd.Parameters.Add(new SqlParameter("@Codigo_Jogo", codigo));

            using var reader = selectCmd.ExecuteReader();
            if (!reader.Read())
                return NotFound("Jogo não encontrado.");

            var id = reader.GetInt32(0);
            var golosCasaAtual = reader.GetInt32(1);
            var golosForaAtual = reader.GetInt32(2);
            var estadoAtual = reader.GetInt32(3);

            reader.Close();

            // 2) Determinar novos valores
            var novoGolosCasa = request.Golos_Casa ?? golosCasaAtual;
            var novoGolosFora = request.Golos_Fora ?? golosForaAtual;
            var novoEstado = request.Estado ?? estadoAtual;

            if (novoEstado < 1 || novoEstado > 5)
                return BadRequest("Estado inválido. Deve estar entre 1 e 5.");

            if (!TransicaoEstadoValida(estadoAtual, novoEstado))
                return BadRequest($"Transição de estado inválida: {estadoAtual} -> {novoEstado}.");

            // 3) Atualizar na BD
            using var updateCmd = connection.CreateCommand();
            updateCmd.CommandText = @"
                UPDATE Jogos
                SET Golos_Casa = @Golos_Casa,
                    Golos_Fora = @Golos_Fora,
                    Estado = @Estado
                WHERE Id = @Id;";
            updateCmd.Parameters.Add(new SqlParameter("@Golos_Casa", novoGolosCasa));
            updateCmd.Parameters.Add(new SqlParameter("@Golos_Fora", novoGolosFora));
            updateCmd.Parameters.Add(new SqlParameter("@Estado", novoEstado));
            updateCmd.Parameters.Add(new SqlParameter("@Id", id));

            var linhas = updateCmd.ExecuteNonQuery();
            if (linhas == 0)
                return StatusCode(500, "Falha ao atualizar o jogo.");

            // 4) Devolver o jogo atualizado
            return ObterJogoPorCodigo(codigo);
        }

        private bool TransicaoEstadoValida(int estadoAtual, int novoEstado)
        {
            // 1=Agendado, 2=Em Curso, 3=Finalizado, 4=Cancelado, 5=Adiado
            if (estadoAtual == novoEstado) return true;

            return (estadoAtual, novoEstado) switch
            {
                (1, 2) => true,  // Agendado -> Em Curso
                (2, 3) => true,  // Em Curso -> Finalizado
                (1, 4) => true,  // Agendado -> Cancelado
                (1, 5) => true,  // Agendado -> Adiado
                _ => false
            };
        }

        // DELETE api/jogos/{codigo}
        [HttpDelete("{codigo}")]
        public IActionResult RemoverJogo(String codigo)
        {
            using var connection = _connectionFactory.CreateConnection();
            connection.Open();

            // 1) Buscar estado atual
            using var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = @"
                                    SELECT Id, Estado
                                    FROM Jogos
                                    WHERE Codigo_Jogo = @Codigo_Jogo;";
            selectCmd.Parameters.Add(new SqlParameter("@Codigo_Jogo", codigo));

            using var reader = selectCmd.ExecuteReader();
            if (!reader.Read())
                return NotFound("Jogo não encontrado.");

            var id = reader.GetInt32(0);
            var estadoAtual = reader.GetInt32(1);

            reader.Close();

            if (estadoAtual != 1) // Só pode remover se estiver Agendado (estado=1)
                return BadRequest("Só é permitido remover jogos no estado Agendado");

            // 2) Remover o jogo
            using var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Jogos WHERE Id = @Id;";
            deleteCmd.Parameters.Add(new SqlParameter("@Id", id));

            var linhas = deleteCmd.ExecuteNonQuery();
            if (linhas == 0)
                return StatusCode(500, "Falha ao remover o jogo.");

            return NoContent(); // 204
        }
    }



}
