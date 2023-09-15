using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using CommonPluginsShared;
using SystemChecker.Models;

namespace SystemChecker.Services
{
    public class Gpu
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private List<GpuEquivalence> Equivalence { get; set; } = new List<GpuEquivalence>
        {
            new GpuEquivalence {Nvidia = "Nvidia GTX 960", Amd = "Amd R9 380"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 760", Amd = "Amd Radeo HD 7870"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 1030", Amd = "Amd RX 550"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 1050", Amd = "Amd RX 560"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 1060", Amd = "Amd RX 580"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 1660", Amd = "Amd RX 590"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 970", Amd = "Amd RX 570"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 970", Amd = "Amd R9 390"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 660", Amd = "Amd Radeon HD 7850"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 670", Amd = "Amd Radep HD 7870"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 780", Amd = "Amd Radeon R9 290"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 1070", Amd = "Amd Radeon RX Vega 56"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 1070", Amd = "Amd Radeon 5600 XT"},
            new GpuEquivalence {Nvidia = "Nvidia RTX 2060", Amd = "Amd Radeon RX Vega 56"},
            new GpuEquivalence {Nvidia = "Nvidia RTX 2060", Amd = "Amd Radeon 5600 XT"},
            new GpuEquivalence {Nvidia = "Nvidia GTX 1080", Amd = "Amd Radeon RX Vega 64"},
            new GpuEquivalence {Nvidia = "Nvidia RTX 2060 SUPER", Amd = "Amd Radeon RX Vega 64"},
            new GpuEquivalence {Nvidia = "Nvidia RTX 2060 SUPER", Amd = "Amd Radeon 5700"},
            new GpuEquivalence {Nvidia = "Nvidia RTX 2070", Amd = "Amd Radeon RX Vega 64"},
            new GpuEquivalence {Nvidia = "Nvidia RTX 2070", Amd = "Amd Radeon 5700 XT"}
        };
        private string CardPcName { get; set; }
        private GpuObject CardPc { get; set; }
        private string CardRequierementName { get; set; }
        private GpuObject CardRequierement { get; set; }

        public bool IsWithNoCard = false;
        public bool IsIntegrate => CardPc.IsIntegrate;
        public bool CardRequierementIsOld => CardRequierement.IsOld; 

        public Gpu(SystemConfiguration systemConfiguration, string GpuRequierement)
        {
            CardPcName = DeleteInfo(systemConfiguration.GpuName);
            CardRequierementName = DeleteInfo(GpuRequierement);


            // VRAM only
            double Vram = 0;
            string TempVram = string.Empty;
            if (GpuRequierement.ToLower().IndexOf("vram") > -1 && !CallIsNvidia(GpuRequierement) && !CallIsAmd(GpuRequierement))
            {
                TempVram = GpuRequierement.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
                TempVram = Regex.Replace(TempVram, "vram", string.Empty, RegexOptions.IgnoreCase);

                if (TempVram.ToLower().IndexOf("mb") > -1 && !CallIsNvidia(GpuRequierement) && !CallIsAmd(GpuRequierement))
                {
                    double.TryParse(Regex.Replace(TempVram, "mb", string.Empty, RegexOptions.IgnoreCase).Trim(), out Vram);
                    if (Vram > 0)
                    {
                        Vram = Vram * 1024;
                    }
                }
                if (TempVram.ToLower().IndexOf("gb") > -1 && !CallIsNvidia(GpuRequierement) && !CallIsAmd(GpuRequierement))
                {
                    double.TryParse(Regex.Replace(TempVram, "gb", string.Empty, RegexOptions.IgnoreCase).Trim(), out Vram);
                    if (Vram > 0)
                    {
                        Vram = Vram * 1024 * 1024;
                    }
                }
            }

            // Rezolution only
            int ResolutionHorizontal = 0;
            if (GpuRequierement.ToLower().IndexOf("1280×720") > -1 || GpuRequierement.ToLower().IndexOf("1280 × 720") > -1)
            {
                ResolutionHorizontal = 1280;
            }
            if (GpuRequierement.ToLower().IndexOf("1368×") > -1 || GpuRequierement.ToLower().IndexOf("1368 ×") > -1)
            {
                ResolutionHorizontal = 1368;
            }
            if (GpuRequierement.ToLower().IndexOf("1600×") > -1 || GpuRequierement.ToLower().IndexOf("1600 ×") > -1)
            {
                ResolutionHorizontal = 1600;
            }
            if (GpuRequierement.ToLower().IndexOf("1920×") > -1 || GpuRequierement.ToLower().IndexOf("1920 ×") > -1)
            {
                ResolutionHorizontal = 1920;
            }


            CardPc = SetCard(DeleteInfo(systemConfiguration.GpuName));
            CardRequierement = SetCard(DeleteInfo(GpuRequierement));


            CardPc.Vram = systemConfiguration.GpuRam;
            CardRequierement.Vram = (long)Vram;

            CardPc.ResolutionHorizontal = (int)systemConfiguration.CurrentHorizontalResolution;
            CardRequierement.ResolutionHorizontal = ResolutionHorizontal;
        }

        public CheckResult IsBetter()
        {
            Common.LogDebug(true, $"Gpu.IsBetter - CardPc({CardPcName}): {Serialization.ToJson(CardPc)}");
            Common.LogDebug(true, $"Gpu.IsBetter - CardRequierement({CardRequierementName}): {Serialization.ToJson(CardRequierement)}");

            // DirectX
            if (CardRequierement.IsDx)
            {
                if (CardPc.IsIntegrate)
                {
                    if (CardRequierement.DxVersion < 10)
                    {
                        IsWithNoCard = true;
                        return new CheckResult { Result = true };
                    }
                }
                else
                {
                    IsWithNoCard = true;
                    return new CheckResult { Result = true };
                }
            }

            // OpenGL
            if (CardRequierement.IsOGL && CardRequierement.OglVersion < 4)
            {
                IsWithNoCard = true;
                return new CheckResult { Result = true };
            }

            // No card defined
            if (!CardRequierement.IsIntegrate && !CardRequierement.IsNvidia && !CardRequierement.IsAmd)
            {
                if (CardRequierement.Vram != 0 && CardRequierement.Vram <= CardPc.Vram)
                {
                    IsWithNoCard = true;
                    return new CheckResult { Result = true };
                }
                if (CardRequierement.ResolutionHorizontal != 0 && CardRequierement.ResolutionHorizontal <= CardPc.ResolutionHorizontal)
                {
                    IsWithNoCard = true;
                    return new CheckResult { Result = true };
                }
            }

            // Old card requiered
            if (CardRequierement.IsOld || CardPc.IsOld)
            {
                return new CheckResult { Result = true };
            }

            // Integrate
            if (CardRequierement.IsIntegrate && (CardPc.IsNvidia || CardPc.IsAmd) && !CardPc.IsOld)
            {
                return new CheckResult { Result = true };
            }
            if (CardRequierement.IsIntegrate && CardPc.IsIntegrate)
            {
                if (CardRequierement.Type == CardPc.Type)
                {
                    return new CheckResult { Result = CardRequierement.Number <= CardPc.Number };
                }

                if (CardRequierement.Type == "HD" && CardPc.Type == "UHD")
                {
                    return new CheckResult { Result = true };
                }

                if (CardRequierement.Number > 999 && CardPc.Number < 1000)
                {
                    return new CheckResult { Result = true };
                }
                if (CardRequierement.Number > 999 && CardPc.Number > 999)
                {
                    return new CheckResult { Result = CardRequierement.Number < CardPc.Number };
                }
                if (CardRequierement.Number < 1000 && CardPc.Number < 1000)
                {
                    return new CheckResult { Result = CardRequierement.Number < CardPc.Number };
                }
            }

            // Nvidia vs Nvidia
            if (CardRequierement.IsNvidia && CardPc.IsNvidia)
            {
                return new CheckResult { SameConstructor = true, Result = CardRequierement.Number <= CardPc.Number };
            }

            // Amd vs Amd
            if (CardRequierement.IsAmd && CardPc.IsAmd)
            {
                if (CardRequierement.Type == CardPc.Type)
                {
                    return new CheckResult { SameConstructor = true, Result = CardRequierement.Number <= CardPc.Number };
                }

                if (CardRequierement.Type == "Radeon HD" && CardRequierement.Type != CardPc.Type)
                {
                    return new CheckResult { SameConstructor = true, Result = true };
                }

                switch (CardRequierement.Type + CardPc.Type)
                {
                    case "R5R7":
                        return new CheckResult { SameConstructor = true, Result = true };
                    case "R5R9":
                        return new CheckResult { SameConstructor = true, Result = true };
                    case "R5RX":
                        return new CheckResult { SameConstructor = true, Result = true };
                    case "R7R9":
                        return new CheckResult { SameConstructor = true, Result = true };
                    case "R7RX":
                        return new CheckResult { SameConstructor = true, Result = true };
                    case "R9RX":
                        return new CheckResult { SameConstructor = true, Result = true };
                }
            }

            // Nvidia vs Amd
            if (CardRequierement.IsNvidia && CardPc.IsAmd)
            {
                string gpuEquivalenceName = string.Empty;
                try
                {
                    List<GpuEquivalence> EquivalenceAmd = Equivalence.FindAll(x => x.Amd.ToLower().Contains(CardRequierement.Number.ToString()) && x.Amd.ToLower().Contains(CardRequierement.Type)).ToList();

                    CheckResult tempValue = new CheckResult();
                    foreach (GpuEquivalence gpuEquivalence in EquivalenceAmd)
                    {
                        gpuEquivalenceName = gpuEquivalence.Amd;
                        CardRequierement = SetCard(gpuEquivalenceName);

                        tempValue = IsBetter();
                        if (tempValue.Result)
                        {
                            return tempValue;
                        }
                    }

                    logger.Warn($"No equivalence for {Serialization.ToJson(CardPc)} & {Serialization.ToJson(CardRequierement)}");
                    return new CheckResult(); 
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on IsBetter() for Nvidia vs Amd", true, "SystemChecker");
                }
            }

            // Amd vs Nvidia
            if (CardRequierement.IsAmd && CardPc.IsNvidia)
            {
                string gpuEquivalenceName = string.Empty;
                try
                {
                    List<GpuEquivalence> EquivalenceNvidia = Equivalence.FindAll(x => x.Nvidia.ToLower().Contains(CardRequierement.Number.ToString()) && x.Nvidia.ToLower().Contains(CardRequierement.Type)).ToList();

                    CheckResult tempValue = new CheckResult();
                    foreach (GpuEquivalence gpuEquivalence in EquivalenceNvidia)
                    {
                        gpuEquivalenceName = gpuEquivalence.Nvidia;
                        CardRequierement = SetCard(gpuEquivalenceName);

                        tempValue = IsBetter();
                        if (tempValue.Result)
                        {
                            return tempValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Error on IsBetter() for Amd vs Nvidia", true, "SystemChecker");
                }

                logger.Warn($"No equivalence for {Serialization.ToJson(CardPc)} & {Serialization.ToJson(CardRequierement)}");
                return new CheckResult();
            }

            logger.Warn($"No GPU treatment for {Serialization.ToJson(CardPc)} & {Serialization.ToJson(CardRequierement)}");
            return new CheckResult();
        }

        private string DeleteInfo(string GpuName)
        {
            return GpuName.Replace("™", string.Empty)
                .Replace("(1024 MB)", string.Empty)
                .Replace("(256 MB)", string.Empty)
                .Replace("(512 MB)", string.Empty)
                .Replace("(1792 MB)", string.Empty)
                .Replace("(1 GB)", string.Empty)
                .Replace("(2 GB)", string.Empty)
                .Replace("(3 GB)", string.Empty)
                .Replace("(4 GB)", string.Empty)
                .Replace("(6 GB)", string.Empty)
                .Replace("(8 GB)", string.Empty)
                .Trim();
        }

        public static bool CallIsNvidia(string GpuName)
        {
            return (
                (GpuName.ToLower().IndexOf("nvidia") > -1 || GpuName.ToLower().IndexOf("geforce") > -1 || GpuName.ToLower().IndexOf("gtx") > -1 || GpuName.ToLower().IndexOf("rtx") > -1) 
                && GpuName.ToLower().IndexOf("(nvidia)") == -1
                );
        }
        public static bool CallIsAmd(string GpuName)
        {
            return ((GpuName.ToLower().IndexOf("amd") > -1 || GpuName.ToLower().IndexOf("radeon") > -1 || GpuName.ToLower().IndexOf("ati ") > -1) 
                && GpuName.ToLower().IndexOf("(amd)") == -1);
        }
        public static bool CallIsIntel(string GpuName)
        {
            return GpuName.ToLower().IndexOf("intel") > -1;
        }

        private GpuObject SetCard(string GpuName)
        {
            bool IsIntegrate = false;
            bool IsNvidia = false;
            bool IsAmd = false;
            bool IsOld = false;
            bool IsM = false;

            bool IsOgl = false;
            bool IsDx = false;
            int DxVersion = 0;
            int OglVersion = 0;

            string Type = string.Empty;
            int Number = 0;

            IsIntegrate = GpuName.Contains("intel", StringComparison.InvariantCultureIgnoreCase) || GpuName.Contains("vega", StringComparison.InvariantCultureIgnoreCase);
            IsNvidia = CallIsNvidia(GpuName);
            IsAmd = CallIsAmd(GpuName);

            int.TryParse(Regex.Replace(GpuName.Replace("R5", string.Empty).Replace("R7", string.Empty).Replace("R9", string.Empty), "[^.0-9]", string.Empty).Trim(), out Number);


            #region Check is mobile version
            if (Regex.IsMatch("[0-9]m", GpuName.ToLower(), RegexOptions.IgnoreCase))
            {
                IsM = true;
            }
            if (Regex.IsMatch("m[0-9]", GpuName.ToLower(), RegexOptions.IgnoreCase))
            {
                IsM = true;
            }
            #endregion


            #region Check old & other
            // Other
            if (GpuName.ToLower().IndexOf("directx") > -1 || Regex.IsMatch(GpuName.ToLower(), "dx[0-9]*"))
            {
                int.TryParse(Regex.Replace(GpuName, @"[^\d]", string.Empty).Trim(), out DxVersion);
                if (DxVersion > 0)
                {
                    IsDx = true;

                    if (DxVersion > 50)
                    {
                        DxVersion = int.Parse(DxVersion.ToString().Substring(0, DxVersion.ToString().Length - 1));
                    }
                }
            }
            if (GpuName.ToLower().IndexOf("pretty much any 3d graphics card") > -1 || GpuName.ToLower().IndexOf("integrat") > -1)
            {
                IsOld = true;
            }
            if (GpuName.ToLower().IndexOf("svga") > -1)
            {
                IsOld = true;
            }
            if (GpuName.ToLower().IndexOf("opengl") > -1 || GpuName.ToLower().IndexOf("open gl") > -1)
            {
                IsOgl = true;
                int.TryParse(
                    GpuName.ToLower().Replace("opengl", string.Empty).Replace("open gl", string.Empty)
                    .Replace(".0", string.Empty).Trim(),
                    out OglVersion
                );
            }
            if (GpuName.ToLower().IndexOf("direct3d") > -1)
            {
                IsOld = true;
            }

            if (IsNvidia)
            {
                if (GpuName.ToLower().IndexOf("geforce gt") > -1 && GpuName.ToLower().IndexOf("gtx") == -1)
                {
                    IsOld = true;
                }
                if (Number >= 5000 && GpuName.ToLower().IndexOf("rtx") == -1)
                {
                    IsOld = true;
                }
                if (Number < 450)
                {
                    IsOld = true;
                }
                if (Regex.IsMatch(GpuName.ToLower(), "geforce[0-9]"))
                {
                    IsOld = true;
                }
                if (GpuName.ToLower().IndexOf("geforce fx") > -1)
                {
                    IsOld = true;
                }
            }

            if (IsAmd)
            {
                if (GpuName.ToLower().IndexOf("radeon x") > -1)
                {
                    IsOld = true;
                }
                if (GpuName.ToLower().IndexOf("radeon hd") > -1 && Number < 7000)
                {
                    IsOld = true;
                }
                if (GpuName.ToLower().IndexOf("radeon r") > -1 && Number < 300)
                {
                    IsOld = true;
                }
                if (Regex.IsMatch(GpuName.ToLower(), "radeon [0-9]"))
                {
                    IsOld = true;
                }
            }
            #endregion


            #region Type
            if (!IsOld)
            {
                if (IsIntegrate)
                {
                    if (GpuName.ToLower().IndexOf("uhd") > -1)
                    {
                        Type = "UHD";
                    }
                    if (GpuName.ToLower().IndexOf(" hd") > -1)
                    {
                        Type = "HD";
                    }
                }

                if (IsNvidia)
                {
                    if (GpuName.ToLower().IndexOf("gts") > -1)
                    {
                        Type = "gts";
                    }
                    if (GpuName.ToLower().IndexOf("gtx") > -1)
                    {
                        Type = "GTX";
                    }
                    if (GpuName.ToLower().IndexOf("rtx") > -1)
                    {
                        Type = "RTX";
                    }
                }

                if (IsAmd)
                {
                    if (GpuName.ToLower().IndexOf("radeon hd") > -1)
                    {
                        Type = "Radeon HD";
                    }
                    if (GpuName.ToLower().IndexOf("r5") > -1)
                    {
                        Type = "R5";
                    }
                    if (GpuName.ToLower().IndexOf("r7") > -1)
                    {
                        Type = "R7";
                    }
                    if (GpuName.ToLower().IndexOf("r9") > -1)
                    {
                        Type = "R9";
                    }
                    if (GpuName.ToLower().IndexOf("rx") > -1)
                    {
                        Type = "RX";
                    }
                }
            }
            #endregion


            if (!IsAmd && !IsNvidia && !IsIntegrate && !IsDx)
            {
                IsOld = true;
            }


            if (GpuName.Contains("(nvidia)", StringComparison.OrdinalIgnoreCase) && GpuName.Contains("(amd)", StringComparison.OrdinalIgnoreCase))
            {
                IsOld = false;
            }


            return new GpuObject
            {
                IsIntegrate = IsIntegrate,
                IsNvidia = IsNvidia,
                IsAmd = IsAmd,
                IsOld = IsOld,
                IsM = IsM,

                IsOGL = IsOgl,
                IsDx = IsDx,
                DxVersion = DxVersion,
                OglVersion = OglVersion,

                Type = Type,
                Number = Number,
                Vram = 0,
                ResolutionHorizontal = 0,
            };
        }
    }

    public class GpuObject
    {
        public bool IsIntegrate { get; set; }
        public bool IsNvidia { get; set; }
        public bool IsAmd { get; set; }
        public bool IsOld { get; set; }
        public bool IsM { get; set; }

        public bool IsOGL { get; set; }
        public bool IsDx { get; set; }
        public int DxVersion { get; set; }
        public int OglVersion { get; set; }

        public string Type { get; set; }
        public int Number { get; set; }
        public long Vram { get; set; }
        public int ResolutionHorizontal { get; set; }
    }

    public class GpuEquivalence
    {
        public string Nvidia { get; set; }
        public string Amd { get; set; }
    }
}
