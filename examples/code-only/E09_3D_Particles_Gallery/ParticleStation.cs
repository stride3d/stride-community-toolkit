using Example.Common.Galleries;
using Stride.Animations;
using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Particles;
using Stride.Particles.Components;
using Stride.Particles.Initializers;
using Stride.Particles.Materials;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace E09_3D_Particles_Gallery;

/// <summary>The textures the stations draw with, loaded once from the shared resources folder.</summary>
/// <param name="Smoke">An 8 by 8 flipbook of a puff of smoke forming and thinning.</param>
/// <param name="Fire">An 8 by 8 flipbook of a flame.</param>
/// <param name="Flame">An 8 by 8 flipbook of a taller flame.</param>
/// <param name="Bonfire">An 8 by 8 flipbook of a bonfire.</param>
/// <param name="Dot">A small soft dot, for sparks and fireflies.</param>
/// <param name="Radial">A radial grey gradient, for glows and beams.</param>
public sealed record ParticleTextures(Texture Smoke, Texture Fire, Texture Flame, Texture Bonfire, Texture Dot, Texture Radial) : IDisposable
{
    /// <summary>Loads every texture from the resources folder next to the executable, as sRGB.</summary>
    /// <param name="device">The device to create them on.</param>
    /// <returns>The set, owned by the caller.</returns>
    public static ParticleTextures Load(GraphicsDevice device)
    {
        return new ParticleTextures(One("smoke.png"), One("fire8x8.png"), One("flame8x8.png"), One("bonfire8x8.png"), One("dot.png"), One("radial-grad-gray.png"));

        Texture One(string file)
        {
            using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Resources", file));
            using var image = Image.Load(stream);

            var pixels = image.PixelBuffer[0].GetPixels<Color>();

            // The sample's textures are bright on black with no alpha, made for additive blending. Alpha
            // from brightness makes the same picture work alpha-blended too, and leaves a real alpha alone.
            // A pixel whose alpha is its brightest channel is already premultiplied - a colour at that
            // coverage - so only a real alpha is premultiplied by the loader
            var derived = pixels.All(pixel => pixel.A == byte.MaxValue);

            if (derived)
            {
                for (var i = 0; i < pixels.Length; i++)
                {
                    pixels[i].A = Math.Max(pixels[i].R, Math.Max(pixels[i].G, pixels[i].B));
                }

                image.PixelBuffer[0].SetPixels(pixels);
            }

            // The toolkit's loader takes it from here: the decoder's BGRA put the right way round, sRGB, and
            // the mipmaps a particle a few pixels wide needs
            return TextureLoader.FromImage(device, image, new TextureLoadOptions(TextureRole.Color) { PremultiplyAlpha = !derived });
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Smoke.Dispose();
        Fire.Dispose();
        Flame.Dispose();
        Bonfire.Dispose();
        Dot.Dispose();
        Radial.Dispose();
    }
}

/// <summary>
/// A gallery station with what a particle exhibit needs on top of the frame: the textures, the
/// entity carrying the particle system, and the variation the visitor has chosen with V.
/// </summary>
/// <remarks>
/// An exhibit's setup method reads <see cref="Variation"/> through <see cref="Pick"/>, builds its
/// emitters and hands them to <see cref="Place"/>. Pressing V bumps the variation and runs the
/// setup again, so a variation is just the same method with a different branch taken.
/// </remarks>
public sealed class ParticleStation : GalleryStation
{
    /// <summary>The shared textures; set once by the gallery's configure step.</summary>
    public ParticleTextures Textures { get; set; } = null!;

    /// <summary>The entity carrying the station's particle system, or <see langword="null"/> before setup.</summary>
    public Entity? Entity { get; private set; }

    /// <summary>The station's particle system, or <see langword="null"/> before setup.</summary>
    public ParticleSystemComponent? Particles => Entity?.Get<ParticleSystemComponent>();

    /// <summary>Particles alive across the station's emitters, or zero when it has none.</summary>
    public int LivingParticles => Particles?.ParticleSystem.Emitters.Sum(e => e.LivingParticles) ?? 0;

    /// <summary>Which variation of the exhibit is up, counted from 0 and wrapped by <see cref="Pick"/>.</summary>
    public int Variation { get; set; }

    /// <summary>The names of the variations, as the last setup declared them.</summary>
    public IReadOnlyList<string> VariationNames { get; private set; } = [];

    /// <summary>
    /// Declares the exhibit's variations and returns which one is up. Called first thing in a
    /// setup method: <c>var v = s.Pick("slow", "normal", "fast");</c>.
    /// </summary>
    /// <param name="names">One name per variation, in order.</param>
    /// <returns>The index of the variation to build.</returns>
    public int Pick(params string[] names)
    {
        VariationNames = names;
        Variation = (Variation % names.Length + names.Length) % names.Length;

        return Variation;
    }

    /// <summary>
    /// Puts a particle system on the station: one entity at a station-local position, rotated so
    /// the emitters' local X is the station's right and local Z faces the visitor. Any system
    /// placed before is removed, which is what makes a variation a clean rebuild.
    /// </summary>
    /// <param name="local">Where the emitters sit, in station coordinates.</param>
    /// <param name="warmup">Seconds to simulate before the first frame, so a steady effect starts steady.</param>
    /// <param name="emitters">The emitters; each may carry its own spawners, initializers, updaters, shape and material.</param>
    /// <returns>The component, for an update method that wants to move or restart it.</returns>
    public ParticleSystemComponent Place(Vector3 local, float warmup, params ParticleEmitter[] emitters)
    {
        Remove();

        var particles = new ParticleSystemComponent();

        foreach (var emitter in emitters)
        {
            particles.ParticleSystem.Emitters.Add(emitter);
        }

        particles.ParticleSystem.Settings = new ParticleSystemSettings { WarmupTime = warmup };

        Entity = new Entity($"Station {Number} particles")
        {
            Transform = { Position = At(local), Rotation = FacingRotation() },
        };

        Entity.Add(particles);
        Entity.Scene = Scene;

        return particles;
    }

    /// <summary>Puts a particle system on the station with no warm-up. See <see cref="Place(Vector3, float, ParticleEmitter[])"/>.</summary>
    /// <param name="local">Where the emitters sit, in station coordinates.</param>
    /// <param name="emitters">The emitters.</param>
    /// <returns>The component.</returns>
    public ParticleSystemComponent Place(Vector3 local, params ParticleEmitter[] emitters) => Place(local, 0f, emitters);

    /// <summary>Takes the station's particle system out of the scene.</summary>
    public void Remove()
    {
        if (Entity is null) return;

        Entity.Scene = null;
        Entity = null;
    }

    /// <summary>Starts the station's simulation over from nothing, as if it had just been placed.</summary>
    public void Restart() => Particles?.ParticleSystem.ResetSimulation();
}

/// <summary>Materials in one call, since every station wants one and the defaults are not the ones anybody wants.</summary>
public static class ParticleMaterials
{
    /// <summary>A flat colour, alpha-blended or additive.</summary>
    /// <param name="color">The emissive colour; values above 1 are intensity in HDR.</param>
    /// <param name="additive">0 blends, 1 adds light, anything between mixes.</param>
    /// <returns>The material.</returns>
    public static ParticleMaterialComputeColor Flat(Color4 color, float additive = 0f)
        => new() { ComputeColor = new ComputeColor(color), AlphaAdditive = additive };

    /// <summary>A texture, optionally tinted, alpha-blended or additive.</summary>
    /// <param name="texture">The texture; its alpha is the particle's shape.</param>
    /// <param name="tint">A colour multiplied in, white by default.</param>
    /// <param name="additive">0 blends, 1 adds light, anything between mixes.</param>
    /// <param name="uv">A flipbook or scroll animation of the texture coordinates, or none.</param>
    /// <returns>The material.</returns>
    /// <param name="softEdge">The distance over which the particle fades where it meets geometry; 0 is a hard cut.</param>
    public static ParticleMaterialComputeColor Textured(Texture texture, Color4? tint = null, float additive = 0f, UVBuilder? uv = null, float softEdge = 0f)
    {
        IComputeColor color = new ComputeTextureColor(texture);

        if (tint is { } t)
        {
            color = new ComputeBinaryColor(color, new ComputeColor(t), BinaryOperator.Multiply);
        }

        return new ParticleMaterialComputeColor { ComputeColor = color, AlphaAdditive = additive, UVBuilder = uv, SoftEdgeDistance = softEdge };
    }

    /// <summary>The smoke sheet as a flipbook, the general-purpose soft particle.</summary>
    /// <param name="textures">The loaded textures.</param>
    /// <param name="tint">A colour multiplied in, white by default.</param>
    /// <param name="additive">0 blends, 1 adds light, anything between mixes.</param>
    /// <param name="softEdge">The distance over which the particle fades where it meets geometry; 0 is a hard cut.</param>
    /// <returns>The material.</returns>
    public static ParticleMaterialComputeColor Smoke(ParticleTextures textures, Color4? tint = null, float additive = 0f, float softEdge = 0f)
        => Textured(textures.Smoke, tint, additive, new UVBuilderFlipbook { XDivisions = 8, YDivisions = 8, AnimationSpeed = 64 }, softEdge);

    /// <summary>A flipbook: the texture is a grid of frames played over the particle's life.</summary>
    /// <param name="texture">The sheet.</param>
    /// <param name="columns">Frames across.</param>
    /// <param name="rows">Frames down.</param>
    /// <param name="speed">Frames shown over the particle's life; columns times rows shows each once.</param>
    /// <param name="tint">A colour multiplied in, white by default.</param>
    /// <param name="additive">0 blends, 1 adds light, anything between mixes.</param>
    /// <returns>The material.</returns>
    public static ParticleMaterialComputeColor Flipbook(Texture texture, uint columns, uint rows, uint speed, Color4? tint = null, float additive = 0f)
        => Textured(texture, tint, additive, new UVBuilderFlipbook { XDivisions = columns, YDivisions = rows, AnimationSpeed = speed });
}

/// <summary>Initializers every world-space emitter needs and nobody remembers.</summary>
public static class Initializers
{
    /// <summary>
    /// A position initializer with no range: the particle is born exactly at the emitter. The
    /// engine does that by default too; this makes it explicit on an emitter that has no other
    /// position initializer, so the reader does not have to know the default.
    /// </summary>
    /// <returns>The initializer.</returns>
    public static InitialPositionSeed AtEmitter() => new() { PositionMin = Vector3.Zero, PositionMax = Vector3.Zero };
}

/// <summary>Curves in one call: the over-time updaters sample these, and building them by hand is four nested objects per key.</summary>
public static class Curves
{
    /// <summary>A float curve through the given points, linear between them.</summary>
    /// <param name="keys">Pairs of time in 0..1 over the particle's life and value. For the size updater the value is the size in world units, not a factor on the initial size; for the rotation updater it is degrees.</param>
    /// <returns>A sampler ready for an updater or a shape builder.</returns>
    public static ComputeCurveSamplerFloat Float(params (float Time, float Value)[] keys)
    {
        var curve = new ComputeAnimationCurveFloat();

        foreach (var (time, value) in keys)
        {
            curve.KeyFrames.Add(new AnimationKeyFrame<float> { Key = time, Value = value });
        }

        return new ComputeCurveSamplerFloat { Curve = curve };
    }

    /// <summary>A colour curve through the given points, linear between them.</summary>
    /// <param name="keys">Pairs of time in 0..1 over the particle's life and colour.</param>
    /// <returns>A sampler ready for the colour updater.</returns>
    public static ComputeCurveSamplerColor4 Color(params (float Time, Color4 Value)[] keys)
    {
        var curve = new ComputeAnimationCurveColor4();

        foreach (var (time, value) in keys)
        {
            curve.KeyFrames.Add(new AnimationKeyFrame<Color4> { Key = time, Value = value });
        }

        return new ComputeCurveSamplerColor4 { Curve = curve };
    }
}