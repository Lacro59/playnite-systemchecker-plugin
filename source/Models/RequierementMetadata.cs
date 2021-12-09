using Playnite.SDK;
using Playnite.SDK.Models;

namespace SystemChecker.Models
{
    public abstract class RequierementMetadata
    {
        internal static readonly ILogger logger = LogManager.GetLogger();

        internal Game _game;
        internal GameRequierements gameRequierements = new GameRequierements();


        public bool IsFind()
        {
            return gameRequierements.GetMinimum().HasData;
        }


        public abstract GameRequierements GetRequirements();

        public abstract GameRequierements GetRequirements(string url);
    }
}
