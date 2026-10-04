using CubeCollapse.Shared;
using Stride.CommunityToolkit.Engine;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace CubeCollapse.Setup;

/// <summary>
/// Builds the materials the cubes are painted with.
/// </summary>
/// <remarks>
/// <para>
/// A material is a GPU resource, so one is built per colour up front and shared by every cube using
/// it. Building one per cube would work and look identical, and would also mean a thousand copies of
/// the same thing - a habit worth avoiding early, because it is invisible until it is not.
/// </para>
/// <para>
/// The colour palettes use the material the Bepu playground's shapes use,
/// <see cref="GameExtensions.CreateMaterial"/>: a lit surface under the default light and the sky,
/// so a theme's colours look the same here as in the other examples. The board used to be emissive,
/// to read the same from every angle, but emissive colour added to lit colour washes mid tones out
/// toward white. The glass palettes use the material gallery's thin glass.
/// </para>
/// </remarks>
public static class MaterialFactory
{
    /// <summary>
    /// Creates one material per colour, keyed by colour so a cube can look up the material for the
    /// colour it was given.
    /// </summary>
    /// <remarks>
    /// Never scale a colour with <c>Color * float</c> here: it scales the <em>alpha</em> along with
    /// the RGB, which then multiplies the lit colour a second time through the material's alpha
    /// blend. An earlier version of this board compounded the two to about two percent of the
    /// intended colour. Balance belongs on the light intensities, not on the colour.
    /// </remarks>
    private static Dictionary<Color, Material> CreateCubeMaterials(Game game, ColourPalette palette)
    {
        var colours = palette.Colours;
        var materials = new Dictionary<Color, Material>();

        foreach (var colour in colours)
        {
            materials.Add(colour, Create(game, colour, palette));
        }

        return materials;
    }

    private static Material Create(Game game, Color colour, ColourPalette palette)
        => palette.Glass ? CreateGlassMaterial(game, colour, palette.Glossiness) : game.CreateMaterial(colour);

    /// <summary>
    /// Tinted glass: the thin-glass recipe of the material gallery (station "Thin glass"). The
    /// diffuse colour is the tint, what the glass lets through; there is no diffuse model, or the
    /// glass would go opaque.
    /// </summary>

    /// <param name="game">The running game, which owns the graphics device the material is created on.</param>
    /// <param name="tint">What the glass lets through.</param>
    /// <param name="glossiness">The material gallery uses 0.95 clear, 0.85 tinted and 0.45 frosted.</param>
    private static Material CreateGlassMaterial(Game game, Color tint, float glossiness)
    {
        var descriptor = new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(tint)),
                MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(glossiness)),
                Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0.08f)),
                SpecularModel = new MaterialSpecularThinGlassModelFeature
                {
                    RefractiveIndex = 1.563f,
                    Fresnel = new MaterialSpecularMicrofacetFresnelThinGlass(),
                    Environment = new MaterialSpecularMicrofacetEnvironmentThinGlass(),
                },
                // Both faces, so the back of each cube gets its passes too
                CullMode = CullMode.None,
            },
        };

        return Material.New(game.GraphicsDevice, descriptor, game.Content);
    }

    /// <summary>
    /// Creates the normal cube materials plus the hover variant worn by a clearable group under the
    /// mouse.
    /// </summary>
    /// <param name="game">The running game, which owns the graphics device the materials are created on.</param>
    /// <param name="palette">The palette to build for: its colours, and whether the cubes are glass.</param>
    /// <returns>Both material sets, each keyed by the cube's base colour.</returns>
    /// <remarks>
    /// The variants are built once here, not on hover: a material is a GPU resource, and hovering
    /// happens every frame. The tinting uses <see cref="Color.Lerp"/> rather than
    /// <c>Color * float</c>, which scales the alpha along with the RGB - the exact trap described on
    /// <see cref="CreateCubeMaterials"/>.
    /// </remarks>
    public static CubeMaterialSet CreateCubeMaterialSet(Game game, ColourPalette palette)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(palette);

        var brightened = new Dictionary<Color, Material>();

        foreach (var colour in palette.Colours)
        {
            // Flat colour lifted toward white reads as "lit up". Glass stays the same glass, denser
            // and deeper in colour, the way thicker coloured glass looks
            var hovered = palette.Glass ? Deepen(colour) : Color.Lerp(colour, Color.White, 0.45f);

            brightened.Add(colour, Create(game, hovered, palette));
        }

        return new CubeMaterialSet(CreateCubeMaterials(game, palette), brightened);
    }

    /// <summary>
    /// A glass tint as a thicker pane of the same glass. A thin-glass tint is what the glass lets
    /// through, so its distance from white is its colour: three times that distance gives the
    /// colour more intensity, and a quarter less overall lets less through.
    /// </summary>
    public static Color Deepen(Color tint)
    {
        static byte Channel(byte value) => (byte)(MathUtil.Clamp(255f - (255f - value) * 3f, 0f, 255f) * 0.75f);

        return new Color(Channel(tint.R), Channel(tint.G), Channel(tint.B), tint.A);
    }
}
