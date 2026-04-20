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
            var equipas = new List<string>
            {
                "FC Porto",
                "SL Benfica",
                "Sporting CP",
                "SC Braga",
                "Vitória SC",
                "Boavista",
                "Gil Vicente",
                "Famalicão",
                "Rio Ave",
                "Estoril",
                "Portimonense",
                "Casa Pia",
                "Farense",
                "Moreirense",
                "Arouca",
                "Vizela",
                "Chaves",
                "Estrela da Amadora"
            };

       

            List<(string casa, string fora)> GerarEmparelhamentosJornada(Random rnd, List<string> todasEquipas)
            {
                // Copiar e baralhar lista
                var equipasBaralhadas = todasEquipas.OrderBy(_ => rnd.Next()).ToList();

                var jogos = new List<(string casa, string fora)>();

                for (int i = 0; i < 9; i++)
                {
                    var equipaCasa = equipasBaralhadas[2 * i];
                    var equipaFora = equipasBaralhadas[2 * i + 1];

                    jogos.Add((equipaCasa, equipaFora));
                }

                return jogos;
            }



            string GerarCodigoJogo(int anoParam, int numeroJornadaParam, int numeroJogo)
            {
                var anoStr = anoParam.ToString("0000");
                var jornadaStr = numeroJornadaParam.ToString("00");
                var jogoStr = numeroJogo.ToString("00");
                return $"FUT-{anoStr}-{jornadaStr}{jogoStr}";
            }

            // ------------------------
            // FASE 1: PUBLICAÇÃO DO CALENDÁRIO
            // ------------------------

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7286") // AJUSTA para a URL da tua API de Resultados
            };

            var random = new Random();

            int ano = DateTime.Now.Year;
            int numeroJornada = 1;

            // Definir uma data base para a jornada
            var dataJornada = new DateTime(ano, 9, 15);
            var horaBase = new TimeSpan(20, 0, 0); // 20:00
            var jogosSimulados = new List<SimulacaoJogo>();
            var emparelhamentos = GerarEmparelhamentosJornada(random, equipas);

            for (int i = 0; i < emparelhamentos.Count; i++)
            {
                var (casa, fora) = emparelhamentos[i];

                var codigo = GerarCodigoJogo(ano, numeroJornada, i + 1);

                

                var request = new CriarJogoRequest
                {
                    Codigo_Jogo = codigo,
                    Data_Jogo = dataJornada,
                    Hora_Inicio = horaBase.Add(TimeSpan.FromMinutes(15 * i)), // jogos espaçados de 15 min, por exemplo
                    Equipa_Casa = casa,
                    Equipa_Fora = fora,
                    Golos_Casa = 0,
                    Golos_Fora = 0,
                    Estado = 1 // Agendado
                };

                Console.WriteLine($"A criar jogo {codigo}: {casa} vs {fora}");

                var response = await httpClient.PostAsJsonAsync("/api/jogos", request);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Falha ao criar jogo {codigo}. Status: {response.StatusCode}. Corpo: {body}");
                }
                else
                {
                    Console.WriteLine($"Jogo {codigo} criado com sucesso.");

                    jogosSimulados.Add(new SimulacaoJogo
                    {
                        Codigo = codigo,
                        EquipaCasa = casa,
                        EquipaFora = fora,
                        GolosCasa = 0, // Simular golos entre 0 e 4
                        GolosFora = 0,
                        Estado = 1 // Agendado
                    });
                }
            }

            Console.WriteLine("Publicação do calendário concluída.");

            Console.WriteLine("Iniciando simulação dos jogos (Fase 2)...");

            var tarefas = jogosSimulados.Select(jogo => SimularJogoAsync(httpClient, jogo));

            await Task.WhenAll(tarefas);

            Console.WriteLine("Simulação concluída.");

        }


        static async Task SimularJogoAsync(HttpClient httpClient, SimulacaoJogo jogo)
        {
            // 1) Passar de Agendado (1) para Em Curso (2)
            await AtualizarJogoAsync(httpClient, jogo, novoEstado: 2);
            Console.WriteLine($"Jogo {jogo.Codigo} entrou em curso.");

            // Vamos simular 9 intervalos de 10 segundos (90 minutos fictícios)
            var random = new Random();

            for (int i = 0; i < 9; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(10)); // 10s = 10 minutos fictícios

                // Geração de golos aleatória (prob média 2-3 golos por jogo)
                // Exemplo simples: em cada intervalo, pequena probabilidade de golo
                if (random.NextDouble() < 0.3) // 30% de chance de sair golo neste intervalo
                {
                    bool goloCasa = random.NextDouble() < 0.5;

                    if (goloCasa)
                        jogo.GolosCasa++;
                    else
                        jogo.GolosFora++;

                    Console.WriteLine($"Jogo {jogo.Codigo}: Golo! {jogo.EquipaCasa} {jogo.GolosCasa} - {jogo.GolosFora} {jogo.EquipaFora}");
                }

                await AtualizarJogoAsync(httpClient, jogo, novoEstado: 2);
            }

            // 3) Finalizar jogo
            await AtualizarJogoAsync(httpClient, jogo, novoEstado: 3);
            Console.WriteLine($"Jogo {jogo.Codigo} finalizado: {jogo.EquipaCasa} {jogo.GolosCasa} - {jogo.GolosFora} {jogo.EquipaFora}");
        }

        static async Task AtualizarJogoAsync(HttpClient httpClient, SimulacaoJogo jogo, int novoEstado)
        {
            var request = new AtualizarJogoRequest
            {
                Golos_Casa = jogo.GolosCasa,
                Golos_Fora = jogo.GolosFora,
                Estado = novoEstado
            };

            var response = await httpClient.PutAsJsonAsync($"/api/jogos/{jogo.Codigo}", request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Falha ao atualizar jogo {jogo.Codigo}. Status: {response.StatusCode}. Corpo: {body}");
            }
            else
            {
                jogo.Estado = novoEstado;
            }
        }


    }
}
