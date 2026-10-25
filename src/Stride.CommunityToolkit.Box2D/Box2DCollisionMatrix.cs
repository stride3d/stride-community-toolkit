namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Simple symmetric collision matrix for filtering collisions between group pairs.
/// Pairs default to colliding until configured otherwise.
/// </summary>
public sealed class Box2DCollisionMatrix
{
    private readonly Dictionary<(int, int), bool> _collisionTable = [];

    /// <summary>
    /// Sets whether two groups may collide. Order does not matter.
    /// </summary>
    /// <param name="groupA">First collision group.</param>
    /// <param name="groupB">Second collision group.</param>
    /// <param name="canCollide">Whether members of the two groups collide.</param>
    public void SetCollision(int groupA, int groupB, bool canCollide) => _collisionTable[Key(groupA, groupB)] = canCollide;

    /// <summary>
    /// Returns whether two groups may collide; true for any pair never configured.
    /// </summary>
    /// <param name="groupA">First collision group.</param>
    /// <param name="groupB">Second collision group.</param>
    public bool CanCollide(int groupA, int groupB) => !_collisionTable.TryGetValue(Key(groupA, groupB), out var canCollide) || canCollide;

    // The smaller group first, so (a, b) and (b, a) are one entry
    private static (int, int) Key(int groupA, int groupB) => (Math.Min(groupA, groupB), Math.Max(groupA, groupB));
}