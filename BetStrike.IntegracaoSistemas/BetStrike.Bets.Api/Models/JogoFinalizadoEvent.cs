namespace BetStrike.Bets.Api.Models
{
    public class JogoFinalizadoEvent
    {
        public int JogoId { get; set; }
        public string Codigo_Jogo { get; set; } = string.Empty;
        public int Estado { get; set; }            // 3, 4 ou 5

        public int GolosCasa { get; set; }
        public int GolosFora { get; set; }
        
        public DateTime DataHoraFinalizacao { get; set; }
    }
}
