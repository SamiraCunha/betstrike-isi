using BetStrike.ConsoleApp.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BetStrike.ConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            // Construir a configuração para ler o appsettings.json
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())   // base path para resolver o ficheiro[web:215]
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            // Ler as URLs
            string resultadosBaseUrl = config["ApiUrls:Resultados"];
            string apostasBaseUrl = config["ApiUrls:Apostas"];

            if (string.IsNullOrWhiteSpace(resultadosBaseUrl) ||
                string.IsNullOrWhiteSpace(apostasBaseUrl))
            {
                Console.WriteLine("ERRO: ApiUrls:Resultados ou ApiUrls:Apostas não definidos em appsettings.json.");
                return;
            }

            var httpResultados = new HttpClient { BaseAddress = new Uri(resultadosBaseUrl) };
            var httpApostas = new HttpClient { BaseAddress = new Uri(apostasBaseUrl) };

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== BetStrike Painel de Testes ===");
                Console.WriteLine("1) Plataforma Geradora (exe existente)");
                Console.WriteLine("2) Plataforma Resultados (API)");
                Console.WriteLine("3) Plataforma Apostas (API)");
                Console.WriteLine("0) Sair");
                Console.Write("Escolha: ");

                var opcao = Console.ReadLine();

                switch (opcao)
                {
                    case "1":
                        MenuGeradora(); // Apenas dispara o exe, se quiserem
                        break;
                    case "2":
                        await MenuResultadosAsync(httpResultados);
                        break;
                    case "3":
                        await MenuApostasAsync(httpApostas, httpResultados);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Opção inválida.");
                        break;
                }

                Console.WriteLine();
                Console.WriteLine("ENTER para continuar...");
                Console.ReadLine();
            }
        }

        // ------------- Plataforma Geradora (exe) -------------
        static void MenuGeradora()
        {
            Console.Clear();
            Console.WriteLine("=== Plataforma Geradora ===");
            Console.WriteLine("Esta opção apenas executa o exe BetStrike.DataGenerator.");
            Console.WriteLine("Geração + simulação continuam a ser feitas por esse programa.");
            Console.WriteLine();

            Console.Write("Prima ENTER para executar a plataforma geradora...");
            Console.ReadLine();

            var startInfo = new ProcessStartInfo
            {
                FileName = "\"C:\\Users\\Admin\\source\\repos\\betstrike-isi\\BetStrike.IntegracaoSistemas\\BetStrike.DataGenerator\\bin\\Debug\\BetStrike.DataGenerator.exe\"", // ajusta para o nome/caminho real
                UseShellExecute = true                    // abre numa nova janela de consola
            };

            Process.Start(startInfo);
        }

        // ------------- Plataforma Resultados (API) -------------
        static async Task MenuResultadosAsync(HttpClient httpResultados)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Plataforma Resultados (API) ===");
                Console.WriteLine("1) Listar jogos");
                Console.WriteLine("2) Ver detalhes de um jogo");
                Console.WriteLine("0) Voltar");
                Console.Write("Escolha: ");

                var op = Console.ReadLine();

                switch (op)
                {
                    case "1":
                        await ListarJogosResultadosAsync(httpResultados);
                        break;
                    case "2":
                        await VerJogoResultadosAsync(httpResultados);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Opção inválida.");
                        break;
                }

                Console.WriteLine("ENTER para continuar...");
                Console.ReadLine();
            }
        }

        // ------------- Plataforma Apostas (API) -------------
        static async Task MenuApostasAsync(HttpClient httpApostas, HttpClient httpResultados)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Plataforma Apostas (API) ===");
                Console.WriteLine("1) Listar jogos disponíveis");
                Console.WriteLine("2) Criar aposta");
                Console.WriteLine("3) Ver apostas de um utilizador");
                Console.WriteLine("4) Sincronizar resultado + resolver apostas de um jogo");
                Console.WriteLine("5) Sincronizar jogos da Results para Bets");
                Console.WriteLine("0) Voltar");
                Console.Write("Escolha: ");

                var op = Console.ReadLine();

                switch (op)
                {
                    case "1":
                        await ListarJogosApostasAsync(httpApostas);
                        break;
                    case "2":
                        await CriarApostaAsync(httpApostas);
                        break;
                    case "3":
                        await VerApostasUtilizadorAsync(httpApostas);
                        break;
                    case "4":
                        await SincronizarEResolverJogoAsync(httpApostas);
                        break;
                    case "5":
                        await SincronizarJogosResultadosParaBetsAsync(httpResultados, httpApostas);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Opção inválida.");
                        break;
                }

                Console.WriteLine("ENTER para continuar...");
                Console.ReadLine();
            }
        }

        static async Task SincronizarJogosResultadosParaBetsAsync(HttpClient httpResultados, HttpClient httpApostas)
        {
            Console.Clear();
            Console.WriteLine("=== Sincronizar jogos da Plataforma de Resultados para Apostas ===\n");

            try
            {
                // Lê todos os jogos da Results.Api
                var jogosResults = await httpResultados
                    .GetFromJsonAsync<List<JogosResultados>>("api/jogos"); // ajusta se tua rota for diferente

                if (jogosResults == null || jogosResults.Count == 0)
                {
                    Console.WriteLine("Nenhum jogo encontrado na Plataforma de Resultados.");
                    return;
                }

                int inseridos = 0;
                int jaExistiam = 0;
                int erros = 0;

                foreach (var jogo in jogosResults)
                {
                    var request = new CriarJogoBetsRequest
                    {
                        Codigo_Jogo = jogo.Codigo_Jogo,
                        DataHora_Inicio = jogo.Data_Jogo.Date + jogo.Hora_Inicio,
                        Equipa_Casa = jogo.Equipa_Casa,
                        Equipa_Fora = jogo.Equipa_Fora,
                        Tipo_Competicao = "Primeira Liga",
                        Estado = jogo.Estado
                    };

                    var response = await httpApostas.PostAsJsonAsync("api/jogos", request);

                    if (response.IsSuccessStatusCode)
                    {
                        inseridos++;
                    }
                    else
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        if (body != null && body.IndexOf("Já existe um jogo com esse Codigo_Jogo", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            jaExistiam++;
                        }
                        else
                        {
                            erros++;
                            Console.WriteLine($"Erro ao sincronizar jogo {jogo.Codigo_Jogo}: {body}");
                        }
                    }
                }

                Console.WriteLine($"\nSincronização concluída:");
                Console.WriteLine($"  Inseridos novos: {inseridos}");
                Console.WriteLine($"  Já existiam:     {jaExistiam}");
                Console.WriteLine($"  Erros:           {erros}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao sincronizar jogos: " + ex.Message);
            }
        }

        static async Task SincronizarEResolverJogoAsync(HttpClient httpApostas)
        {
            Console.Clear();
            Console.WriteLine("=== Sincronizar resultado e resolver apostas ===\n");

            Console.Write("Código do jogo (ex: FUT-2026-0101): ");
            var codigo = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(codigo))
            {
                Console.WriteLine("Código inválido.");
                return;
            }

            try
            {
                var url = $"api/jogos/sincronizar-e-resolver/{codigo}";
                var response = await httpApostas.PostAsync(url, content: null);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Falha ao sincronizar/resolver. Status: {response.StatusCode}");
                    var bodyError = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(bodyError);
                    return;
                }

                var bodyOk = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Operação concluída com sucesso:");
                Console.WriteLine(bodyOk);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao chamar API de Apostas: " + ex.Message);
            }
        }


        static async Task ListarJogosResultadosAsync(HttpClient httpResultados)
        {
            Console.Clear();
            Console.WriteLine("=== Jogos na Plataforma de Resultados ===\n");

            try
            {
                Console.WriteLine("BaseAddress: " + httpResultados.BaseAddress);
                Console.WriteLine("Rota: api/jogos");

                var response = await httpResultados.GetAsync("api/jogos");

                Console.WriteLine("StatusCode: " + response.StatusCode);

                response.EnsureSuccessStatusCode();

                var jogos = await response.Content.ReadFromJsonAsync<List<JogosResultados>>();

                if (jogos == null || jogos.Count == 0)
                {
                    Console.WriteLine("Nenhum jogo encontrado.");
                    return;
                }

                foreach (var jogo in jogos)
                {
                    Console.WriteLine($"{jogo.Codigo_Jogo} | {jogo.Equipa_Casa} {jogo.Golos_Casa} - {jogo.Golos_Fora} {jogo.Equipa_Fora}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao obter jogos: " + ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine("Inner: " + ex.InnerException.Message);
            }
        }
        static async Task VerJogoResultadosAsync(HttpClient httpResultados)
        {
            Console.Clear();
            Console.WriteLine("=== Detalhes de um jogo (Resultados) ===\n");

            Console.Write("Introduza o Código do Jogo (ex: FUT-2026-0101): ");
            var codigo = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(codigo))
            {
                Console.WriteLine("Código inválido.");
                return;
            }

            try
            {
                // GET /api/jogos/{codigo}
                var url = $"api/jogos/{codigo}";
                var jogo = await httpResultados
                    .GetFromJsonAsync<JogosResultados>(url); // deserializa direto[web:135][web:155]

                if (jogo == null)
                {
                    Console.WriteLine("Jogo não encontrado.");
                    return;
                }

                Console.WriteLine($"\nCódigo: {jogo.Codigo_Jogo}");
                Console.WriteLine($"Equipas: {jogo.Equipa_Casa} vs {jogo.Equipa_Fora}");
                Console.WriteLine($"Resultado: {jogo.Golos_Casa} - {jogo.Golos_Fora}");
                Console.WriteLine($"Data: {jogo.Data_Jogo:yyyy-MM-dd} {jogo.Hora_Inicio}");
                Console.WriteLine($"Estado: {jogo.Estado}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Erro HTTP ao obter jogo: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao obter jogo: " + ex.Message);
            }
        }

        static async Task ListarJogosApostasAsync(HttpClient httpApostas)
        {
            Console.Clear();
            Console.WriteLine("=== Jogos disponíveis para aposta ===\n");

            try
            {
                var jogos = await httpApostas
                    .GetFromJsonAsync<List<JogoDisponivelDto>>("api/Jogos/disponiveis");

                if (jogos == null || jogos.Count == 0)
                {
                    Console.WriteLine("Nenhum jogo disponível.");
                    return;
                }

                foreach (var jogo in jogos)
                {
                    Console.WriteLine(
                        $"{jogo.Codigo_Jogo} | {jogo.Equipa_Casa} vs {jogo.Equipa_Fora} | " +
                        $"{jogo.DataHora_Inicio:yyyy-MM-dd HH:mm} | Comp: {jogo.Tipo_Competicao} | Estado: {jogo.Estado}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao obter jogos disponíveis: " + ex.Message);
            }
        }
        static async Task CriarApostaAsync(HttpClient httpApostas)
        {
            Console.Clear();
            Console.WriteLine("=== Criar Aposta ===\n");

            Console.Write("Código do jogo: ");
            var codigoJogo = Console.ReadLine();

            Console.Write("UtilizadorId: ");
            var utilizadorStr = Console.ReadLine();
            if (!int.TryParse(utilizadorStr, out var utilizadorId))
            {
                Console.WriteLine("UtilizadorId inválido.");
                return;
            }

            Console.Write("Tipo de aposta (1/X/2): ");
            var tipoAposta = Console.ReadLine();

            Console.Write("Montante: ");
            var montanteStr = Console.ReadLine();
            if (!decimal.TryParse(montanteStr, out var montante))
            {
                Console.WriteLine("Montante inválido.");
                return;
            }

            Console.Write("Odd no momento: ");
            var oddStr = Console.ReadLine();
            if (!decimal.TryParse(oddStr, out var odd))
            {
                Console.WriteLine("Odd inválida.");
                return;
            }

            var request = new CriarApostaRequest
            {
                Codigo_Jogo = codigoJogo,
                UtilizadorId = utilizadorId,
                Tipo_Aposta = tipoAposta,
                Montante = montante,
                Odd_Momento = odd
            };

            try
            {
                var response = await httpApostas.PostAsJsonAsync("api/apostas", request);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Falha ao criar aposta. Status: {response.StatusCode}");
                    var body = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(body);
                    return;
                }

                var bodyOk = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Aposta criada com sucesso.");
                Console.WriteLine(bodyOk);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao criar aposta: " + ex.Message);
            }
        }

        static async Task VerApostasUtilizadorAsync(HttpClient httpApostas)
        {
            Console.Clear();
            Console.WriteLine("=== Apostas de um utilizador ===\n");

            Console.Write("Introduza o UtilizadorId: ");
            var input = Console.ReadLine();

            if (!int.TryParse(input, out var utilizadorId))
            {
                Console.WriteLine("UtilizadorId inválido.");
                return;
            }

            try
            {
                var url = $"api/apostas/utilizador/{utilizadorId}";
                var apostas = await httpApostas
                    .GetFromJsonAsync<List<ApostaDto>>(url);

                if (apostas == null || apostas.Count == 0)
                {
                    Console.WriteLine("Nenhuma aposta encontrada para este utilizador.");
                    return;
                }

                foreach (var aposta in apostas)
                {
                    Console.WriteLine(
                        $"Id: {aposta.Id} | Jogo: {aposta.Codigo_Jogo} | Tipo: {aposta.Tipo_Aposta} | " +
                        $"Montante: {aposta.Montante} | Odd: {aposta.Odd_Momento} | Estado: {aposta.Estado} | " +
                        $"Registo: {aposta.DataHora_Registo:yyyy-MM-dd HH:mm}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("Erro HTTP ao obter apostas: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao obter apostas: " + ex.Message);
            }
        }
     
    }
}