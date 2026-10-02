using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;

namespace E02_3D_Material_Gallery;

/// <summary>
/// A haircut's shape: how many cards, how long on top and at the sides, how wide, and how they
/// leave the scalp - lifted and swept back for a quiff, lying down for a bob or long hair.
/// </summary>
/// <param name="Strips">How many cards.</param>
/// <param name="Segments">Quads per card along its length.</param>
/// <param name="TopLength">A card's length on the crown, in world units.</param>
/// <param name="SideLength">A card's length at the sides and the nape.</param>
/// <param name="Width">A card's width at the root.</param>
/// <param name="Droop">How much of the length gravity takes: small for hair that holds, large for hair that hangs.</param>
/// <param name="Lift">How far top hair rises off the scalp before it falls: a quiff is lifted, a bob is not.</param>
/// <param name="Sweep">How much top hair is combed back rather than down.</param>
public sealed record HairStyle(int Strips, int Segments, float TopLength, float SideLength, float Width, float Droop, float Lift, float Sweep)
{
    /// <summary>Short at the sides, long on top and swept up and back, the forehead clear.</summary>
    public static HairStyle Quiff { get; } = new(520, 6, 0.75f, 0.28f, Width: 0.04f, 0.35f, 0.9f, 0.8f);

    /// <summary>Chin length all round, lying flat.</summary>
    public static HairStyle Bob { get; } = new(400, 8, 1.1f, 1f, Width: 0.07f, 1f, 0.15f, 0.5f);

    /// <summary>Hanging well past the shoulders; the head it sits on should be raised.</summary>
    public static HairStyle Long { get; } = new(400, 12, 2.4f, 2.2f, Width: 0.08f, 1.4f, 0.1f, 0.5f);
}

/// <summary>
/// What the hair shading models are written for: cards. A card is a thin strip carrying a strand
/// texture whose alpha says where the strands are, and the hair material's three passes - an
/// opaque one for the solid cores, two blended ones for the soft edges - sort the rest out. The
/// strand direction the shading needs is the card's tangent, so the strips carry one. Both the
/// texture and the haircut are made here at runtime; no asset, no file.
/// </summary>
public static class HairCards
{
    /// <summary>
    /// A strand texture: many thin vertical strands, each with its own shade and wobble, thinning
    /// and fading towards the tip. Alpha is the strand coverage; where there is no strand the
    /// texel is clear. Colour, so sRGB.
    /// </summary>
    public static Texture Strands(GraphicsDevice device, Color baseColour, int size = 256, int strands = 320)
    {
        var pixels = new Color[size * size];
        // The same seed every run, so the texture and a screenshot of it are repeatable
        var random = new Random(11);

        for (var i = 0; i < strands; i++)
        {
            // One strand: where it starts, how it sways, how wide, how dark and how long
            var x0 = (float)random.NextDouble() * size;
            var wobble = ((float)random.NextDouble() - 0.5f) * 5f;
            var phase = (float)random.NextDouble() * MathF.Tau;
            var width = 2.2f + (float)random.NextDouble() * 2.6f;
            var shade = 0.65f + (float)random.NextDouble() * 0.5f;
            var length = 0.7f + (float)random.NextDouble() * 0.3f;

            // Row by row from the root: the strand sways sideways and thins towards its tip
            for (var y = 0; y < size * length; y++)
            {
                var v = y / (float)size;
                var x = x0 + MathF.Sin(v * 4f + phase) * wobble;
                var w = width * (1f - 0.6f * v);
                var reach = (int)MathF.Ceiling(w);

                for (var dx = -reach; dx <= reach; dx++)
                {
                    var px = (int)x + dx;
                    if (px < 0 || px >= size) continue;

                    // Full at the strand's centre, nothing at its edge
                    var cover = 1f - MathF.Abs(px - x) / w;
                    if (cover <= 0f) continue;

                    // Solid in the core, soft at the edge, fading over the last part of the length
                    var alpha = MathF.Min(1f, cover * 2.2f) * (1f - MathF.Max(0f, (v / length - 0.85f) / 0.15f));
                    ref var pixel = ref pixels[y * size + px];

                    // Strands overlap: the more opaque one keeps the texel
                    if (alpha * 255f > pixel.A)
                    {
                        pixel = new Color(
                            (byte)Math.Min(255, baseColour.R * shade),
                            (byte)Math.Min(255, baseColour.G * shade),
                            (byte)Math.Min(255, baseColour.B * shade),
                            (byte)(alpha * 255f));
                    }
                }
            }
        }

        return Texture.New2D(device, size, size, PixelFormat.R8G8B8A8_UNorm_SRgb, pixels);
    }

    /// <summary>
    /// A haircut on a head: the face is towards +Z, so the roots stop at a hairline above the
    /// forehead and at the ears, the crown's cards are longer than the sides', and every card
    /// leaves the scalp the way the style says - swept back and lifted on top, down at the sides -
    /// then falls under gravity while never passing inside the head. Each card is a run of quads
    /// lying flat on the scalp, with the strand direction in its tangent and the texture's V along
    /// its length.
    /// </summary>
    /// <param name="headRadius">The sphere the roots sit on.</param>
    /// <param name="style">The cut.</param>
    /// <param name="directionInBitangent">
    /// Store the card's width direction as the tangent instead of the strand direction, so that
    /// the bitangent the shader rebuilds - normal crossed with tangent - is the strand direction.
    /// That is the layout the engine's default direction function expects; the default layout
    /// here is the strand in the tangent, for the other function.
    /// </param>
    public static Model Haircut(GraphicsDevice device, float headRadius, HairStyle style, bool directionInBitangent = false)
    {
        using var builder = new MeshBuilder();

        builder.WithIndexType(IndexingType.Int16);

        var position = builder.WithPosition<Vector3>();
        var normal = builder.WithNormal<Vector3>();
        var tangent = builder.WithTangent<Vector4>();
        var texcoord = builder.WithTextureCoordinate<Vector2>();
        var random = new Random(5);
        var points = new Vector3[style.Segments + 1];

        for (var i = 0; i < style.Strips; i++)
        {
            // A root anywhere on the head that hair grows: above the ears, and on the front only
            // above the hairline, which rises towards the temples
            Vector3 radial;
            do
            {
                var phi = (float)random.NextDouble() * MathF.Tau;
                var y = -1f + (float)random.NextDouble() * 2f;
                var ring = MathF.Sqrt(MathF.Max(0f, 1f - y * y));
                radial = new Vector3(ring * MathF.Cos(phi), y, ring * MathF.Sin(phi));
            }
            while (radial.Y < 0.05f || (radial.Z > 0f && radial.Y < 0.45f + 0.4f * radial.Z));

            var root = radial * headRadius;

            // Sides to crown, 0 to 1: what the length and the lift follow
            var crown = MathUtil.SmoothStep(MathUtil.Clamp((radial.Y - 0.1f) / 0.7f, 0f, 1f));
            var length = MathUtil.Lerp(style.SideLength, style.TopLength, crown) * (0.85f + 0.3f * (float)random.NextDouble());

            // Where the card sets off: down the head at the sides, back and up on top, all of it
            // along the scalp with a touch outward so the roots are not buried
            var down = Tangential(-Vector3.UnitY, radial);
            var back = Tangential(-Vector3.UnitZ, radial);
            var start = Vector3.Normalize(Vector3.Lerp(down, back, style.Sweep * crown) + Vector3.UnitY * (style.Lift * crown) + radial * 0.2f);

            for (var s = 0; s <= style.Segments; s++)
            {
                var t = s / (float)style.Segments;
                var p = root + start * (length * t) + Vector3.UnitY * (-style.Droop * length * t * t);

                // Never inside the head: pushed out to just above the scalp, a little further along the card
                var clearance = headRadius + 0.03f + 0.02f * t;
                if (p.LengthSquared() < clearance * clearance) p = Vector3.Normalize(p) * clearance;

                points[s] = p;
            }

            var first = builder.VertexCount;

            for (var s = 0; s <= style.Segments; s++)
            {
                var t = s / (float)style.Segments;
                var direction = Vector3.Normalize(s < style.Segments ? points[s + 1] - points[s] : points[s] - points[s - 1]);
                var outward = Vector3.Normalize(points[s]);
                var across = Vector3.Cross(outward, direction);
                across = across.LengthSquared() < 0.0001f ? Vector3.UnitX : Vector3.Normalize(across);
                var cardNormal = Vector3.Normalize(Vector3.Cross(across, direction));
                var halfWidth = style.Width * 0.5f * (1f - 0.3f * t);

                // The strand direction goes in the tangent, or the width does and the shader's
                // bitangent - normal x tangent - comes out as the strand direction
                var tangentValue = new Vector4(directionInBitangent ? across : direction, 1f);

                for (var side = 0; side < 2; side++)
                {
                    builder.AddVertex();
                    builder.SetElement(position, points[s] + across * (side == 0 ? -halfWidth : halfWidth));
                    builder.SetElement(normal, cardNormal);
                    builder.SetElement(tangent, tangentValue);
                    builder.SetElement(texcoord, new Vector2(side, t));
                }
            }

            for (var s = 0; s < style.Segments; s++)
            {
                var a = first + s * 2;

                builder.AddIndex(a);
                builder.AddIndex(a + 2);
                builder.AddIndex(a + 1);
                builder.AddIndex(a + 1);
                builder.AddIndex(a + 2);
                builder.AddIndex(a + 3);
            }
        }

        return new Model { new Mesh { Draw = builder.ToMeshDraw(device), MaterialIndex = 0 } };
    }

    /// <summary>A direction flattened onto the scalp at a root: the part of it that is not along the radial.</summary>
    private static Vector3 Tangential(Vector3 direction, Vector3 radial)
    {
        var flat = direction - radial * Vector3.Dot(direction, radial);

        return flat.LengthSquared() < 0.0001f ? Vector3.UnitX : Vector3.Normalize(flat);
    }
}