namespace Example18_Box2DPhysics.Box2DPhysics.Core;

/// <summary>
/// Simple collision matrix for filtering collisions between different groups
/// </summary>
public class CollisionMatrix
{
    private readonly Dictionary<(int groupA, int groupB), bool> _collisionTable = new();

    public void SetCollision(int groupA, int groupB, bool canCollide)
    {
        _collisionTable[(Math.Min(groupA, groupB), Math.Max(groupA, groupB))] = canCollide;
    }

    public bool CanCollide(int groupA, int groupB)
    {
        var key = (Math.Min(groupA, groupB), Math.Max(groupA, groupB));

        return _collisionTable.TryGetValue(key, out var canCollide) ? canCollide : true; // Default to true
    }
}