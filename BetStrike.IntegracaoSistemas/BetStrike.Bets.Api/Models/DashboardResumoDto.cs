namespace BetStrike.Bets.Api.Models
{
    public class DashboardResumoDto
    {
        public decimal VolumeTotalApostado { get; set; }
        public int NumeroTotalApostas { get; set; }

        public int TotalApostasTipo1 { get; set; }
        public int TotalApostasTipoX { get; set; }
        public int TotalApostasTipo2 { get; set; }

        public decimal PercentagemTipo1 { get; set; }
        public decimal PercentagemTipoX { get; set; }
        public decimal PercentagemTipo2 { get; set; }

        public List<DashboardJogoResumoDto> TopJogosPorVolume { get; set; } = new();
    }

    public class DashboardJogoResumoDto
    {
        public int JogoId { get; set; }
        public decimal TotalApostado { get; set; }
        public int NumeroApostas { get; set; }
    }

}
