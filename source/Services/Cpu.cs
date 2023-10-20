using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using SystemChecker.Clients;
using SystemChecker.Models;

namespace SystemChecker.Services
{
    public class Cpu
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private List<CpuEquivalence> Equivalence { get; set; } = new List<CpuEquivalence>
        {
            new CpuEquivalence {Intel=string.Empty, Amd=string.Empty}
        };

        private CpuObject ProcessorPc { get; set; }
        private CpuObject ProcessorRequierement { get; set; }


        public Cpu(SystemConfiguration systemConfiguration, string CpuRequierement)
        {
            ProcessorPc = SetProcessor(systemConfiguration.Cpu);
            ProcessorRequierement = SetProcessor(CpuRequierement);
        }

        public CheckResult IsBetter()
        {
            Common.LogDebug(true, $"Cpu.IsBetter - ProcessorPc: {Serialization.ToJson(ProcessorPc)}");
            Common.LogDebug(true, $"Cpu.IsBetter - ProcessorRequierement: {Serialization.ToJson(ProcessorRequierement)}");

            // Old Processor
            if (!ProcessorPc.IsOld && ProcessorRequierement.IsOld)
            {
                return new CheckResult { Result = true };
            }
            if (ProcessorPc.IsOld && !ProcessorRequierement.IsOld)
            {
                return new CheckResult();
            }
            if (ProcessorPc.IsOld && ProcessorRequierement.IsOld)
            {
                // TODO: CPU old vs old
                logger.Warn($"No CPU treatment for {Serialization.ToJson(ProcessorPc)} & {Serialization.ToJson(ProcessorRequierement)}");
                return new CheckResult();
            }

            if (!ProcessorRequierement.IsIntel && !ProcessorRequierement.IsAmd)
            {
                // Clock
                if (ProcessorRequierement.Clock == 0)
                {
                    return new CheckResult { Result = true };
                }
                else
                {
                    if (ProcessorPc.Clock == 0)
                    {
                        logger.Warn($"ProcessorPc.Clock is 0");
                        return new CheckResult { Result = true };
                    }

                    return new CheckResult { Result = true };
                    //return ProcessorPc.Clock >= ProcessorRequierement.Clock;
                }
            }


            Benchmark benchmark = new Benchmark();
            bool? isBetter = benchmark.IsBetterCpu(ProcessorPc.Name, ProcessorRequierement.Name);
            if (isBetter != null)
            {
                return new CheckResult
                {
                    Result = (bool)isBetter,
                    SameConstructor = true
                };
            }


            // Intel vs Intel
            if (ProcessorPc.IsIntel && ProcessorRequierement.IsIntel)
            {
                if (ProcessorPc.Type == ProcessorRequierement.Type)
                {
                    return new CheckResult { SameConstructor = true, Result = ProcessorPc.Version >= ProcessorRequierement.Version };
                }
                if (int.Parse(Regex.Match(ProcessorPc.Type, @"\d").Value) > int.Parse(Regex.Match(ProcessorRequierement.Type, @"\d").Value))
                {
                    return new CheckResult { Result = true };
                }
                else
                {
                    if (ProcessorPc.Type == "i3" && ProcessorRequierement.Type == "i5" || ProcessorPc.Type == "i5" && ProcessorRequierement.Type == "i7")
                    {
                        return new CheckResult { SameConstructor = true, Result = (ProcessorPc.Version + 1000) >= ProcessorRequierement.Version };
                    }

                    if (ProcessorPc.Type == "i3" && ProcessorRequierement.Type == "i7")
                    {
                        return new CheckResult { SameConstructor = true, Result = (ProcessorPc.Version + 500) >= ProcessorRequierement.Version };
                    }
                }
            }

            // Amd vs Amd
            if (ProcessorPc.IsAmd && ProcessorRequierement.IsAmd)
            {
                if (Regex.Match(ProcessorPc.Type, @"Ryzen[ ][0-9]", RegexOptions.IgnoreCase).Value == Regex.Match(ProcessorRequierement.Type, @"Ryzen[ ][0-9]", RegexOptions.IgnoreCase).Value)
                {
                    return new CheckResult { SameConstructor = true, Result = ProcessorPc.Version >= ProcessorRequierement.Version };
                }
                if (ProcessorPc.Type.ToLower().IndexOf("ryzen") > -1 && ProcessorRequierement.Type.ToLower().IndexOf("athlon") > -1)
                {
                    return new CheckResult { SameConstructor = true, Result = true };
                }
                if (ProcessorPc.Type.ToLower().IndexOf("ryzen") > -1 && ProcessorRequierement.Type.ToLower().IndexOf("ryzen") > -1)
                {
                    if (int.Parse(Regex.Match(ProcessorPc.Type, @"\d").Value) > int.Parse(Regex.Match(ProcessorRequierement.Type, @"\d").Value))
                    {
                        return new CheckResult { SameConstructor = true, Result = true };
                    }
                    else
                    {
                        return new CheckResult { SameConstructor = true, Result = (ProcessorPc.Version + 1500) >= ProcessorRequierement.Version };
                    }
                }
            }


            // Amd vs Intel
            if (ProcessorPc.IsAmd && ProcessorRequierement.IsIntel)
            {
                if (ProcessorPc.Type.ToLower().Contains("ryzen 3") && ProcessorRequierement.Type.ToLower().Contains("i3"))
                {
                    return new CheckResult { Result = (ProcessorPc.Version + 4000) >= ProcessorRequierement.Version };
                }
                if (ProcessorPc.Type.ToLower().Contains("ryzen 3") && ProcessorRequierement.Type.ToLower().Contains("i5"))
                {
                    return new CheckResult { Result = (ProcessorPc.Version + 3000) >= ProcessorRequierement.Version };
                }
                if (ProcessorPc.Type.ToLower().Contains("ryzen 3") && ProcessorRequierement.Type.ToLower().Contains("i7"))
                {
                    return new CheckResult { Result = (ProcessorPc.Version + 2000) >= ProcessorRequierement.Version };
                }

                if (ProcessorPc.Type.ToLower().Contains("ryzen 5") && ProcessorRequierement.Type.ToLower().Contains("i3"))
                {
                    return new CheckResult { Result = (ProcessorPc.Version + 5000) >= ProcessorRequierement.Version };
                }
                if (ProcessorPc.Type.ToLower().Contains("ryzen 5") && ProcessorRequierement.Type.ToLower().Contains("i5"))
                {
                    return new CheckResult { Result = (ProcessorPc.Version + 4000) >= ProcessorRequierement.Version };
                }
                if (ProcessorPc.Type.ToLower().Contains("ryzen 5") && ProcessorRequierement.Type.ToLower().Contains("i7"))
                {
                    return new CheckResult { Result = (ProcessorPc.Version + 3000) >= ProcessorRequierement.Version };
                }

                if (ProcessorPc.Type.ToLower().Contains("ryzen 7") && ProcessorRequierement.Type.ToLower().Contains("i3"))
                {
                    return new CheckResult { Result = (ProcessorPc.Version + 6000) >= ProcessorRequierement.Version };
                }
                if (ProcessorPc.Type.ToLower().Contains("ryzen 7") && ProcessorRequierement.Type.ToLower().Contains("i5"))
                {
                    return new CheckResult { Result = (ProcessorPc.Version + 5000) >= ProcessorRequierement.Version };
                }
                if (ProcessorPc.Type.ToLower().Contains("ryzen 7") && ProcessorRequierement.Type.ToLower().Contains("i7"))
                {
                    return new CheckResult { Result = (ProcessorPc.Version + 4000) >= ProcessorRequierement.Version };
                }
            }

            // Intel vs Amd
            if (ProcessorPc.IsIntel && ProcessorRequierement.IsAmd)
            {
                if (ProcessorRequierement.Type.ToLower().Contains("ryzen 3") && ProcessorPc.Type.ToLower().Contains("i3"))
                {
                    return new CheckResult { Result = (ProcessorRequierement.Version + 4000) >= ProcessorPc.Version };
                }
                if (ProcessorRequierement.Type.ToLower().Contains("ryzen 3") && ProcessorPc.Type.ToLower().Contains("i5"))
                {
                    return new CheckResult { Result = (ProcessorRequierement.Version + 3000) >= ProcessorPc.Version };
                }
                if (ProcessorRequierement.Type.ToLower().Contains("ryzen 3") && ProcessorPc.Type.ToLower().Contains("i7"))
                {
                    return new CheckResult { Result = (ProcessorRequierement.Version + 2000) >= ProcessorPc.Version };
                }

                if (ProcessorRequierement.Type.ToLower().Contains("ryzen 5") && ProcessorPc.Type.ToLower().Contains("i3"))
                {
                    return new CheckResult { Result = (ProcessorRequierement.Version + 5000) >= ProcessorPc.Version };
                }
                if (ProcessorRequierement.Type.ToLower().Contains("ryzen 5") && ProcessorPc.Type.ToLower().Contains("i5"))
                {
                    return new CheckResult { Result = (ProcessorRequierement.Version + 4000) >= ProcessorPc.Version };
                }
                if (ProcessorRequierement.Type.ToLower().Contains("ryzen 5") && ProcessorPc.Type.ToLower().Contains("i7"))
                {
                    return new CheckResult { Result = (ProcessorRequierement.Version + 3000) >= ProcessorPc.Version };
                }

                if (ProcessorRequierement.Type.ToLower().Contains("ryzen 7") && ProcessorPc.Type.ToLower().Contains("i3"))
                {
                    return new CheckResult { Result = (ProcessorRequierement.Version + 6000) >= ProcessorPc.Version };
                }
                if (ProcessorRequierement.Type.ToLower().Contains("ryzen 7") && ProcessorPc.Type.ToLower().Contains("i5"))
                {
                    return new CheckResult { Result = (ProcessorRequierement.Version + 5000) >= ProcessorPc.Version };
                }
                if (ProcessorRequierement.Type.ToLower().Contains("ryzen 7") && ProcessorPc.Type.ToLower().Contains("i7"))
                {
                    return new CheckResult { Result = (ProcessorRequierement.Version + 4000) >= ProcessorPc.Version };
                }
            }


            logger.Warn($"No CPU treatment for {Serialization.ToJson(ProcessorPc)} & {Serialization.ToJson(ProcessorRequierement)}");
            return new CheckResult();
        }

        public static bool CallIsIntel(string CpuName)
        {
            return CpuName.ToLower().IndexOf("intel") > -1 || Regex.IsMatch(CpuName, "i[0-9]");
        }
        public static bool CallIsAmd(string CpuName)
        {
            return CpuName.ToLower().IndexOf("amd") > -1 || CpuName.ToLower().IndexOf("ryzen") > -1;
        }

        private CpuObject SetProcessor(string CpuName)
        {
            bool IsIntel = CallIsIntel(CpuName);
            bool IsAmd = CallIsAmd(CpuName);
            bool IsOld = false;

            string Type = string.Empty;
            int Version = 0;
            double Clock = 0;


            // Type & Version & IsOld
            if (IsIntel)
            {
                Type = Regex.Match(CpuName, "i[0-9]", RegexOptions.IgnoreCase).Value.Trim();   
                int.TryParse(Regex.Match(CpuName, "i[0-9]-[0-9]*", RegexOptions.IgnoreCase).Value.Replace(Type + "-", string.Empty).Trim(), out Version);

                if (Version == 0)
                {
                    int.TryParse(Regex.Match(CpuName, @"\d{4}", RegexOptions.IgnoreCase).Value.Trim(), out Version);
                }

                IsOld = !Regex.IsMatch(CpuName, "i[0-9]", RegexOptions.IgnoreCase);
            }
            if (IsAmd)
            {
                if (CpuName.ToLower().IndexOf("ryzen") > -1)
                {
                    Type = Regex.Match(CpuName, "Ryzen[ ][0-9]", RegexOptions.IgnoreCase).Value.Trim();

                    if (Regex.IsMatch(CpuName, "[0-9]+G", RegexOptions.IgnoreCase))
                    {
                        Type += " G";
                    }
                    if (Regex.IsMatch(CpuName, "[0-9]+XT", RegexOptions.IgnoreCase))
                    {
                        Type += " XT";
                    }
                    if (Regex.IsMatch(CpuName, "[0-9]+X", RegexOptions.IgnoreCase))
                    {
                        Type += " X";
                    }
                    if (Regex.IsMatch(CpuName, "[0-9]+U", RegexOptions.IgnoreCase))
                    {
                        Type += " U";
                    }
                }
                if (CpuName.ToLower().IndexOf("athlon") > -1)
                {
                    Type = "Athlon";


                    if (CpuName.ToLower().IndexOf("ge") > -1)
                    {
                        Type += " GE";
                    }
                    else if (CpuName.ToLower().IndexOf("g") > -1)
                    {
                        Type += " G";
                    }
                }

                if (Regex.IsMatch(CpuName, "\\d{4}"))
                {
                    int.TryParse(Regex.Match(CpuName, "\\d{4}").Value.Trim(), out Version);
                }
                else if (Regex.IsMatch(CpuName, "\\d{3}"))
                {
                    int.TryParse(Regex.Match(CpuName, "\\d{3}").Value.Trim(), out Version);
                }

                IsOld = CpuName.ToLower().IndexOf("ryzen") == -1;
            }


            // Clock GHz
            Double.TryParse(Regex.Match(CpuName, "[0-9]*[.][0-9]*[ GHz]*").Value.Replace("GHz", string.Empty)
                .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim()
                .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim(), out Clock);
            if (Clock == 0)
            {
                Double.TryParse(Regex.Match(CpuName, "[0-9]*[.][0-9]*[GHz]*").Value.Replace("GHz", string.Empty)
                    .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim()
                    .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim(), out Clock);
            }

            // Clock MHz
            if (Clock == 0)
            {
                Double.TryParse(Regex.Match(CpuName, "[0-9]*[.][0-9]*[ MHz]*").Value.Replace("MHz", string.Empty)
                    .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim()
                    .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim(), out Clock);

                if (Clock == 0)
                {
                    Double.TryParse(Regex.Match(CpuName, "[0-9]*[.][0-9]*[MHz]*").Value.Replace("MHz", string.Empty)
                        .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim()
                        .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim(), out Clock);
                }

                if (Clock != 0)
                {
                    Clock = Clock / 1000;
                }
            }

            if (CpuName.ToLower().IndexOf("single core") > -1)
            {
                IsOld = true;
            }
            if (CpuName.ToLower().IndexOf("dual core") > -1)
            {
                IsOld = true;
            }
            if (CpuName.ToLower().IndexOf("quad core") > -1)
            {
                IsOld = true;
            }

            // No Version
            if (Version == 0)
            {
                IsOld = true;
            }


            return new CpuObject
            {
                Name = CpuName,
                IsIntel = IsIntel,
                IsAmd = IsAmd,
                IsOld = IsOld,
                Type = Type,
                Version = Version,
                Clock = Clock
            };
        }
    }

    public class CpuObject
    {
        public string Name { get; set; }
        public bool IsIntel { get; set; }
        public bool IsAmd { get; set; }
        public bool IsOld { get; set; }

        public string Type { get; set; }
        public int Version { get; set; }
        public double Clock { get; set; }
    }

    public class CpuEquivalence
    {
        public string Intel { get; set; }
        public string Amd { get; set; }
    }
}
