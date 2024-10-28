using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using CommonPluginsShared;
using SystemChecker.Models;
using SystemChecker.Clients;

namespace SystemChecker.Services
{
    public class Gpu
    {
        private static readonly ILogger logger = LogManager.GetLogger();

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
            double vram = 0;
            string tempVram = string.Empty;
            if (GpuRequierement.ToLower().IndexOf("vram") > -1 && !CallIsNvidia(GpuRequierement) && !CallIsAmd(GpuRequierement))
            {
                tempVram = GpuRequierement.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
                tempVram = Regex.Replace(tempVram, "vram", string.Empty, RegexOptions.IgnoreCase);

                if (tempVram.ToLower().IndexOf("mb") > -1 && !CallIsNvidia(GpuRequierement) && !CallIsAmd(GpuRequierement))
                {
                    _ = double.TryParse(Regex.Replace(tempVram, "mb", string.Empty, RegexOptions.IgnoreCase).Trim(), out vram);
                    if (vram > 0)
                    {
                        vram = vram * 1024;
                    }
                }
                if (tempVram.ToLower().IndexOf("gb") > -1 && !CallIsNvidia(GpuRequierement) && !CallIsAmd(GpuRequierement))
                {
                    _ = double.TryParse(Regex.Replace(tempVram, "gb", string.Empty, RegexOptions.IgnoreCase).Trim(), out vram);
                    if (vram > 0)
                    {
                        vram = vram * 1024 * 1024;
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
            CardRequierement.Vram = (long)vram;

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
                    if (CardRequierement.DxVersion < 12)
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
            if (CardRequierement.IsOld && !CardPc.IsOld)
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


            Benchmark benchmark = new Benchmark();
            bool? isBetter = benchmark.IsBetterGpu(CardPcName, CardRequierementName);
            if (isBetter != null)
            {
                return new CheckResult
                {
                    Result = (bool)isBetter,
                    SameConstructor = true
                };
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

            }

            // Amd vs Nvidia
            if (CardRequierement.IsAmd && CardPc.IsNvidia)
            {

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
            return Regex.IsMatch(GpuName, @"nvidia", RegexOptions.IgnoreCase)
                || Regex.IsMatch(GpuName, @"geforce", RegexOptions.IgnoreCase)
                || Regex.IsMatch(GpuName, @"rtx", RegexOptions.IgnoreCase)
                || Regex.IsMatch(GpuName, @"gts", RegexOptions.IgnoreCase)
                || Regex.IsMatch(GpuName, @"gtx", RegexOptions.IgnoreCase);
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
            bool isIntegrate = false;
            bool isNvidia = false;
            bool isAmd = false;
            bool isOld = false;
            bool isM = false;

            bool isOgl = false;
            bool isDx = false;
            int dxVersion = 0;
            int oglVersion = 0;

            string type = string.Empty;

            isIntegrate = GpuName.Contains("intel", StringComparison.InvariantCultureIgnoreCase) || GpuName.Contains("vega", StringComparison.InvariantCultureIgnoreCase);
            isNvidia = CallIsNvidia(GpuName);
            isAmd = CallIsAmd(GpuName);

            _ = int.TryParse(Regex.Replace(GpuName.Replace("R5", string.Empty).Replace("R7", string.Empty).Replace("R9", string.Empty), "[^.0-9]", string.Empty).Trim(), out int Number);

            #region Check is mobile version
            if (Regex.IsMatch(GpuName.ToLower(), "[0-9]m", RegexOptions.IgnoreCase))
            {
                isM = true;
            }
            if (Regex.IsMatch(GpuName.ToLower(), "m[0-9]", RegexOptions.IgnoreCase))
            {
                isM = true;
            }
            #endregion


            #region Check old & other
            // Other
            if (GpuName.ToLower().IndexOf("directx") > -1 || Regex.IsMatch(GpuName.ToLower(), "dx[0-9]*"))
            {
                isDx = true;
                _ = int.TryParse(Regex.Replace(GpuName, @"[^\d]", string.Empty).Trim(), out dxVersion);
                if (dxVersion > 0)
                {
                    if (dxVersion > 50)
                    {
                        dxVersion = int.Parse(dxVersion.ToString().Substring(0, dxVersion.ToString().Length - 1));
                    }
                }
                else
                {
                    dxVersion = 8;
                }
            }
            if (GpuName.ToLower().IndexOf("pretty much any 3d graphics card") > -1 || GpuName.ToLower().IndexOf("integrat") > -1)
            {
                isOld = true;
            }
            if (GpuName.ToLower().IndexOf("svga") > -1)
            {
                isOld = true;
            }
            if (GpuName.ToLower().IndexOf("opengl") > -1 || GpuName.ToLower().IndexOf("open gl") > -1)
            {
                isOgl = true;
                int.TryParse(
                    GpuName.ToLower().Replace("opengl", string.Empty).Replace("open gl", string.Empty)
                    .Replace(".0", string.Empty).Trim(),
                    out oglVersion
                );
            }
            if (GpuName.ToLower().IndexOf("direct3d") > -1)
            {
                isOld = true;
            }

            if (isNvidia)
            {
                if (GpuName.ToLower().IndexOf("geforce gt") > -1 && GpuName.ToLower().IndexOf("gtx") == -1)
                {
                    isOld = true;
                }
                if (Number >= 5000 && GpuName.ToLower().IndexOf("rtx") == -1)
                {
                    isOld = true;
                }
                if (Number == 4200 || Number == 4400 || Number == 4600 || Number == 4800)
                {
                    isOld = true;
                }
                if (Number < 450)
                {
                    isOld = true;
                }
                if (Regex.IsMatch(GpuName.ToLower(), "geforce[0-9]"))
                {
                    isOld = true;
                }
                if (GpuName.ToLower().IndexOf("geforce fx") > -1)
                {
                    isOld = true;
                }
            }

            if (isAmd)
            {
                if (GpuName.ToLower().IndexOf("radeon x") > -1)
                {
                    isOld = true;
                }
                if (GpuName.ToLower().IndexOf("radeon hd") > -1 && Number < 7000)
                {
                    isOld = true;
                }
                if (GpuName.ToLower().IndexOf("radeon r") > -1 && Number < 300)
                {
                    isOld = true;
                }
                if (Regex.IsMatch(GpuName.ToLower(), "radeon [0-9]"))
                {
                    isOld = true;
                }
            }
            #endregion


            #region Type
            if (!isOld)
            {
                if (isIntegrate)
                {
                    if (GpuName.ToLower().IndexOf("uhd") > -1)
                    {
                        type = "UHD";
                    }
                    if (GpuName.ToLower().IndexOf(" hd") > -1)
                    {
                        type = "HD";
                    }
                }

                if (isNvidia)
                {
                    if (GpuName.ToLower().IndexOf("gts") > -1)
                    {
                        type = "gts";
                    }
                    if (GpuName.ToLower().IndexOf("gtx") > -1)
                    {
                        type = "GTX";
                    }
                    if (GpuName.ToLower().IndexOf("rtx") > -1)
                    {
                        type = "RTX";
                    }
                }

                if (isAmd)
                {
                    if (GpuName.ToLower().IndexOf("radeon hd") > -1)
                    {
                        type = "Radeon HD";
                    }
                    if (GpuName.ToLower().IndexOf("r5") > -1)
                    {
                        type = "R5";
                    }
                    if (GpuName.ToLower().IndexOf("r7") > -1)
                    {
                        type = "R7";
                    }
                    if (GpuName.ToLower().IndexOf("r9") > -1)
                    {
                        type = "R9";
                    }
                    if (GpuName.ToLower().IndexOf("rx") > -1)
                    {
                        type = "RX";
                    }
                }
            }
            #endregion


            if (!isAmd && !isNvidia && !isIntegrate && !isDx)
            {
                isOld = true;
            }


            if (GpuName.Contains("(nvidia)", StringComparison.OrdinalIgnoreCase) && GpuName.Contains("(amd)", StringComparison.OrdinalIgnoreCase))
            {
                isOld = false;
            }


            return new GpuObject
            {
                IsIntegrate = isIntegrate,
                IsNvidia = isNvidia,
                IsAmd = isAmd,
                IsOld = isOld,
                IsM = isM,

                IsOGL = isOgl,
                IsDx = isDx,
                DxVersion = dxVersion,
                OglVersion = oglVersion,

                Type = type,
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
}
