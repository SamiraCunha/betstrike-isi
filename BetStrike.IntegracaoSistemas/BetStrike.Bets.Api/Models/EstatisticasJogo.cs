namespace BetStrike.Bets.Api.Models
{
    public class EstatisticasJogo
    {
        public int Id { get; set; }
        public int JogoId { get; set; }
        public decimal Total_Apostado { get; set; }
        public int Numero_Apostas { get; set; }
        public int Apostas_Tipo1 { get; set; }
        public int Apostas_TipoX { get; set; }
        public int Apostas_Tipo2 { get; set; }
        public decimal Valor_Apostas_Tipo1 { get; set; }
        public decimal Valor_Apostas_TipoX { get; set; }
        public decimal Valor_Apostas_Tipo2 { get; set; }
        public DateTime DataHora_UltimaAtualizacao { get; set; }

       
    }
}
