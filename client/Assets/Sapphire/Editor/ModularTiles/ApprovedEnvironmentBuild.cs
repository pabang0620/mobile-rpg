using UnityEngine;

namespace Sapphire.EditorTools.ModularTiles
{
    /// <summary>Rebuilds approved environment assets and their Slime Kingdom scene.</summary>
    public static class ApprovedEnvironmentBuild
    {
        public static void Build()
        {
            ModularGroundBuilder.Build();
            ModularElevationBuilder.Build();
            ModularStairsBuilder.Build();
            ModularWaterBuilder.Build();
            ModularDecorationBuilder.Build();
            ModularWorldConnectionBuilder.Build();
            SapphireSceneBuilder.RebuildSlimeKingdom();
            Debug.Log("APPROVED_ENVIRONMENT_BUILD SUCCESS");
        }
    }
}
