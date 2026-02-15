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
	public class Cpu : HardwareChecker
	{
		private static readonly Regex IntelTypeRegex = new Regex(@"i[0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex IntelVersionRegex = new Regex(@"i[0-9]-[0-9]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex FourDigitsRegex = new Regex(@"\d{4}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex ThreeDigitsRegex = new Regex(@"\d{3}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex RyzenTypeRegex = new Regex(@"Ryzen[ ][0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex RyzenGRegex = new Regex(@"[0-9]+G", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex RyzenXTRegex = new Regex(@"[0-9]+XT", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex RyzenXRegex = new Regex(@"[0-9]+X", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex RyzenURegex = new Regex(@"[0-9]+U", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex ClockGhzRegex = new Regex(@"[0-9]*[.][0-9]*[ GHz]*", RegexOptions.Compiled);
		private static readonly Regex ClockGhzRegex2 = new Regex(@"[0-9]*[.][0-9]*[GHz]*", RegexOptions.Compiled);
		private static readonly Regex ClockMhzRegex = new Regex(@"[0-9]*[.][0-9]*[ MHz]*", RegexOptions.Compiled);
		private static readonly Regex ClockMhzRegex2 = new Regex(@"[0-9]*[.][0-9]*[MHz]*", RegexOptions.Compiled);
		private static readonly Regex SingleDigitRegex = new Regex(@"\d", RegexOptions.Compiled);

		protected override string CheckType => "CPU";
		protected override string ComponentPcName => ProcessorPc.Name;
		protected override string ComponentRequirementName => ProcessorRequirement.Name;

		private CpuObject ProcessorPc { get; set; }
		private CpuObject ProcessorRequirement { get; set; }

		public Cpu(SystemConfiguration systemConfiguration, string cpuRequirement)
		{
			ProcessorPc = SetProcessor(systemConfiguration.Cpu);
			ProcessorPc = SetProcessor(systemConfiguration.Cpu);
			ProcessorRequirement = SetProcessor(cpuRequirement);

			PerformCheck();
		}

		protected override CheckResult PerformCheck()
		{
			Common.LogDebug(true, $"Cpu.PerformCheck - ProcessorPc: {Serialization.ToJson(ProcessorPc)}");
			Common.LogDebug(true, $"Cpu.PerformCheck - ProcessorRequirement: {Serialization.ToJson(ProcessorRequirement)}");

			if (!ProcessorPc.IsOld && ProcessorRequirement.IsOld)
			{
				return new CheckResult { Result = true };
			}

			if (ProcessorPc.IsOld && !ProcessorRequirement.IsOld)
			{
				return new CheckResult();
			}

			if (ProcessorPc.IsOld && ProcessorRequirement.IsOld)
			{
				Logger.Warn($"No CPU treatment for {Serialization.ToJson(ProcessorPc)} & {Serialization.ToJson(ProcessorRequirement)}");
				return new CheckResult();
			}

			if (!ProcessorRequirement.IsIntel && !ProcessorRequirement.IsAmd)
			{
				if (ProcessorRequirement.Clock == 0 || ProcessorPc.Clock == 0)
				{
					return new CheckResult { Result = true };
				}

				return new CheckResult { Result = true };
			}

			bool? isBetter = CallBenchmark(ProcessorPc.Name, ProcessorRequirement.Name, false);
			if (isBetter != null)
			{
				return new CheckResult
				{
					Result = (bool)isBetter,
					SameConstructor = true
				};
			}

			if (ProcessorPc.IsIntel && ProcessorRequirement.IsIntel)
			{
				if (ProcessorPc.Type == ProcessorRequirement.Type)
				{
					return new CheckResult { SameConstructor = true, Result = ProcessorPc.Version >= ProcessorRequirement.Version };
				}

				Match pcMatch = SingleDigitRegex.Match(ProcessorPc.Type);
				Match reqMatch = SingleDigitRegex.Match(ProcessorRequirement.Type);

				if (pcMatch.Success && reqMatch.Success)
				{
					int pcDigit = int.Parse(pcMatch.Value);
					int reqDigit = int.Parse(reqMatch.Value);

					if (pcDigit > reqDigit)
					{
						return new CheckResult { Result = true };
					}
				}

				if (ProcessorPc.Type == "i3" && ProcessorRequirement.Type == "i5"
					|| ProcessorPc.Type == "i5" && ProcessorRequirement.Type == "i7")
				{
					return new CheckResult { SameConstructor = true, Result = (ProcessorPc.Version + 1000) >= ProcessorRequirement.Version };
				}

				if (ProcessorPc.Type == "i3" && ProcessorRequirement.Type == "i7")
				{
					return new CheckResult { SameConstructor = true, Result = (ProcessorPc.Version + 500) >= ProcessorRequirement.Version };
				}
			}

			if (ProcessorPc.IsAmd && ProcessorRequirement.IsAmd)
			{
				Match pcRyzenMatch = RyzenTypeRegex.Match(ProcessorPc.Type);
				Match reqRyzenMatch = RyzenTypeRegex.Match(ProcessorRequirement.Type);

				if (pcRyzenMatch.Success && reqRyzenMatch.Success && pcRyzenMatch.Value == reqRyzenMatch.Value)
				{
					return new CheckResult { SameConstructor = true, Result = ProcessorPc.Version >= ProcessorRequirement.Version };
				}

				bool pcHasRyzen = ProcessorPc.Type.IndexOf("ryzen", StringComparison.OrdinalIgnoreCase) > -1;
				bool reqHasRyzen = ProcessorRequirement.Type.IndexOf("ryzen", StringComparison.OrdinalIgnoreCase) > -1;
				bool reqHasAthlon = ProcessorRequirement.Type.IndexOf("athlon", StringComparison.OrdinalIgnoreCase) > -1;

				if (pcHasRyzen && reqHasAthlon)
				{
					return new CheckResult { SameConstructor = true, Result = true };
				}

				if (pcHasRyzen && reqHasRyzen)
				{
					Match pcDigitMatch = SingleDigitRegex.Match(ProcessorPc.Type);
					Match reqDigitMatch = SingleDigitRegex.Match(ProcessorRequirement.Type);

					if (pcDigitMatch.Success && reqDigitMatch.Success)
					{
						int pcDigit = int.Parse(pcDigitMatch.Value);
						int reqDigit = int.Parse(reqDigitMatch.Value);

						if (pcDigit > reqDigit)
						{
							return new CheckResult { SameConstructor = true, Result = true };
						}
						else
						{
							return new CheckResult { SameConstructor = true, Result = (ProcessorPc.Version + 1500) >= ProcessorRequirement.Version };
						}
					}
				}
			}

			if (ProcessorPc.IsAmd && ProcessorRequirement.IsIntel)
			{
				return CompareAmdVsIntel(ProcessorPc, ProcessorRequirement);
			}

			if (ProcessorPc.IsIntel && ProcessorRequirement.IsAmd)
			{
				return CompareIntelVsAmd(ProcessorPc, ProcessorRequirement);
			}

			Logger.Warn($"No CPU treatment for {Serialization.ToJson(ProcessorPc)} & {Serialization.ToJson(ProcessorRequirement)}");
			return new CheckResult();
		}

		private CheckResult CompareAmdVsIntel(CpuObject amd, CpuObject intel)
		{
			bool amdHasRyzen3 = amd.Type.IndexOf("ryzen 3", StringComparison.OrdinalIgnoreCase) > -1;
			bool amdHasRyzen5 = amd.Type.IndexOf("ryzen 5", StringComparison.OrdinalIgnoreCase) > -1;
			bool amdHasRyzen7 = amd.Type.IndexOf("ryzen 7", StringComparison.OrdinalIgnoreCase) > -1;

			bool intelHasI3 = intel.Type.IndexOf("i3", StringComparison.OrdinalIgnoreCase) > -1;
			bool intelHasI5 = intel.Type.IndexOf("i5", StringComparison.OrdinalIgnoreCase) > -1;
			bool intelHasI7 = intel.Type.IndexOf("i7", StringComparison.OrdinalIgnoreCase) > -1;

			if (amdHasRyzen3)
			{
				if (intelHasI3) return new CheckResult { Result = (amd.Version + 4000) >= intel.Version };
				if (intelHasI5) return new CheckResult { Result = (amd.Version + 3000) >= intel.Version };
				if (intelHasI7) return new CheckResult { Result = (amd.Version + 2000) >= intel.Version };
			}
			else if (amdHasRyzen5)
			{
				if (intelHasI3) return new CheckResult { Result = (amd.Version + 5000) >= intel.Version };
				if (intelHasI5) return new CheckResult { Result = (amd.Version + 4000) >= intel.Version };
				if (intelHasI7) return new CheckResult { Result = (amd.Version + 3000) >= intel.Version };
			}
			else if (amdHasRyzen7)
			{
				if (intelHasI3) return new CheckResult { Result = (amd.Version + 6000) >= intel.Version };
				if (intelHasI5) return new CheckResult { Result = (amd.Version + 5000) >= intel.Version };
				if (intelHasI7) return new CheckResult { Result = (amd.Version + 4000) >= intel.Version };
			}

			return new CheckResult();
		}

		private CheckResult CompareIntelVsAmd(CpuObject intel, CpuObject amd)
		{
			bool amdHasRyzen3 = amd.Type.IndexOf("ryzen 3", StringComparison.OrdinalIgnoreCase) > -1;
			bool amdHasRyzen5 = amd.Type.IndexOf("ryzen 5", StringComparison.OrdinalIgnoreCase) > -1;
			bool amdHasRyzen7 = amd.Type.IndexOf("ryzen 7", StringComparison.OrdinalIgnoreCase) > -1;

			bool intelHasI3 = intel.Type.IndexOf("i3", StringComparison.OrdinalIgnoreCase) > -1;
			bool intelHasI5 = intel.Type.IndexOf("i5", StringComparison.OrdinalIgnoreCase) > -1;
			bool intelHasI7 = intel.Type.IndexOf("i7", StringComparison.OrdinalIgnoreCase) > -1;

			if (amdHasRyzen3)
			{
				if (intelHasI3) return new CheckResult { Result = (amd.Version + 4000) >= intel.Version };
				if (intelHasI5) return new CheckResult { Result = (amd.Version + 3000) >= intel.Version };
				if (intelHasI7) return new CheckResult { Result = (amd.Version + 2000) >= intel.Version };
			}
			else if (amdHasRyzen5)
			{
				if (intelHasI3) return new CheckResult { Result = (amd.Version + 5000) >= intel.Version };
				if (intelHasI5) return new CheckResult { Result = (amd.Version + 4000) >= intel.Version };
				if (intelHasI7) return new CheckResult { Result = (amd.Version + 3000) >= intel.Version };
			}
			else if (amdHasRyzen7)
			{
				if (intelHasI3) return new CheckResult { Result = (amd.Version + 6000) >= intel.Version };
				if (intelHasI5) return new CheckResult { Result = (amd.Version + 5000) >= intel.Version };
				if (intelHasI7) return new CheckResult { Result = (amd.Version + 4000) >= intel.Version };
			}

			return new CheckResult();
		}

		public static bool CallIsIntel(string cpuName)
		{
			return cpuName.IndexOf("intel", StringComparison.OrdinalIgnoreCase) > -1
				|| IntelTypeRegex.IsMatch(cpuName);
		}

		public static bool CallIsAmd(string cpuName)
		{
			return cpuName.IndexOf("amd", StringComparison.OrdinalIgnoreCase) > -1
				|| cpuName.IndexOf("ryzen", StringComparison.OrdinalIgnoreCase) > -1;
		}

		private CpuObject SetProcessor(string cpuName)
		{
			bool isIntel = CallIsIntel(cpuName);
			bool isAmd = CallIsAmd(cpuName);
			bool isOld = false;
			string type = string.Empty;
			int version = 0;
			double clock = 0;

			string cpuLower = cpuName.ToLower();

			if (isIntel)
			{
				Match typeMatch = IntelTypeRegex.Match(cpuName);
				if (typeMatch.Success)
				{
					type = typeMatch.Value.Trim();

					Match versionMatch = IntelVersionRegex.Match(cpuName);
					if (versionMatch.Success)
					{
						int.TryParse(versionMatch.Value.Replace(type + "-", string.Empty).Trim(), out version);
					}

					if (version == 0)
					{
						Match fourDigitsMatch = FourDigitsRegex.Match(cpuName);
						if (fourDigitsMatch.Success)
						{
							int.TryParse(fourDigitsMatch.Value.Trim(), out version);
						}
					}
				}

				isOld = !IntelTypeRegex.IsMatch(cpuName);
			}

			if (isAmd)
			{
				if (cpuLower.IndexOf("ryzen") > -1)
				{
					Match ryzenMatch = RyzenTypeRegex.Match(cpuName);
					if (ryzenMatch.Success)
					{
						type = ryzenMatch.Value.Trim();

						if (RyzenGRegex.IsMatch(cpuName)) type += " G";
						else if (RyzenXTRegex.IsMatch(cpuName)) type += " XT";
						else if (RyzenXRegex.IsMatch(cpuName)) type += " X";
						else if (RyzenURegex.IsMatch(cpuName)) type += " U";
					}
				}
				else if (cpuLower.IndexOf("athlon") > -1)
				{
					type = "Athlon";

					if (cpuLower.IndexOf("ge") > -1) type += " GE";
					else if (cpuLower.IndexOf("g") > -1) type += " G";
				}

				Match fourDigitsMatch = FourDigitsRegex.Match(cpuName);
				if (fourDigitsMatch.Success)
				{
					int.TryParse(fourDigitsMatch.Value.Trim(), out version);
				}
				else
				{
					Match threeDigitsMatch = ThreeDigitsRegex.Match(cpuName);
					if (threeDigitsMatch.Success)
					{
						int.TryParse(threeDigitsMatch.Value.Trim(), out version);
					}
				}

				isOld = cpuLower.IndexOf("ryzen") == -1;
			}

			Match clockMatch = ClockGhzRegex.Match(cpuName);
			if (clockMatch.Success)
			{
				string clockStr = clockMatch.Value.Replace("GHz", string.Empty)
					.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
					.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
					.Trim();
				double.TryParse(clockStr, out clock);
			}

			if (clock == 0)
			{
				clockMatch = ClockGhzRegex2.Match(cpuName);
				if (clockMatch.Success)
				{
					string clockStr = clockMatch.Value.Replace("GHz", string.Empty)
						.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
						.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
						.Trim();
					double.TryParse(clockStr, out clock);
				}
			}

			if (clock == 0)
			{
				clockMatch = ClockMhzRegex.Match(cpuName);
				if (clockMatch.Success)
				{
					string clockStr = clockMatch.Value.Replace("MHz", string.Empty)
						.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
						.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
						.Trim();
					if (double.TryParse(clockStr, out clock) && clock != 0)
					{
						clock /= 1000;
					}
				}
			}

			if (clock == 0)
			{
				clockMatch = ClockMhzRegex2.Match(cpuName);
				if (clockMatch.Success)
				{
					string clockStr = clockMatch.Value.Replace("MHz", string.Empty)
						.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
						.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
						.Trim();
					if (double.TryParse(clockStr, out clock) && clock != 0)
					{
						clock /= 1000;
					}
				}
			}

			if (cpuLower.IndexOf("single core") > -1
				|| cpuLower.IndexOf("dual core") > -1
				|| cpuLower.IndexOf("quad core") > -1)
			{
				isOld = true;
			}

			if (version == 0)
			{
				isOld = true;
			}

			return new CpuObject
			{
				Name = cpuName,
				IsIntel = isIntel,
				IsAmd = isAmd,
				IsOld = isOld,
				Type = type,
				Version = version,
				Clock = clock
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
}