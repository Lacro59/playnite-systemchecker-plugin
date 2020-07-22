using System.Collections.Generic;

namespace SystemChecker.Models
{
    class SystemConfiguration
    {
        public string Name { get; set; }
        public string Os { get; set; }
        public string Cpu { get; set; }
        public uint CpuMaxClockSpeed { get; set; }
        public string GpuName { get; set; }
        public long GpuRam { get; set; }
        public uint CurrentVerticalResolution { get; set; }
        public uint CurrentHorizontalResolution { get; set; }
        public long Ram { get; set; }
        public string RamUsage { get; set; }
        public List<SystemDisk> Disks { get; set; }
    }
}
