using BetStrike.Bets.Api.Data;
using BetStrike.Bets.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BetStrike.Bets.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EstatisticasController : ControllerBase
    {
        private readonly EstatisticasRepository _repository;

        public EstatisticasController(EstatisticasRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("jogo/{jogoId}")]
        public async Task<ActionResult<EstatisticasJogo>> ObterPorJogoId(int jogoId)
        {
            var estatisticas = await _repository.ObterPorJogoId(jogoId);

            if (estatisticas == null)
                return NotFound("Não existem estatísticas para este jogo.");

            return Ok(estatisticas);
        }
    }
}
