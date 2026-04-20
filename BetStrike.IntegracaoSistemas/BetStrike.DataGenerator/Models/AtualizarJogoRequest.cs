using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetStrike.DataGenerator.Models
{
    public class AtualizarJogoRequest
    {
        public int? Golos_Casa { get; set; }
        public int? Golos_Fora { get; set; }
        public int? Estado { get; set; }
    }
}
