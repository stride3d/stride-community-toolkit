using Stride.Graphics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.CommunityToolkit.Rendering;

/// <summary>
/// The descriptors behind the toolkit's material helpers, for when a helper's material is nearly
/// right: take the descriptor, add or swap a feature, and compile it with
/// <c>game.CreateMaterial(descriptor)</c> or <c>Material.New</c>.
/// </summary>
/// <remarks>
/// A material in Stride is a bag of features: a colour or a texture in the diffuse slot, a diffuse
/// model that says how light spreads, a glossiness and a metalness, a specular model that says what
/// shape the reflection has, and so on. Each method here fills that bag one common way; what a
/// helper leaves out is left out of the shader, so the flat material has no lighting code at all.
/// The manual page on materials explains what the numbers claim and what the gallery shows.
/// </remarks>
public static class MaterialDescriptors
{
    /// <summary>The glossiness the helpers use when none is given: neither rough nor a mirror.</summary>
    public const float DefaultGlossiness = 0.65f;

    /// <summary>
    /// The four numbers of a PBR (physically based rendering) material - the model where a surface is
    /// described by what it is made of, a colour, a metalness and a glossiness, and light is computed
    /// from that - under the Lambert diffuse and microfacet specular models. What <c>game.CreateMaterial(colour, metalness, glossiness)</c> compiles.
    /// </summary>
    /// <param name="colour">The colour: the diffuse of a dielectric, the reflection tint of a metal.</param>
    /// <param name="metalness">0 for a dielectric, which keeps its colour as diffuse and reflects a colourless 4 percent; 1 for a metal, which has no diffuse and reflects in its own colour.</param>
    /// <param name="glossiness">0 for rough, where the highlight is a haze; 1 for a mirror.</param>
    /// <remarks>
    /// The specular model is the engine's default, whose environment term is a lookup texture the engine
    /// ships as an asset. Compile the descriptor with <c>game.CreateMaterial(descriptor)</c> or
    /// <c>Material.New(device, descriptor, game.Content)</c> so that texture resolves; <c>Material.New</c>
    /// without the content manager leaves it empty and every metal renders black.
    /// </remarks>
    public static MaterialDescriptor Pbr(Color colour, float metalness = 0f, float glossiness = DefaultGlossiness) => new()
    {
        Attributes =
        {
            Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(colour)),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(glossiness)),
            Specular = new MaterialMetalnessMapFeature(new ComputeFloat(metalness)),
            SpecularModel = Microfacet(),
        },
    };

    /// <summary>
    /// A texture where the colour was, with the same numbers as <see cref="Pbr"/>: the albedo tiled
    /// <paramref name="tiling"/> times across the mesh's UV range.
    /// </summary>
    /// <param name="texture">The albedo. Load a colour texture as sRGB; the descriptor does not decide that.</param>
    /// <param name="metalness">As in <see cref="Pbr"/>.</param>
    /// <param name="glossiness">As in <see cref="Pbr"/>.</param>
    /// <param name="tiling">How many times the texture repeats across the UV range; 1 maps it once.</param>
    public static MaterialDescriptor Textured(Texture texture, float metalness = 0f, float glossiness = DefaultGlossiness, float tiling = 1f) => new()
    {
        Attributes =
        {
            Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor(texture, TextureCoordinate.Texcoord0, new Vector2(tiling), Vector2.Zero)),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(glossiness)),
            Specular = new MaterialMetalnessMapFeature(new ComputeFloat(metalness)),
            SpecularModel = Microfacet(),
        },
    };

    /// <summary>
    /// A surface that gives off its own light: the colour as diffuse under Lambert, and the same colour
    /// as emissive at <paramref name="intensity"/>. Above 1 the emissive overshoots the display range,
    /// which is what a bloom post effect picks out of the frame. No specular: a lamp has no highlight of its own.
    /// </summary>
    /// <param name="colour">The colour, lit and emitted.</param>
    /// <param name="intensity">The emissive strength; 1 is the colour as given, 5 or more blooms under post effects.</param>
    public static MaterialDescriptor Emissive(Color colour, float intensity = 1f) => new()
    {
        Attributes =
        {
            Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(colour)),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Emissive = new MaterialEmissiveMapFeature(new ComputeColor(colour)) { Intensity = new ComputeFloat(intensity) },
        },
    };

    /// <summary>
    /// A screen showing a texture: the texture as emissive, clamped so nothing wraps at the edges, and
    /// no lighting, so the picture reads as given whichever way the surface faces. The material for a
    /// monitor showing a render-texture camera's feed.
    /// </summary>
    /// <param name="texture">What the screen shows; a render target works as well as a loaded image.</param>
    /// <param name="intensity">The emissive strength; 1 shows the texture's own colours.</param>
    public static MaterialDescriptor Screen(Texture texture, float intensity = 1f) => new()
    {
        Attributes =
        {
            Emissive = new MaterialEmissiveMapFeature(new ComputeTextureColor(texture) { AddressModeU = TextureAddressMode.Clamp, AddressModeV = TextureAddressMode.Clamp })
            {
                Intensity = new ComputeFloat(intensity),
            },
        },
    };

    /// <summary>
    /// A flat colour unaffected by lighting: the colour as emissive and as diffuse, no specular. What
    /// <c>game.CreateFlatMaterial(colour)</c> compiles; the material for 2D shapes and HUD elements.
    /// </summary>
    /// <param name="colour">The colour, alpha included.</param>
    public static MaterialDescriptor Flat(Color colour) => new()
    {
        Attributes =
        {
            Emissive = new MaterialEmissiveMapFeature(new ComputeColor(colour)),
            Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(colour)),
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = null,
            SpecularModel = null,
        },
    };

    /// <summary>
    /// The engine's microfacet specular model at its defaults: Schlick Fresnel, Smith-Schlick-GGX visibility,
    /// the GGX distribution and the GGX lookup-table environment term. One method so every descriptor here
    /// shares one choice; the lookup table needs the content manager at compile time (see <see cref="Pbr"/>).
    /// When there is no content manager to give - a tool, a test - set
    /// <c>Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial()</c>, a fit that needs no asset.
    /// </summary>
    public static MaterialSpecularMicrofacetModelFeature Microfacet() => new();
}