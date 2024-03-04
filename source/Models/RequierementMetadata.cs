using Playnite.SDK;
using Playnite.SDK.Models;

namespace SystemChecker.Models
{
    public abstract class RequierementMetadata
    {
        internal static readonly ILogger logger = LogManager.GetLogger();

        internal Game GameContext { get; set; }
        internal GameRequierements GameRequierements { get; set; } = new GameRequierements();


        public bool IsFind()
        {
            return GameRequierements.GetMinimum().HasData;
        }


        public abstract GameRequierements GetRequirements();

        public abstract GameRequierements GetRequirements(string url);
    }
}
