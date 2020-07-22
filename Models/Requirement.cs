using System.Collections.Generic;

namespace SystemChecker.Models
{
    class Requirement
    {
        public List<string> Os { get; set; } = new List<string>();
        public List<string> Cpu { get; set; } = new List<string>();
        public List<string> Gpu { get; set; } = new List<string>();
        public long Ram { get; set; }
        public string RamUsage { get; set; }
        public long Storage { get; set; }
        public string StorageUsage { get; set; }
    }
}
