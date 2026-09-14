namespace Sapphire.Domain.Grid
{
    /// <summary>
    /// Outcome of a single move attempt requested through GridMover.
    /// </summary>
    public enum MoveResult
    {
        Started,
        BlockedByObstacle,
        AlreadyMoving
    }
}
