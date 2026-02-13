using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using CommonPluginsShared;
using SystemChecker.Models;
using SystemChecker.Clients;

namespace SystemChecker.Services
{
	public class Gpu : HardwareChecker
	{
		private static readonly Regex VramRegex = new Regex(@"vram", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex MbRegex = new Regex(@"mb", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex GbRegex = new Regex(@"gb", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex NumbersOnlyRegex = new Regex(@"[^.0-9]", RegexOptions.Compiled);
		private static readonly Regex DigitMRegex = new Regex(@"[0-9]m", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex MDigitRegex = new Regex(@"m[0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex DxRegex = new Regex(@"dx[0-9]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex NonDigitsRegex = new Regex(@"[^\d]", RegexOptions.Compiled);
		private static readonly Regex GeforceDigitRegex = new Regex(@"geforce[0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex RadeonDigitRegex = new Regex(@"radeon [0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex SingleDigitRegex = new Regex(@"\d", RegexOptions.Compiled);

		protected override string CheckType => "GPU";
		protected override string ComponentPcName => _cardPcName;
		protected override string ComponentRequirementName => _cardRequirementName;

		private string _cardPcName;
		private GpuObject CardPc { get; set; }
		private string _cardRequirementName;
		private GpuObject CardRequirement { get; set; }

		public bool IsWithNoCard = false;
		public bool IsIntegrate => CardPc.IsIntegrate;
		public bool CardRequirementIsOld => CardRequirement.IsOld;

		public Gpu(SystemConfiguration systemConfiguration, string gpuRequirement)
		{
			_cardPcName = DeleteInfo(systemConfiguration.GpuName);
			_cardRequirementName = DeleteInfo(gpuRequirement);

			string gpuLower = gpuRequirement.ToLower();

			double vram = 0;
			if (gpuLower.IndexOf("vram") > -1 && !CallIsNvidia(gpuRequirement) && !CallIsAmd(gpuRequirement))
			{
				string tempVram = gpuRequirement.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
				tempVram = VramRegex.Replace(tempVram, string.Empty);

				if (gpuLower.IndexOf("mb") > -1)
				{
					if (double.TryParse(MbRegex.Replace(tempVram, string.Empty).Trim(), out vram) && vram > 0)
					{
						vram *= 1024;
					}
				}
				else if (gpuLower.IndexOf("gb") > -1)
				{
					if (double.TryParse(GbRegex.Replace(tempVram, string.Empty).Trim(), out vram) && vram > 0)
					{
						vram *= 1024 * 1024;
					}
				}
			}

			int ResolutionHorizontal = 0;
			if (gpuLower.IndexOf("1280") > -1 && (gpuLower.IndexOf("×720") > -1 || gpuLower.IndexOf("× 720") > -1))
			{
				ResolutionHorizontal = 1280;
			}
			else if (gpuLower.IndexOf("1368×") > -1 || gpuLower.IndexOf("1368 ×") > -1)
			{
				ResolutionHorizontal = 1368;
			}
			else if (gpuLower.IndexOf("1600×") > -1 || gpuLower.IndexOf("1600 ×") > -1)
			{
				ResolutionHorizontal = 1600;
			}
			else if (gpuLower.IndexOf("1920×") > -1 || gpuLower.IndexOf("1920 ×") > -1)
			{
				ResolutionHorizontal = 1920;
			}

			CardPc = SetCard(DeleteInfo(systemConfiguration.GpuName));
			CardRequirement = SetCard(DeleteInfo(gpuRequirement));

			CardPc.Vram = systemConfiguration.GpuRam;
			CardRequirement.Vram = (long)vram;
			CardPc.ResolutionHorizontal = (int)systemConfiguration.CurrentHorizontalResolution;
			CardRequirement.ResolutionHorizontal = ResolutionHorizontal;
		}

		protected override CheckResult PerformCheck()
		{
			Common.LogDebug(true, $"Gpu.PerformCheck - CardPc({_cardPcName}): {Serialization.ToJson(CardPc)}");
			Common.LogDebug(true, $"Gpu.PerformCheck - CardRequirement({_cardRequirementName}): {Serialization.ToJson(CardRequirement)}");

			if (CardRequirement.IsDx)
			{
				if (CardPc.IsIntegrate)
				{
					if (CardRequirement.DxVersion < 12)
					{
						IsWithNoCard = true;
						return new CheckResult { Result = true };
					}
					else
					{
						IsWithNoCard = true;
						return new CheckResult { Result = true };
					}
				}
			}

			if (CardRequirement.IsOGL && CardRequirement.OglVersion < 4)
			{
				IsWithNoCard = true;
				return new CheckResult { Result = true };
			}

			if (!CardRequirement.IsIntegrate && !CardRequirement.IsNvidia && !CardRequirement.IsAmd)
			{
				if (CardRequirement.Vram != 0 && CardRequirement.Vram <= CardPc.Vram)
				{
					IsWithNoCard = true;
					return new CheckResult { Result = true };
				}

				if (CardRequirement.ResolutionHorizontal != 0 && CardRequirement.ResolutionHorizontal <= CardPc.ResolutionHorizontal)
				{
					IsWithNoCard = true;
					return new CheckResult { Result = true };
				}
			}

			if (CardRequirement.IsOld && !CardPc.IsOld)
			{
				return new CheckResult { Result = true };
			}

			if (CardRequirement.IsIntegrate && (CardPc.IsNvidia || CardPc.IsAmd) && !CardPc.IsOld)
			{
				return new CheckResult { Result = true };
			}

			if (CardRequirement.IsIntegrate && CardPc.IsIntegrate)
			{
				if (CardRequirement.Type == CardPc.Type)
				{
					return new CheckResult { Result = CardRequirement.Number <= CardPc.Number };
				}

				if (CardRequirement.Type == "HD" && CardPc.Type == "UHD")
				{
					return new CheckResult { Result = true };
				}

				if (CardRequirement.Number != 0 && CardPc.Number != 0)
				{
					if (CardRequirement.Number > 999 && CardPc.Number < 1000)
					{
						return new CheckResult { Result = true };
					}

					if (CardRequirement.Number > 999 && CardPc.Number > 999)
					{
						return new CheckResult { Result = CardRequirement.Number < CardPc.Number };
					}

					if (CardRequirement.Number < 1000 && CardPc.Number < 1000)
					{
						return new CheckResult { Result = CardRequirement.Number < CardPc.Number };
					}
				}
				else
				{
					Logger.Warn($"No GPU treatment for {_cardPcName}: {Serialization.ToJson(CardPc)} & {_cardRequirementName}: {Serialization.ToJson(CardRequirement)}");
					return new CheckResult { Result = true, SameConstructor = true };
				}
			}

			bool? isBetter = CallBenchmark(_cardPcName, _cardRequirementName, true);
			if (isBetter != null)
			{
				return new CheckResult
				{
					Result = (bool)isBetter,
					SameConstructor = true
				};
			}

			if (CardRequirement.IsNvidia && CardPc.IsNvidia)
			{
				return new CheckResult { SameConstructor = true, Result = CardRequirement.Number <= CardPc.Number };
			}

			if (CardRequirement.IsAmd && CardPc.IsAmd)
			{
				if (CardRequirement.Type == CardPc.Type)
				{
					return new CheckResult { SameConstructor = true, Result = CardRequirement.Number <= CardPc.Number };
				}

				if (CardRequirement.Type == "Radeon HD" && CardRequirement.Type != CardPc.Type)
				{
					return new CheckResult { SameConstructor = true, Result = true };
				}

				switch (CardRequirement.Type + CardPc.Type)
				{
					case "R5R7":
					case "R5R9":
					case "R5RX":
					case "R7R9":
					case "R7RX":
					case "R9RX":
						return new CheckResult { SameConstructor = true, Result = true };
				}
			}

			Logger.Warn($"No GPU treatment for {_cardPcName}: {Serialization.ToJson(CardPc)} & {_cardRequirementName}: {Serialization.ToJson(CardRequirement)}");
			return new CheckResult();
		}

		public static bool CallIsNvidia(string gpuName)
		{
			return gpuName.IndexOf("nvidia", StringComparison.OrdinalIgnoreCase) > -1
				|| gpuName.IndexOf("geforce", StringComparison.OrdinalIgnoreCase) > -1
				|| gpuName.IndexOf("gtx", StringComparison.OrdinalIgnoreCase) > -1
				|| gpuName.IndexOf("rtx", StringComparison.OrdinalIgnoreCase) > -1;
		}

		public static bool CallIsAmd(string gpuName)
		{
			return gpuName.IndexOf("amd", StringComparison.OrdinalIgnoreCase) > -1
				|| gpuName.IndexOf("ati", StringComparison.OrdinalIgnoreCase) > -1
				|| gpuName.IndexOf("radeon", StringComparison.OrdinalIgnoreCase) > -1;
		}

		public static bool CallIsIntel(string gpuName)
		{
			return gpuName.IndexOf("intel", StringComparison.OrdinalIgnoreCase) > -1;
		}

		private static string DeleteInfo(string gpuName)
		{
			return gpuName
				.Replace("(R)", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("(TM)", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("(tm)", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("Graphics", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Trim();
		}

		private GpuObject SetCard(string gpuName)
		{
			bool isIntegrate = gpuName.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) > -1;
			bool isNvidia = CallIsNvidia(gpuName);
			bool isAmd = CallIsAmd(gpuName);
			bool isOld = false;
			bool isM = false;
			bool isOgl = false;
			bool isDx = false;
			int dxVersion = 0;
			int oglVersion = 0;
			string type = string.Empty;

			int.TryParse(NumbersOnlyRegex.Replace(
				gpuName.Replace("R5", string.Empty).Replace("R7", string.Empty).Replace("R9", string.Empty),
				string.Empty).Trim(),
				out int Number);

			if (DigitMRegex.IsMatch(gpuName) || MDigitRegex.IsMatch(gpuName))
			{
				isM = true;
			}

			string gpuLower = gpuName.ToLower();

			if (gpuLower.IndexOf("directx") > -1 || DxRegex.IsMatch(gpuLower))
			{
				isDx = true;
				if (int.TryParse(NonDigitsRegex.Replace(gpuName, string.Empty).Trim(), out dxVersion) && dxVersion > 50)
				{
					dxVersion = int.Parse(dxVersion.ToString().Substring(0, dxVersion.ToString().Length - 1));
				}
				else if (dxVersion == 0)
				{
					dxVersion = 8;
				}
			}

			if (gpuLower.IndexOf("pretty much any 3d graphics card") > -1
				|| gpuLower.IndexOf("integrat") > -1
				|| gpuLower.IndexOf("svga") > -1
				|| gpuLower.IndexOf("direct3d") > -1)
			{
				isOld = true;
			}

			if (gpuLower.IndexOf("opengl") > -1 || gpuLower.IndexOf("open gl") > -1)
			{
				isOgl = true;
				string temp = gpuLower.Replace("opengl", string.Empty).Replace("open gl", string.Empty).Replace(".0", string.Empty).Trim();
				int.TryParse(temp, out oglVersion);
			}

			if (isNvidia)
			{
				if (gpuLower.IndexOf("geforce gt") > -1 && gpuLower.IndexOf("gtx") == -1)
				{
					isOld = true;
				}

				if (Number >= 5000 && gpuLower.IndexOf("rtx") == -1)
				{
					isOld = true;
				}

				if (Number == 4200 || Number == 4400 || Number == 4600 || Number == 4800 || Number < 450)
				{
					isOld = true;
				}

				if (GeforceDigitRegex.IsMatch(gpuLower) || gpuLower.IndexOf("geforce fx") > -1)
				{
					isOld = true;
				}
			}

			if (isAmd)
			{
				if (gpuLower.IndexOf("radeon x") > -1)
				{
					isOld = true;
				}

				if (gpuLower.IndexOf("radeon hd") > -1 && Number < 7000)
				{
					isOld = true;
				}

				if (gpuLower.IndexOf("radeon r") > -1 && Number < 300)
				{
					isOld = true;
				}

				if (RadeonDigitRegex.IsMatch(gpuLower))
				{
					isOld = true;
				}
			}

			if (!isOld)
			{
				if (isIntegrate)
				{
					if (gpuLower.IndexOf("uhd") > -1)
					{
						type = "UHD";
					}
					else if (gpuLower.IndexOf(" hd") > -1)
					{
						type = "HD";
					}
				}

				if (isNvidia)
				{
					if (gpuLower.IndexOf("rtx") > -1)
					{
						type = "RTX";
					}
					else if (gpuLower.IndexOf("gtx") > -1)
					{
						type = "GTX";
					}
					else if (gpuLower.IndexOf("gts") > -1)
					{
						type = "gts";
					}
				}

				if (isAmd)
				{
					if (gpuLower.IndexOf("rx") > -1)
					{
						type = "RX";
					}
					else if (gpuLower.IndexOf("r9") > -1)
					{
						type = "R9";
					}
					else if (gpuLower.IndexOf("r7") > -1)
					{
						type = "R7";
					}
					else if (gpuLower.IndexOf("r5") > -1)
					{
						type = "R5";
					}
					else if (gpuLower.IndexOf("radeon hd") > -1)
					{
						type = "Radeon HD";
					}
				}
			}

			if (!isAmd && !isNvidia && !isIntegrate && !isDx)
			{
				isOld = true;
			}

			if (gpuName.IndexOf("(nvidia)", StringComparison.OrdinalIgnoreCase) > -1
				&& gpuName.IndexOf("(amd)", StringComparison.OrdinalIgnoreCase) > -1)
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