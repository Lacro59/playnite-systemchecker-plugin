using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
    public class Cpu
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private List<CpuEquivalence> Equivalence = new List<CpuEquivalence>
            {
                new CpuEquivalence {Intel="", Amd=""}
            };

        private CpuObject ProcessorPc { get; set; }
        private CpuObject ProcessorRequierement { get; set; }


        public Cpu(SystemConfiguration systemConfiguration, string CpuRequierement)
        {








    }

        public bool IsBetter()
        {
            return false;
        }

        private bool CallIsIntel(string CpuName)
        {
            return CpuName.ToLower().IndexOf("intel") > -1 || Regex.IsMatch(CpuName, "i[0-9]*");
        }
        private bool CallIsAmd(string CpuName)
        {
            return CpuName.ToLower().IndexOf("amd") > -1 || CpuName.ToLower().IndexOf("ryzen") > -1;
        }

        public CpuObject SetProcessor(string CpuName)
        {
            bool IsIntel = CallIsIntel(CpuName);
            bool IsAmd = CallIsAmd(CpuName);

            string Type = "";
            string Version = "";
            double Clock = 0;


            // Version
            if (IsIntel)
            {
                Type = Regex.Match(CpuName, "i[0-9]*").Value.Trim();
                Version = Regex.Match(CpuName, "i[0-9]*-[0-9]*").Value.Replace(Type + "-", "").Trim();
            }
            if (IsAmd)
            {

            }

            // Clock
            Double.TryParse(Regex.Match(CpuName, "[0-9]*[.][0-9]*[ GHz]*").Value.Replace("GHz", "")
                .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out Clock);




            return new CpuObject
            {
                IsIntel = IsIntel,
                IsAmd = IsAmd,
                Type = Type,
                Version = Version,
                Clock = Clock
            };
        }
    }

    public class CpuObject
    {
        public bool IsIntel { get; set; }
        public bool IsAmd { get; set; }

        public string Type { get; set; }
        public string Version { get; set; }
        public double Clock { get; set; }
    }

    public class CpuEquivalence
    {
        public string Intel { get; set; }
        public string Amd { get; set; }
    }
}
