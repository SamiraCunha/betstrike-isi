namespace BetStrike.Bets.Api.Models
{
    public class AtualizarJogoRequest
    {
        public int Estado { get; set; }  // 1: Não Iniciado, 2: Em Andamento, 3: Finalizado, 4: Adiado, 5: Cancelado
    }
}
