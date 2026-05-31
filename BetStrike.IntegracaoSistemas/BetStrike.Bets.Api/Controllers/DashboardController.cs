using BetStrike.Bets.Api.Data;
using BetStrike.Bets.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BetStrike.Bets.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {

        private readonly EstatisticasRepository _estatisticasRepository;

        public DashboardController(EstatisticasRepository estatisticasRepository)
        {
            _estatisticasRepository = estatisticasRepository;
        }

        [HttpGet("resumo")]
        public async Task<ActionResult<DashboardResumoDto>> GetResumo()
        {
            var estatisticas = await _estatisticasRepository.ObterTodas();

            var lista = estatisticas.ToList();
            if (!lista.Any())
                return Ok(new DashboardResumoDto());

            var volumeTotal = lista.Sum(e => e.Total_Apostado);
            var numeroTotalApostas = lista.Sum(e => e.Numero_Apostas);

            var totalTipo1 = lista.Sum(e => e.Apostas_Tipo1);
            var totalTipoX = lista.Sum(e => e.Apostas_TipoX);
            var totalTipo2 = lista.Sum(e => e.Apostas_Tipo2);

            var totalTipos = totalTipo1 + totalTipoX + totalTipo2;

            var dto = new DashboardResumoDto
            {
                VolumeTotalApostado = volumeTotal,
                NumeroTotalApostas = numeroTotalApostas,
                TotalApostasTipo1 = totalTipo1,
                TotalApostasTipoX = totalTipoX,
                TotalApostasTipo2 = totalTipo2,
                PercentagemTipo1 = totalTipos == 0 ? 0 : (decimal)totalTipo1 / totalTipos * 100,
                PercentagemTipoX = totalTipos == 0 ? 0 : (decimal)totalTipoX / totalTipos * 100,
                PercentagemTipo2 = totalTipos == 0 ? 0 : (decimal)totalTipo2 / totalTipos * 100,
                TopJogosPorVolume = lista
                    .OrderByDescending(e => e.Total_Apostado)
                    .Take(5)
                    .Select(e => new DashboardJogoResumoDto
                    {
                        JogoId = e.JogoId,
                        TotalApostado = e.Total_Apostado,
                        NumeroApostas = e.Numero_Apostas
                    })
                    .ToList()
            };

            return Ok(dto);
        }
    }
}
