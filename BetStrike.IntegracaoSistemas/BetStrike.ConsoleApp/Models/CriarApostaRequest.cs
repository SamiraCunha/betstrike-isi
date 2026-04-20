using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetStrike.ConsoleApp.Models
{
    public class CriarApostaRequest
    {
        public string Codigo_Jogo { get; set; }
        public int UtilizadorId { get; set; }
        public string Tipo_Aposta { get; set; }
        public decimal Montante { get; set; }
        public decimal Odd_Momento { get; set; }
    }
}
