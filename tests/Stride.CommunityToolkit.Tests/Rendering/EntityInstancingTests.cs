using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.Core.Mathematics;
using Stride.Engine;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

public class EntityInstancingTests
{
    private const float Tolerance = 1e-4f;

    /// <summary>
    /// Builds an entity whose world matrix is a rigid transform derived from <paramref name="seed"/>,
    /// so tests have varied rotations and translations without depending on a running game.
    /// </summary>
    private static Entity CreateRigidEntity(int seed)
    {
        var random = new Random(seed);

        var rotation = Quaternion.RotationYawPitchRoll(
            random.NextSingle() * MathF.Tau,
            random.NextSingle() * MathF.Tau,
            random.NextSingle() * MathF.Tau);

        var translation = new Vector3(
            random.NextSingle() * 100f - 50f,
            random.NextSingle() * 100f - 50f,
            random.NextSingle() * 100f - 50f);

        var entity = new Entity($"Instance{seed}");

        entity.Transform.WorldMatrix = Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);

        return entity;
    }

    private static void AssertMatricesEqual(Matrix expected, Matrix actual)
    {
        for (var i = 0; i < 16; i++)
        {
            Assert.InRange(MathF.Abs(expected[i] - actual[i]), 0f, Tolerance);
        }
    }

    [Fact]
    public void Update_RigidInverse_MatchesGeneralInverse()
    {
        var instancing = new EntityInstancing { AssumeRigidTransforms = true };
        var entities = new List<Entity>();

        for (var i = 0; i < 200; i++)
        {
            var entity = CreateRigidEntity(i);

            entities.Add(entity);
            instancing.AddInstance(entity);
        }

        instancing.Update();

        for (var i = 0; i < entities.Count; i++)
        {
            var world = instancing.WorldMatrices[i];

            Matrix.Invert(ref world, out var expected);

            AssertMatricesEqual(expected, instancing.WorldInverseMatrices[i]);
        }
    }

    [Fact]
    public void Update_GeneralInverse_HandlesScaledTransforms()
    {
        // The rigid fast path is only valid without scale, so scaled instances must use the general one
        var instancing = new EntityInstancing { AssumeRigidTransforms = false };
        var entity = new Entity();

        entity.Transform.WorldMatrix =
            Matrix.Scaling(2f, 3f, 4f)
            * Matrix.RotationY(0.5f)
            * Matrix.Translation(5f, 6f, 7f);

        instancing.AddInstance(entity);
        instancing.Update();

        var world = instancing.WorldMatrices[0];

        Matrix.Invert(ref world, out var expected);

        AssertMatricesEqual(expected, instancing.WorldInverseMatrices[0]);
    }

    [Fact]
    public void Update_SequentialAndParallel_ProduceIdenticalResults()
    {
        const int count = 3000;

        var sequential = new EntityInstancing { ParallelThreshold = int.MaxValue };
        var parallel = new EntityInstancing { ParallelThreshold = 1 };

        for (var i = 0; i < count; i++)
        {
            // Separate entities per instancing, same transforms, so registration order matches
            sequential.AddInstance(CreateRigidEntity(i));
            parallel.AddInstance(CreateRigidEntity(i));
        }

        sequential.Update();
        parallel.Update();

        Assert.Equal(sequential.InstanceCount, parallel.InstanceCount);

        for (var i = 0; i < count; i++)
        {
            AssertMatricesEqual(sequential.WorldMatrices[i], parallel.WorldMatrices[i]);
            AssertMatricesEqual(sequential.WorldInverseMatrices[i], parallel.WorldInverseMatrices[i]);
        }

        Assert.Equal(sequential.BoundingBox.Minimum, parallel.BoundingBox.Minimum);
        Assert.Equal(sequential.BoundingBox.Maximum, parallel.BoundingBox.Maximum);
    }

    [Theory]
    [InlineData(int.MaxValue)] // sequential
    [InlineData(1)]            // parallel
    public void Update_BoundingBox_ContainsEveryInstancePosition(int parallelThreshold)
    {
        var instancing = new EntityInstancing { ParallelThreshold = parallelThreshold };
        var entities = new List<Entity>();

        for (var i = 0; i < 500; i++)
        {
            var entity = CreateRigidEntity(i);

            entities.Add(entity);
            instancing.AddInstance(entity);
        }

        instancing.Update();

        var expectedMin = new Vector3(float.MaxValue);
        var expectedMax = new Vector3(float.MinValue);

        foreach (var entity in entities)
        {
            var position = entity.Transform.WorldMatrix.TranslationVector;

            expectedMin = Vector3.Min(expectedMin, position);
            expectedMax = Vector3.Max(expectedMax, position);
        }

        Assert.Equal(expectedMin, instancing.BoundingBox.Minimum);
        Assert.Equal(expectedMax, instancing.BoundingBox.Maximum);
    }

    [Fact]
    public void AddInstance_SameEntityTwice_IsIgnored()
    {
        var instancing = new EntityInstancing();
        var entity = CreateRigidEntity(1);

        Assert.True(instancing.AddInstance(entity));
        Assert.False(instancing.AddInstance(entity));
        Assert.Equal(1, instancing.RegisteredInstanceCount);
    }

    [Fact]
    public void RemoveInstance_UnknownEntity_ReturnsFalse()
    {
        var instancing = new EntityInstancing();

        instancing.AddInstance(CreateRigidEntity(1));

        Assert.False(instancing.RemoveInstance(CreateRigidEntity(2)));
        Assert.Equal(1, instancing.RegisteredInstanceCount);
    }

    [Fact]
    public void RemoveInstance_FromMiddle_KeepsRemainingTransforms()
    {
        var instancing = new EntityInstancing();
        var entities = new List<Entity>();

        for (var i = 0; i < 10; i++)
        {
            var entity = CreateRigidEntity(i);

            entities.Add(entity);
            instancing.AddInstance(entity);
        }

        // Removing from the middle swaps the last instance into the hole
        Assert.True(instancing.RemoveInstance(entities[3]));
        Assert.True(instancing.RemoveInstance(entities[0]));

        instancing.Update();

        Assert.Equal(8, instancing.InstanceCount);

        var expected = entities.Where(e => e != entities[3] && e != entities[0])
            .Select(e => e.Transform.WorldMatrix.TranslationVector)
            .ToList();

        var actual = Enumerable.Range(0, instancing.InstanceCount)
            .Select(i => instancing.WorldMatrices[i].TranslationVector)
            .ToList();

        Assert.Equal(expected.Count, actual.Count);
        Assert.All(expected, position => Assert.Contains(position, actual));
    }

    [Fact]
    public void RemoveInstance_AfterSwap_StillRemovesSwappedEntity()
    {
        // The swapped-in entity's index must be re-registered, or removing it later corrupts the list
        var instancing = new EntityInstancing();
        var entities = new List<Entity>();

        for (var i = 0; i < 5; i++)
        {
            var entity = CreateRigidEntity(i);

            entities.Add(entity);
            instancing.AddInstance(entity);
        }

        instancing.RemoveInstance(entities[1]);

        // entities[4] was swapped into index 1
        Assert.True(instancing.RemoveInstance(entities[4]));

        instancing.Update();

        Assert.Equal(3, instancing.InstanceCount);

        var remaining = Enumerable.Range(0, instancing.InstanceCount)
            .Select(i => instancing.WorldMatrices[i].TranslationVector)
            .ToList();

        Assert.DoesNotContain(entities[1].Transform.WorldMatrix.TranslationVector, remaining);
        Assert.DoesNotContain(entities[4].Transform.WorldMatrix.TranslationVector, remaining);
    }

    [Fact]
    public void Clear_RemovesEveryInstance()
    {
        var instancing = new EntityInstancing();

        for (var i = 0; i < 10; i++)
        {
            instancing.AddInstance(CreateRigidEntity(i));
        }

        instancing.Clear();
        instancing.Update();

        Assert.Equal(0, instancing.RegisteredInstanceCount);
        Assert.Equal(0, instancing.InstanceCount);
        Assert.Equal(BoundingBox.Empty, instancing.BoundingBox);
    }

    [Fact]
    public void Update_WhenSkipAllowed_ReusesPreviousMatrices()
    {
        var instancing = new SkippableInstancing();
        var entity = CreateRigidEntity(1);

        instancing.AddInstance(entity);
        instancing.Update();

        var gathered = instancing.WorldMatrices[0];

        // Move the entity, then let the instancing claim nothing moved
        entity.Transform.WorldMatrix = Matrix.Translation(999f, 999f, 999f);
        instancing.CanSkip = true;
        instancing.Update();

        Assert.True(instancing.UpdateSkippedLastFrame);
        AssertMatricesEqual(gathered, instancing.WorldMatrices[0]);
    }

    [Fact]
    public void Update_AfterRegistrationChanges_DoesNotSkip()
    {
        // A structural change must always re-gather, however confident the skip policy is
        var instancing = new SkippableInstancing { CanSkip = true };

        instancing.AddInstance(CreateRigidEntity(1));
        instancing.Update();

        Assert.False(instancing.UpdateSkippedLastFrame);
        Assert.Equal(1, instancing.InstanceCount);

        instancing.AddInstance(CreateRigidEntity(2));
        instancing.Update();

        Assert.False(instancing.UpdateSkippedLastFrame);
        Assert.Equal(2, instancing.InstanceCount);

        // Only once the set is stable may it skip
        instancing.Update();

        Assert.True(instancing.UpdateSkippedLastFrame);
    }

    [Fact]
    public void InstanceHooks_MirrorSwapRemoveOrdering()
    {
        // BepuEntityInstancing keeps a body list in lockstep with the transforms through these hooks,
        // so the ordering contract they rely on is verified here without needing a physics simulation
        var instancing = new TrackingInstancing();
        var entities = new List<Entity>();

        for (var i = 0; i < 6; i++)
        {
            var entity = CreateRigidEntity(i);

            entities.Add(entity);
            instancing.AddInstance(entity);
        }

        Assert.Equal(entities, instancing.Tracked);

        instancing.RemoveInstance(entities[2]);
        instancing.Update();

        Assert.Equal(instancing.InstanceCount, instancing.Tracked.Count);

        // Every tracked entry must still line up with the transform at the same index
        for (var i = 0; i < instancing.InstanceCount; i++)
        {
            AssertMatricesEqual(instancing.Tracked[i].Transform.WorldMatrix, instancing.WorldMatrices[i]);
        }

        instancing.Clear();

        Assert.Empty(instancing.Tracked);
    }

    private sealed class SkippableInstancing : EntityInstancing
    {
        public bool CanSkip { get; set; }

        protected override bool CanSkipUpdate() => CanSkip;
    }

    /// <summary>Mirrors the hook protocol the way a derived class with parallel data must.</summary>
    private sealed class TrackingInstancing : EntityInstancing
    {
        public List<Entity> Tracked { get; } = [];

        protected override void OnInstanceAdded(Entity entity) => Tracked.Add(entity);

        protected override void OnInstanceRemoved(int index, int lastIndex)
        {
            if (index != lastIndex)
            {
                Tracked[index] = Tracked[lastIndex];
            }

            Tracked.RemoveAt(lastIndex);
        }

        protected override void OnInstancesCleared() => Tracked.Clear();
    }
}