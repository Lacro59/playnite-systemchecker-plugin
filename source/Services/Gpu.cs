using Playnite.SDK.Data;
using CommonPluginsShared;
using SystemChecker.Models;
using CommonPluginsShared.SystemInfo;
using SystemChecker.Services.Parser;

namespace SystemChecker.Services
{
	public class Gpu : HardwareChecker
	{
		protected override string CheckType => "GPU";
		protected override string ComponentPcName => _cardPcName;
		protected override string ComponentRequirementName => _cardRequirementName;

		private string _cardPcName;
		private GpuObject CardPc { get; set; }
		private string _cardRequirementName;
		private GpuObject CardRequirement { get; set; }

		public bool IsWithNoCard = false;

		public Gpu(SystemConfiguration systemConfiguration, string gpuRequirement)
		{
			_cardRequirementName = GpuRequirementParser.NormalizeToken(gpuRequirement);
			_cardPcName = GpuRequirementParser.Parse(systemConfiguration.GpuName).Name;

			CardPc = GpuRequirementParser.Parse(systemConfiguration.GpuName);
			CardRequirement = GpuRequirementParser.Parse(gpuRequirement);

			CardPc.Vram = systemConfiguration.GpuRam;
			CardPc.ResolutionHorizontal = (int)systemConfiguration.CurrentHorizontalResolution;
		}

		protected override CheckResult PerformCheck()
		{
			Common.LogDebug(true, $"Gpu.PerformCheck - CardPc({_cardPcName}): {Serialization.ToJson(CardPc)}");
			Common.LogDebug(true, $"Gpu.PerformCheck - CardRequirement({_cardRequirementName}): {Serialization.ToJson(CardRequirement)}");

			if (GpuRequirementParser.IsDiscardable(_cardRequirementName))
			{
				IsWithNoCard = true;
				return CheckResult.Pass();
			}

			if (GpuRequirementParser.IsDiscardableGeneric(_cardRequirementName))
			{
				if (CardPc.IsNvidia || CardPc.IsAmd)
				{
					IsWithNoCard = true;
					return CheckResult.Pass();
				}

				return CheckResult.Unknown();
			}

			if (GpuRequirementParser.IsIntegratedRequirement(_cardRequirementName) && (CardPc.IsNvidia || CardPc.IsAmd))
			{
				IsWithNoCard = true;
				return CheckResult.Pass();
			}

			if (GpuRequirementParser.IsLegacyRequirement(_cardRequirementName) && (CardPc.IsNvidia || CardPc.IsAmd))
			{
				IsWithNoCard = true;
				return CheckResult.Pass();
			}

			if (CardRequirement.IsIntegrate && (CardPc.IsNvidia || CardPc.IsAmd))
			{
				return CheckResult.Pass();
			}

			if (CardRequirement.IsDx)
			{
				IsWithNoCard = true;
				return CheckResult.Pass();
			}

			if (CardRequirement.IsOGL && CardRequirement.OglVersion < 4)
			{
				IsWithNoCard = true;
				return CheckResult.Pass();
			}

			if (!CardRequirement.IsNamedModel)
			{
				return EvaluateGenericRequirement();
			}

			bool? isBetter = CallBenchmark(_cardPcName, _cardRequirementName, true);
			if (isBetter != null)
			{
				return isBetter.Value ? CheckResult.Pass() : CheckResult.Fail();
			}

			Logger.Warn($"No GPU benchmark match for {_cardPcName}: {Serialization.ToJson(CardPc)} & {_cardRequirementName}: {Serialization.ToJson(CardRequirement)}");
			return CheckResult.Unknown();
		}

		private CheckResult EvaluateGenericRequirement()
		{
			if (CardRequirement.Vram != 0 && CardRequirement.Vram <= CardPc.Vram)
			{
				IsWithNoCard = true;
				return CheckResult.Pass();
			}

			if (CardRequirement.ResolutionHorizontal != 0 && CardRequirement.ResolutionHorizontal <= CardPc.ResolutionHorizontal)
			{
				IsWithNoCard = true;
				return CheckResult.Pass();
			}

			if (CardRequirement.Vram != 0 || CardRequirement.ResolutionHorizontal != 0)
			{
				IsWithNoCard = true;
				return CheckResult.Fail();
			}

			return CheckResult.Unknown();
		}
	}

	public class GpuObject
	{
		public string Name { get; set; }
		public bool IsIntegrate { get; set; }
		public bool IsNvidia { get; set; }
		public bool IsAmd { get; set; }
		public bool IsNamedModel { get; set; }
		public bool IsOGL { get; set; }
		public bool IsDx { get; set; }
		public int DxVersion { get; set; }
		public int OglVersion { get; set; }
		public long Vram { get; set; }
		public int ResolutionHorizontal { get; set; }
	}
}
