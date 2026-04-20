namespace BetStrike.Results.Api.Models
{
    public class AtualizarJogoRequest
    {
        public int? Golos_Casa { get; set; } //  ? = o campo pode ter valor ou pode ser null
        public int? Golos_Fora { get; set; }
        public int? Estado { get; set; } //0..5
    }
}
