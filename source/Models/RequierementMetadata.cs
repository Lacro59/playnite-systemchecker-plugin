using Playnite.SDK;
using Playnite.SDK.Models;

namespace SystemChecker.Models
{
    public abstract class RequierementMetadata
    {
        internal static readonly ILogger logger = LogManager.GetLogger();

        internal Game GameContext { get; set; }
        internal PluginGameRequierements PluginGameRequierements { get; set; } = new PluginGameRequierements();


        public bool IsFind()
        {
            return PluginGameRequierements.GetMinimum().HasData;
        }

        public abstract PluginGameRequierements GetRequirements();

        public abstract PluginGameRequierements GetRequirements(string url);
    }
}