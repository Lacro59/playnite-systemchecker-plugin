using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SystemChecker.Models
{
    public class Requirement : ObservableObject
    {
        public bool IsMinimum { get; set; }
        public List<string> Os { get; set; } = new List<string>();
        public List<string> Cpu { get; set; } = new List<string>();
        public List<string> Gpu { get; set; } = new List<string>();
        public double Ram { get; set; }
        public string RamUsage { get; set; }
        public double Storage { get; set; }
        public string StorageUsage { get; set; }

        [DontSerialize]
        public bool HasData => Os.Count > 1 || Cpu.Count > 1 || Gpu.Count > 1 || Ram > 0 || Storage > 0;
    }
}
