namespace BetStrike.Bets.Api.Models
{
    public class CriarApostaRequest
    {
        public string Codigo_Jogo { get; set; }
        public int UtilizadorId { get; set; }
        public string Tipo_Aposta { get; set; }   // "1", "X" ou "2"
        public decimal Montante { get; set; }
        public decimal Odd_Momento { get; set; }
    }
}
