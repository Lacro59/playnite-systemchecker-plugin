using CommonPluginsShared;
using CommonPluginsShared.SystemInfo;
using Playnite.SDK.Data;
using SystemChecker.Models;
using SystemChecker.Services.Parser;

namespace SystemChecker.Services
{
	public class Cpu : HardwareChecker
	{
		protected override string CheckType => "CPU";
		protected override string ComponentPcName => ProcessorPc.Name;
		protected override string ComponentRequirementName => ProcessorRequirement.Name;

		private CpuObject ProcessorPc { get; set; }
		private CpuObject ProcessorRequirement { get; set; }

		public Cpu(SystemConfiguration systemConfiguration, string cpuRequirement)
		{
			ProcessorPc = CpuRequirementParser.Parse(systemConfiguration.Cpu);
			ProcessorRequirement = CpuRequirementParser.Parse(cpuRequirement);

			PerformCheck();
		}

		protected override CheckResult PerformCheck()
		{
			Common.LogDebug(true, $"Cpu.PerformCheck - ProcessorPc: {Serialization.ToJson(ProcessorPc)}");
			Common.LogDebug(true, $"Cpu.PerformCheck - ProcessorRequirement: {Serialization.ToJson(ProcessorRequirement)}");

			if (!ProcessorRequirement.IsNamedModel)
			{
				return EvaluateGenericRequirement();
			}

			bool? isBetter = CallBenchmark(ProcessorPc.Name, ProcessorRequirement.Name, false);
			if (isBetter != null)
			{
				return isBetter.Value ? CheckResult.Pass() : CheckResult.Fail();
			}

			Logger.Warn($"No CPU benchmark match for {Serialization.ToJson(ProcessorPc)} & {Serialization.ToJson(ProcessorRequirement)}");
			return CheckResult.Unknown();
		}

		private CheckResult EvaluateGenericRequirement()
		{
			if (ProcessorPc.IsNamedModel)
			{
				return CheckResult.Pass();
			}

			if (ProcessorRequirement.Clock == 0 || ProcessorPc.Clock == 0)
			{
				return CheckResult.Pass();
			}

			return ProcessorPc.Clock >= ProcessorRequirement.Clock
				? CheckResult.Pass()
				: CheckResult.Fail();
		}
	}

	public class CpuObject
	{
		public string Name { get; set; }
		public bool IsIntel { get; set; }
		public bool IsAmd { get; set; }
		public bool IsNamedModel { get; set; }
		public double Clock { get; set; }
	}
}
