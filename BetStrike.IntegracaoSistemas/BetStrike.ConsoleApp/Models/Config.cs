using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetStrike.ConsoleApp.Models
{
    public class ApiUrlsConfig
    {
        public string Resultados { get; set; }
        public string Apostas { get; set; }
    }

    public class AppSettings
    {
        public ApiUrlsConfig ApiUrls { get; set; }
    }
}
