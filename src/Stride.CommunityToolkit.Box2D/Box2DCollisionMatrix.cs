namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// Which collision groups collide with which: a symmetric table over positive group numbers, where every pair
/// collides until <see cref="SetCollision"/> says otherwise. <see cref="Box2DSimulation.CollisionMatrix"/> is the
/// one the simulation consults, for the shapes that joined a group through
/// <see cref="ShapeFixtureBuilder.InCollisionGroup"/>.
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