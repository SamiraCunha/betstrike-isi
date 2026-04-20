namespace BetStrike.Results.Api.Models
{
    public class CriarJogoRequest
    {

        public string Codigo_Jogo { get; set; }
        public DateTime Data_Jogo { get; set; }
        public TimeSpan Hora_Inicio { get; set; }
        public string Equipa_Casa { get; set; }
        public string Equipa_Fora { get; set; }
        public int Golos_Casa { get; set; }
        public int Golos_Fora { get; set; }
        public int Estado { get; set; } //0..5
    }
}
