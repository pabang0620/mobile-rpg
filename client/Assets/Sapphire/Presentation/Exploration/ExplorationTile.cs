namespace Sapphire
{
    /// <summary>
    /// Visual tiles understood by <see cref="PixelTileMapRenderer"/>. Collision and exits belong
    /// to the application layer; this enum intentionally only describes how a cell is drawn.
    /// </summary>
    public enum ExplorationTile
    {
        Grass,
        Path,
        Water,
        Sand,
        Flowers,
        Tree,
        Wall,
        Roof,
        Door
    }

    /// <summary>Cardinal art directions for the two-frame SD walker.</summary>
    public enum ExplorationFacing
    {
        Down,
        Left,
        Right,
        Up
    }
}
