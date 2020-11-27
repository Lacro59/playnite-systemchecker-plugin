using System.Collections.Generic;

namespace SystemChecker.Models
{
    public class SystemDisk : ObservableObject
    {
        public string Name { get; set; }
        public string Drive { get; set; }
        public long FreeSpace { get; set; }
        public string FreeSpaceUsage { get; set; }
    }
}
