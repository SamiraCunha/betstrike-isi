using BetStrike.DataGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BetStrike.DataGenerator
{
    internal class Program
    {

        public class JogoResponse
        {
            public string Codigo_Jogo { get; set; }
            public DateTime Data_Jogo { get; set; }
            public TimeSpan Hora_Inicio { get; set; }
            public string Equipa_Casa { get; set; }
            public string Equipa_Fora { get; set; }
            public int Estado { get; set; }
        }

        class SimulacaoJogo
        {
            public string Codigo { get; set; }
            public string EquipaCasa { get; set; }
            public string EquipaFora { get; set; }
            public int GolosCasa { get; set; }
            public int GolosFora { get; set; }
            public int Estado { get; set; } = 1; // começa Agendado
        }
        static async Task Main(string[] args)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7286") // API Resultados
            };

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== BetStrike DataGenerator ===\n");
                Console.WriteLine("1) Publicar calendário (criar jogos agendados)");
                Console.WriteLine("2) Simular jogos existentes (em curso/finalizar)");
                Console.WriteLine("0) Sair\n");
                Console.Write("Escolha uma opção: ");
                var opcao = Console.ReadLine();

                switch (opcao)
                {
                    case "1":
                        await PublicarCalendarioAsync(httpClient);
                        break;
                    case "2":
                        await SimularTodosJogosAsync(httpClient);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Opção inválida.");
                        break;
                }

                Console.WriteLine("\nENTER para voltar ao menu...");
                Console.ReadLine();
            }
        }
        static async Task PublicarCalendarioAsync(HttpClient httpClient)
        {
            Console.Write("\nNúmero da jornada (ex: 1, 2, 3): ");
            var input = Console.ReadLine();
            if (!int.TryParse(input, out int numeroJornada) || numeroJornada <= 0)
            {
                Console.WriteLine("Número de jornada inválido.");
                return;
            }

            var equipas = new List<string>
            {
                "FC Porto", "SL Benfica", "Sporting CP", "SC Braga",
                "Vitória SC", "Boavista", "Gil Vicente", "Famalicão",
                "Rio Ave", "Estoril", "Portimonense", "Casa Pia",
                "Farense", "Moreirense", "Arouca", "Vizela",
                "Chaves", "Estrela da Amadora"
            };

            var random = new Random();
            int ano = DateTime.Now.Year;
            var dataJornada = new DateTime(ano, 9, 15).AddDays((numeroJornada - 1) * 7); // por ex: 1 semana entre jornadas
            var horaBase = new TimeSpan(20, 0, 0);

            // Baralhar e emparelhar — cada equipa aparece exatamente uma vez
            var emparelhamentos = GerarEmparelhamentos(random, equipas);

            Console.WriteLine($"\nA publicar calendário — Jornada {numeroJornada} ({dataJornada:dd/MM/yyyy})\n");

            for (int i = 0; i < emparelhamentos.Count; i++)
            {
                var (casa, fora) = emparelhamentos[i];
                var codigo = GerarCodigo(ano, numeroJornada, i + 1);
                var hora = horaBase.Add(TimeSpan.FromMinutes(15 * i));

                var request = new CriarJogoRequest
                {
                    Codigo_Jogo = codigo,
                    Data_Jogo = dataJornada,
                    Hora_Inicio = hora,
                    Equipa_Casa = casa,
                    Equipa_Fora = fora,
                    Golos_Casa = 0,
                    Golos_Fora = 0,
                    Estado = 1 // Agendado
                };

                Console.Write($"  A criar {codigo}: {casa} vs {fora} ... ");

                var response = await httpClient.PostAsJsonAsync("/api/jogos", request);

                if (response.IsSuccessStatusCode)
                    Console.WriteLine("OK");
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"ERRO {(int)response.StatusCode}: {body}");
                }
            }

            Console.WriteLine("\nCalendário publicado. Todos os jogos estão no estado Agendado (1).");
        }

        // ────────────────────────────────────────────────────────────────────────
        // FASE 2 — Simular todos os jogos agendados em paralelo
        // ────────────────────────────────────────────────────────────────────────

        static async Task SimularTodosJogosAsync(HttpClient httpClient)
        {
            // Vai buscar à API os jogos no estado Agendado (1)
            Console.WriteLine("\nA obter jogos agendados da API...");

            List<JogoResponse> jogos;

            try
            {
                // Tenta usar o filtro por estado; se a API não suportar query string,
                // obtém todos e filtra localmente
                // Vai buscar TODOS os jogos (sem filtro)
                var resposta = await httpClient.GetAsync("/api/jogos");

                if (!resposta.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Erro ao obter jogos: {resposta.StatusCode}");
                    return;
                }

                jogos = await resposta.Content.ReadFromJsonAsync<List<JogoResponse>>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exceção ao obter jogos: {ex.Message}");
                return;
            }

            // Filtrar localmente para garantir apenas Agendados
            jogos = jogos?.Where(j => j.Estado == 1).ToList();

            if (jogos == null || jogos.Count == 0)
            {
                Console.WriteLine("Não há jogos agendados para simular. Publique o calendário primeiro (opção 1).");
                return;
            }

            Console.WriteLine($"\nEncontrados {jogos.Count} jogo(s) agendados. A iniciar simulação em paralelo...\n");

            // Converter para SimulacaoJogo
            var simulacoes = jogos.Select(j => new SimulacaoJogo
            {
                Codigo = j.Codigo_Jogo,
                EquipaCasa = j.Equipa_Casa,
                EquipaFora = j.Equipa_Fora,
                GolosCasa = 0,
                GolosFora = 0,
                Estado = 1
            }).ToList();

            // Correr TODOS os jogos em paralelo — Task.WhenAll
            var tarefas = simulacoes.Select(jogo => SimularJogoAsync(httpClient, jogo));
            await Task.WhenAll(tarefas);

            Console.WriteLine("\nTodos os jogos foram finalizados.");
        }

        // ────────────────────────────────────────────────────────────────────────
        // Simulação de um jogo individual
        // ────────────────────────────────────────────────────────────────────────
        static async Task SimularJogoAsync(HttpClient httpClient, SimulacaoJogo jogo)
        {
            var random = new Random(Guid.NewGuid().GetHashCode()); // seed único por jogo

            // ── Decidir destino do jogo antes de começar ─────────────────────────────
            // 85% Finalizado | 8% Cancelado | 7% Adiado
            double sorteio = random.NextDouble();
            int estadoFinal;
            string descricaoFinal;

            if (sorteio < 0.08)
            {
                // Cancelado — não chega a entrar Em Curso
                estadoFinal = 4;
                descricaoFinal = "CANCELADO";
                await AtualizarJogoAsync(httpClient, jogo, novoEstado: 4);
                Log(jogo.Codigo, $"CANCELADO — {jogo.EquipaCasa} vs {jogo.EquipaFora} (antes de começar)");
                return; // não simula nada
            }
            else if (sorteio < 0.15)
            {
                // Adiado — não chega a entrar Em Curso
                estadoFinal = 5;
                descricaoFinal = "ADIADO";
                await AtualizarJogoAsync(httpClient, jogo, novoEstado: 5);
                Log(jogo.Codigo, $"ADIADO — {jogo.EquipaCasa} vs {jogo.EquipaFora} (antes de começar)");
                return; // não simula nada
            }
            else
            {
                estadoFinal = 3;
                descricaoFinal = "FINALIZADO";
            }

            // ── Jogo segue para simulação normal (85% dos casos) ─────────────────────

            await AtualizarJogoAsync(httpClient, jogo, novoEstado: 2);
            Log(jogo.Codigo, $"Em Curso — {jogo.EquipaCasa} vs {jogo.EquipaFora}");

            for (int minuto = 10; minuto <= 90; minuto += 10)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));

                if (random.NextDouble() < 0.15)
                {
                    jogo.GolosCasa++;
                    Log(jogo.Codigo, $"GOLO! {jogo.EquipaCasa} — {jogo.GolosCasa}-{jogo.GolosFora} (min {minuto})");
                }

                if (random.NextDouble() < 0.15)
                {
                    jogo.GolosFora++;
                    Log(jogo.Codigo, $"GOLO! {jogo.EquipaFora} — {jogo.GolosCasa}-{jogo.GolosFora} (min {minuto})");
                }

                await AtualizarJogoAsync(httpClient, jogo, novoEstado: 2);
            }

            await AtualizarJogoAsync(httpClient, jogo, novoEstado: 3);
            Log(jogo.Codigo, $"FINALIZADO — {jogo.EquipaCasa} {jogo.GolosCasa}-{jogo.GolosFora} {jogo.EquipaFora}");
        }
        

        // ────────────────────────────────────────────────────────────────────────
        // Atualizar jogo na API
        // ────────────────────────────────────────────────────────────────────────

        static async Task AtualizarJogoAsync(HttpClient httpClient, SimulacaoJogo jogo, int novoEstado)
        {
            var request = new AtualizarJogoRequest
            {
                Golos_Casa = jogo.GolosCasa,
                Golos_Fora = jogo.GolosFora,
                Estado = novoEstado
            };

            try
            {
                var response = await httpClient.PutAsJsonAsync($"/api/jogos/{jogo.Codigo}", request);

                if (response.IsSuccessStatusCode)
                    jogo.Estado = novoEstado;
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    Log(jogo.Codigo, $"ERRO ao atualizar para estado {novoEstado}: {response.StatusCode} — {body}");
                }
            }
            catch (Exception ex)
            {
                Log(jogo.Codigo, $"Exceção ao atualizar: {ex.Message}");
            }
        }

        // ────────────────────────────────────────────────────────────────────────
        // Utilitários
        // ────────────────────────────────────────────────────────────────────────

        static List<(string casa, string fora)> GerarEmparelhamentos(Random rnd, List<string> equipas)
        {
            // Baralhar a lista — garante cada equipa exatamente uma vez (9 jogos de 18 equipas)
            var baralha = equipas.OrderBy(_ => rnd.Next()).ToList();
            var jogos = new List<(string, string)>();

            for (int i = 0; i < baralha.Count / 2; i++)
                jogos.Add((baralha[2 * i], baralha[2 * i + 1]));

            return jogos;
        }

        static string GerarCodigo(int ano, int jornada, int numeroJogo) =>
            $"FUT-{ano:0000}-{jornada:00}{numeroJogo:00}";

        // Log thread-safe com lock para não misturar linhas entre jogos paralelos
        static readonly object _logLock = new object();
        static void Log(string codigo, string mensagem)
        {
            lock (_logLock)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{codigo}] {mensagem}");
            }
        }
    }





 
}
