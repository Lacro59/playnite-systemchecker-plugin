using Newtonsoft.Json;
using Playnite.SDK;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
    public class Gpu
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private List<GpuEquivalence> Equivalence = new List<GpuEquivalence>
            {
                new GpuEquivalence {Nvidia="", Amd=""}
            };
        private GpuObject CardPc { get; set; } 
        private GpuObject CardRequierement { get; set; } 


        public Gpu(SystemConfiguration systemConfiguration, string GpuRequierement)
        {
            // VRAM only
            int Vram = 0;
            if (GpuRequierement.ToLower().IndexOf("vram") > -1 && !CallIsNvidia(GpuRequierement) && !CallIsAmd(GpuRequierement))
            {
                int.TryParse(Regex.Replace(GpuRequierement, "[^.0-9]", "").Trim(), out Vram);
                if (Vram > 0)
                {
                    if (GpuRequierement.ToLower().IndexOf("g") > -1)
                    {
                        Vram = Vram * 1024 * 1024;
                    }
                    else
                    {
                        Vram = Vram * 1024;
                    }
                }
            }
            if (GpuRequierement.ToLower().IndexOf("mb") > -1 && !CallIsNvidia(GpuRequierement) && !CallIsAmd(GpuRequierement))
            {
                int.TryParse(Regex.Replace(GpuRequierement, "[^.0-9]", "").Trim(), out Vram);
                if (Vram > 0)
                {
                    Vram = Vram * 1024;
                }
            }
            if (GpuRequierement.ToLower().IndexOf("gb") > -1 && !CallIsNvidia(GpuRequierement) && !CallIsAmd(GpuRequierement))
            {
                int.TryParse(Regex.Replace(GpuRequierement, "[^.0-9]", "").Trim(), out Vram);
                if (Vram > 0)
                {
                    Vram = Vram * 1024 * 1024;
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
            CardRequierement.Vram = Vram;

            CardPc.ResolutionHorizontal = (int)systemConfiguration.CurrentHorizontalResolution;
            CardRequierement.ResolutionHorizontal = ResolutionHorizontal;


            logger.Debug($"Pc({DeleteInfo(systemConfiguration.GpuName)}) " + JsonConvert.SerializeObject(CardPc));
            logger.Debug($"Requierement({DeleteInfo(GpuRequierement)}) " + JsonConvert.SerializeObject(CardRequierement));
        }

        public bool IsBetter()
        {
            // Old card requiered
            if (CardRequierement.IsOld || CardPc.IsOld)
            {
                return true;
            }

            // DirectX
            if (CardRequierement.IsDx)
            {
                if (CardPc.IsIntegrate)
                {
                    if (CardRequierement.DxVersion < 10)
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }

            // No card defined
            if (!CardRequierement.IsIntegrate && !CardRequierement.IsNvidia && !CardRequierement.IsAmd)
            {
                if (CardRequierement.Vram != 0 && CardRequierement.Vram <= CardPc.Vram)
                {
                    return true;
                }
                if (CardRequierement.ResolutionHorizontal != 0 && CardRequierement.ResolutionHorizontal <= CardPc.ResolutionHorizontal)
                {
                    return true;
                }
            }

            // Integrate
            if (CardRequierement.IsIntegrate && (CardPc.IsNvidia || CardPc.IsAmd) && !CardPc.IsOld)
            {
                return true;
            }
            if (CardRequierement.IsIntegrate && CardPc.IsIntegrate)
            {
                if (CardRequierement.Type == CardPc.Type)
                {
                    return (CardRequierement.Number <= CardPc.Number);
                }

                if (CardRequierement.Type == "HD" && CardPc.Type == "UHD")
                {
                    return true;
                }

                if (CardRequierement.Number > 999 && CardPc.Number < 1000)
                {
                    return true;
                }
                if (CardRequierement.Number > 999 && CardPc.Number > 999)
                {
                    return (CardRequierement.Number < CardPc.Number);
                }
                if (CardRequierement.Number < 1000 && CardPc.Number < 1000)
                {
                    return (CardRequierement.Number < CardPc.Number);
                }
            }

            // Nvidia vs Nvidia
            if (CardRequierement.IsNvidia && CardPc.IsNvidia)
            {
                return (CardRequierement.Number <= CardPc.Number);
            }

            // Amd vs Amd
            if (CardRequierement.IsAmd && CardPc.IsAmd)
            {
                if (CardRequierement.Type == CardPc.Type)
                {
                    return (CardRequierement.Number <= CardPc.Number);
                }

                if (CardRequierement.Type == "Radeon HD" && CardRequierement.Type != CardPc.Type)
                {
                    return true;
                }

                switch (CardRequierement.Type + CardPc.Type)
                {
                    case "R5R7":
                        return true;
                    case "R5R9":
                        return true;
                    case "R5RX":
                        return true;
                    case "R7R9":
                        return true;
                    case "R7RX":
                        return true;
                    case "R9RX":
                        return true;
                }
            }


            return false;
        }

        private string DeleteInfo(string GpuName)
        {
            return GpuName.Replace("™", "")
                .Replace("(1024 MB)", "")
                .Replace("(256 MB)", "")
                .Replace("(512 MB)", "")
                .Replace("(1792 MB)", "")
                .Replace("(1 GB)", "")
                .Replace("(2 GB)", "")
                .Replace("(3 GB)", "")
                .Replace("(4 GB)", "")
                .Replace("(6 GB)", "")
                .Replace("(8 GB)", "")
                .Trim();
        }

        private bool CallIsNvidia(string GpuName)
        {
            return (
                GpuName.ToLower().IndexOf("nvidia") > -1 || GpuName.ToLower().IndexOf("geforce") > -1
                || GpuName.ToLower().IndexOf("gtx") > -1 || GpuName.ToLower().IndexOf("rtx") > -1
                );
        }
        private bool CallIsAmd(string GpuName)
        {
            return (GpuName.ToLower().IndexOf("amd") > -1 || GpuName.ToLower().IndexOf("radeon") > -1 || GpuName.ToLower().IndexOf("ati ") > -1);
        }

        private GpuObject SetCard(string GpuName)
        {
            bool IsIntegrate = false;
            bool IsNvidia = false;
            bool IsAmd = false;
            bool IsOld = false;
            bool IsM = false;

            bool IsDx = false;
            int DxVersion = 0;

            string Type = "";
            int Number = 0;

            IsIntegrate = (GpuName.ToLower().IndexOf("intel") > -1);
            IsNvidia = CallIsNvidia(GpuName);
            IsAmd = CallIsAmd(GpuName);

            int.TryParse(Regex.Replace(GpuName.Replace("R5", "").Replace("R7", "").Replace("R9", ""), "[^.0-9]", "").Trim(), out Number);


            #region Check is mobile version
            if (Regex.IsMatch("[0-9]M", GpuName.ToLower()))
            {
                IsM = true;
            }
            if (Regex.IsMatch("M[0-9]", GpuName.ToLower()))
            {
                IsM = true;
            }
            #endregion


            #region Check old
            // Other
            if (GpuName.ToLower().IndexOf("directx") > -1 || Regex.IsMatch(GpuName.ToLower(), "dx[0-9]*"))
            {
                int.TryParse(Regex.Replace(GpuName, @"[^\d]", "").Trim(), out DxVersion);
                if (DxVersion > 0)
                {
                    IsDx = true;
                }
            }
            if (GpuName.ToLower().IndexOf("pretty much any 3d graphics card") > -1)
            {
                IsOld = true;
            }

            if (IsNvidia)
            {
                if (GpuName.ToLower().IndexOf("geforce gt") > -1 && GpuName.ToLower().IndexOf("gtx") == -1)
                {
                    IsOld = true;
                }
                if (Number >= 7000)
                {
                    IsOld = true;
                }

                if (Number < 450)
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


            return new GpuObject
            {
                IsIntegrate = IsIntegrate,
                IsNvidia = IsNvidia,
                IsAmd = IsAmd,
                IsOld = IsOld,
                IsM = IsM,

                IsDx = IsDx,
                DxVersion = DxVersion,

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

        public bool IsDx { get; set; }
        public int DxVersion { get; set; }

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
