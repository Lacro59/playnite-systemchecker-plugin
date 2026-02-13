using Playnite.SDK;
using Playnite.SDK.Models;
using SystemChecker.Services;

namespace SystemChecker.Models
{
    public abstract class RequirementMetadata
    {
        internal static readonly ILogger Logger = LogManager.GetLogger();
		internal static readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;

		internal Game GameContext { get; set; }
        internal PluginGameRequirements PluginGameRequirements { get; set; } = new PluginGameRequirements();


        public bool IsFind()
        {
            return PluginGameRequirements.GetMinimum().HasData;
        }

        public abstract PluginGameRequirements GetRequirements();

        public abstract PluginGameRequirements GetRequirements(string url);
    }
}