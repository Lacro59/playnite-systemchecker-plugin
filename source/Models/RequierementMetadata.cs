using Playnite.SDK;
using Playnite.SDK.Models;
using SystemChecker.Services;

namespace SystemChecker.Models
{
    public abstract class RequierementMetadata
    {
        internal static readonly ILogger Logger = LogManager.GetLogger();
		internal static readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;

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