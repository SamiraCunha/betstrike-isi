# betstrike-isi
Trabalho de Integração de Sistemas – Parte 1 (BetStrike)

## Requisitos

- .NET 8 (ou versão usada no projeto).
- SQL Server (localdb ou instância completa).
- Visual Studio / VS Code.

---
## Como executar

1. Restaurar/criar as bases de dados `Resultados`, `Apostas` e `Pagamentos` com os scripts fornecidos (tabelas + SPs + trigger).
2. Atualizar strings de ligação e URLs das APIs em:
   - `BetStrike.Results.Api/appsettings.json`
   - `BetStrike.Bets.Api/appsettings.json`
   - `BetStrike.ConsoleApp/appsettings.json`
3. Executar:
   - `BetStrike.Results.Api`
   - `BetStrike.Bets.Api`
   - `BetStrike.ConsoleApp`
   - `BetStrike.DataGenerator` (a partir do menu da ConsoleApp ou manualmente)
4. Seguir o fluxo descrito em “Fluxo end-to-end”.


## Fluxo end-to-end (exemplo)

1. **Publicar calendário**  
   DataGenerator (opção 1) cria 9 jogos Agendados (1) na Results.Api.

2. **Sincronizar jogos para Apostas**  
   ConsoleApp → “Sincronizar jogos da Results para Bets” → cria jogos na BD `Apostas`.

3. **Criar apostas**  
   ConsoleApp → “Criar aposta”:
   - Bets.Api insere a aposta e invoca `spDebitarAposta` em Pagamentos.
   - Pagamentos:
     - cria saldo inicial de 50€ (DE),
     - debita o montante (AP).

4. **Simular jogos**  
   DataGenerator (opção 2) simula os jogos:
   - maioria termina como Finalizado (3),
   - alguns podem ser Cancelados (4) ou Adiados (5).

5. **Sincronizar resultado + resolver apostas**  
   ConsoleApp → “Sincronizar resultado + resolver apostas de um jogo”:
   - Bets.Api lê o resultado da Results.Api,
   - atualiza `Jogo` + `Resultado`,
   - chama `spResolverApostasDoJogo`,
   - Trigger em `Aposta` gera `PG` ou `RE` em Pagamentos.

6. **Ver apostas e saldo**  
   - ConsoleApp → “Ver apostas de um utilizador” → estados Ganha/Perdida/Anulada.  
   - BD Pagamentos:
     - tabela `Saldo_Utilizador` reflete bónus, débitos e créditos,
     - tabela `Transacao` regista `DE`, `AP`, `PG`, `RE`.

---



