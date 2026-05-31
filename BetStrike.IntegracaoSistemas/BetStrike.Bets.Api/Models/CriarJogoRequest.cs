namespace BetStrike.Bets.Api.Models
{
    public class CriarJogoRequest
    {
        public string Codigo_Jogo { get; set; }
        public DateTime DataHora_Inicio { get; set; }
        public string Equipa_Casa { get; set; }
        public string Equipa_Fora { get; set; }
        public string Tipo_Competicao { get; set; } = string.Empty;
        public int Estado { get; set; }  // 1..5
    }
}
