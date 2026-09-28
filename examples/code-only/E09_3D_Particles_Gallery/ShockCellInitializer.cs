using Stride.Core.Mathematics;
using Stride.Particles;
using Stride.Particles.Initializers;

namespace E09_3D_Particles_Gallery;

/// <summary>
/// An initializer of our own for shock diamonds: every new particle is born in one of a few cells
/// strung along an axis, with no velocity, so the cells stand still while the exhaust rushes through
/// them. Each cell is smaller and dimmer than the one before. The spacing can change while the
/// engine runs: a throttled engine pulls its diamonds closer to the nozzle.
/// </summary>
/// <remarks>
/// Use it with a <see cref="Stride.Particles.ShapeBuilders.ShapeBuilderOrientedQuad"/>, a short
/// lifetime and no size or colour updater: the initializer writes the direction the quad lies
/// along, the size and the colour, and an updater would overwrite them.
/// </remarks>
public sealed class ShockCellInitializer : ParticleInitializer
{
    /// <summary>The axis the cells are strung along, in the emitter's space.</summary>
    public Vector3 Axis { get; set; } = Vector3.UnitX;

    /// <summary>How many cells there are.</summary>
    public int Cells { get; set; } = 4;

    /// <summary>The distance from the emitter to the first cell.</summary>
    public float First { get; set; } = 0.5f;

    /// <summary>The distance from one cell to the next.</summary>
    public float Spacing { get; set; } = 1f;

    /// <summary>The size of a particle in the first cell.</summary>
    public float Size { get; set; } = 0.5f;

    /// <summary>The size of a cell as a fraction of the one before it.</summary>
    public float Shrink { get; set; } = 0.88f;

    /// <summary>The brightness of a cell as a fraction of the one before it.</summary>
    public float Dimming { get; set; } = 0.75f;

    /// <summary>The colour of a particle in the first cell, each channel 1 at most: a particle's colour is stored in bytes. Intensity belongs in the material.</summary>
    public Color4 Color { get; set; } = Color4.White;

    /// <summary>Declares the fields the initializer writes.</summary>
    public ShockCellInitializer()
    {
        RequiredFields.Add(ParticleFields.Position);
        RequiredFields.Add(ParticleFields.Direction);
        RequiredFields.Add(ParticleFields.Size);
        RequiredFields.Add(ParticleFields.Color);
        RequiredFields.Add(ParticleFields.RandomSeed);
    }

    /// <inheritdoc/>
    public override void Initialize(ParticlePool pool, int startIdx, int endIdx, int maxCapacity)
    {
        if (!pool.FieldExists(ParticleFields.Position) || !pool.FieldExists(ParticleFields.Direction) || !pool.FieldExists(ParticleFields.Size)
            || !pool.FieldExists(ParticleFields.Color) || !pool.FieldExists(ParticleFields.RandomSeed)) return;

        var positions = pool.GetField(ParticleFields.Position);
        var directions = pool.GetField(ParticleFields.Direction);
        var sizes = pool.GetField(ParticleFields.Size);
        var colours = pool.GetField(ParticleFields.Color);
        var seeds = pool.GetField(ParticleFields.RandomSeed);

        var axis = Vector3.Normalize(Axis);

        WorldRotation.Rotate(ref axis);

        for (var i = startIdx; i != endIdx; i = (i + 1) % maxCapacity)
        {
            var particle = pool.FromIndex(i);
            var seed = particle.Get(seeds);

            // Which cell, by seed: every cell gets its share of the particles
            var cell = Math.Min((int)(seed.GetFloat(RandomOffset.Offset1A) * Cells), Cells - 1);
            var flicker = 0.8f + 0.4f * seed.GetFloat(RandomOffset.Offset2A);

            var distance = (First + cell * Spacing) * WorldScale.X;
            var brightness = MathF.Pow(Dimming, cell) * flicker;

            particle.Set(positions, WorldPosition + axis * distance);
            particle.Set(directions, axis);
            particle.Set(sizes, Size * MathF.Pow(Shrink, cell) * flicker * WorldScale.X);
            particle.Set(colours, new Color4(Color.R * brightness, Color.G * brightness, Color.B * brightness, Color.A));
        }
    }
}