namespace BetStrike.Bets.Api.Models
{
    public class ApostaBackgroundMessage
    {
        public int ApostaId { get; set; }
        public int JogoId { get; set; }
        public string TipoAposta { get; set; } = string.Empty; // "1", "X", "2"
        public decimal Montante { get; set; }
        public decimal OddMomento { get; set; }
        public int Estado { get; set; } // 1=Pendente, 2=Ganha, 3=Perdida, 4=Anulada
        public DateTime DataHoraRegisto { get; set; }
    }
}
