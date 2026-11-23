using Stride.CommunityToolkit.Rendering;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The material creators: each compiles one of the <see cref="MaterialDescriptors"/> with the game's content manager.
/// </summary>
public static partial class GameExtensions
{
    /// <summary>
    /// Creates a PBR (physically based rendering) material from a colour, a metalness and a glossiness,
    /// under the Lambert diffuse and microfacet specular models.
    /// </summary>
    /// <param name="game">The game instance used to access the graphics device.</param>
    /// <param name="color">The colour. Defaults to null, which uses <see cref="GameDefaults.DefaultMaterialColor"/>.</param>
    /// <param name="metalness">0 for a dielectric, which keeps its colour as diffuse and reflects a colourless 2 percent; 1 for a metal, which has no diffuse and reflects in its own colour. Defaults to 0, a matte coloured surface.</param>
    /// <param name="glossiness">0 for rough, where the highlight is a haze; 1 for a mirror. Defaults to 0.65f.</param>
    /// <returns>A new material instance with the specified or default attributes.</returns>
    /// <remarks>
    /// Compiles <see cref="MaterialDescriptors.Pbr"/> with the game's content manager, so the specular model's
    /// lookup-table environment term resolves; <c>Material.New</c> without it leaves the table empty and every metal
    /// renders black. A colour alone gives a dielectric; ask for a metal explicitly. When the material is nearly
    /// right but needs one more feature, take the descriptor from <see cref="MaterialDescriptors"/>, add to it,
    /// and compile it with <see cref="CreateMaterial(IGame, MaterialDescriptor)"/>. The manual page on materials
    /// explains what the numbers claim.
    /// </remarks>
    public static Material CreateMaterial(this IGame game, Color? color = null, float metalness = 0f, float glossiness = MaterialDescriptors.DefaultGlossiness)
        => Material.New(game.GraphicsDevice, MaterialDescriptors.Pbr(color ?? GameDefaults.DefaultMaterialColor, metalness, glossiness), game.Content);

    /// <summary>
    /// Compiles a descriptor into a material with the game's content manager, so the engine's lookup-table
    /// environment term resolves, and keeps the descriptor on it, so the material can be used as a
    /// <see cref="MaterialBlendLayer"/> later: <c>Material.New</c> leaves
    /// <see cref="Material.Descriptor"/> null, and the generator composes a layer from its features.
    /// </summary>
    /// <param name="game">The game instance used to access the graphics device.</param>
    /// <param name="descriptor">The bag of features: one from <see cref="MaterialDescriptors"/> with something added, or your own.</param>
    /// <returns>The compiled material, its descriptor attached.</returns>
    public static Material CreateMaterial(this IGame game, MaterialDescriptor descriptor)
    {
        var material = Material.New(game.GraphicsDevice, descriptor, game.Content);

        material.Descriptor = descriptor;

        return material;
    }

    /// <summary>
    /// Creates a material with a texture where the colour would be, tiled <paramref name="tiling"/> times, with
    /// the same metalness and glossiness as <see cref="CreateMaterial(IGame, Color?, float, float)"/>.
    /// </summary>
    /// <param name="game">The game instance used to access the graphics device.</param>
    /// <param name="texture">The albedo, loaded as a colour (sRGB) texture.</param>
    /// <param name="metalness">0 for a dielectric, 1 for a metal.</param>
    /// <param name="glossiness">0 for rough, 1 for a mirror.</param>
    /// <param name="tiling">How many times the texture repeats across the UV range.</param>
    /// <returns>A new material instance.</returns>
    public static Material CreateTexturedMaterial(this IGame game, Texture texture, float metalness = 0f, float glossiness = MaterialDescriptors.DefaultGlossiness, float tiling = 1f)
        => Material.New(game.GraphicsDevice, MaterialDescriptors.Textured(texture, metalness, glossiness, tiling), game.Content);

    /// <summary>
    /// Creates a material that gives off its own light: a lamp, a sign, a glowing edge. Above an intensity of 1 the
    /// colour overshoots the display range, which is what a bloom post effect picks out of the frame.
    /// </summary>
    /// <param name="game">The game instance used to access the graphics device.</param>
    /// <param name="color">The colour, lit and emitted.</param>
    /// <param name="intensity">The emissive strength; 1 is the colour as given, 5 or more blooms under post effects.</param>
    /// <returns>A new material instance.</returns>
    public static Material CreateEmissiveMaterial(this IGame game, Color color, float intensity = 1f)
        => Material.New(game.GraphicsDevice, MaterialDescriptors.Emissive(color, intensity), game.Content);

    /// <summary>
    /// Creates the material for a screen showing a texture, such as a monitor showing a render-texture camera's
    /// feed or a poster: unlit and clamped at the edges, so the picture reads as given whichever way the surface faces.
    /// </summary>
    /// <param name="game">The game instance used to access the graphics device.</param>
    /// <param name="texture">What the screen shows; a render target works as well as a loaded image.</param>
    /// <param name="intensity">The emissive strength; 1 shows the texture's own colours.</param>
    /// <returns>A new material instance.</returns>
    public static Material CreateScreenMaterial(this IGame game, Texture texture, float intensity = 1f)
        => Material.New(game.GraphicsDevice, MaterialDescriptors.Screen(texture, intensity), game.Content);

    /// <summary>
    /// Creates an unlit translucent material: a zone, a placement preview, a selection volume, a marker drawn over the
    /// scene. The colour's alpha is the opacity, and no light touches it.
    /// </summary>
    /// <param name="game">The game instance used to access the graphics device.</param>
    /// <param name="color">The colour, with its alpha as the opacity. 255 is opaque.</param>
    /// <param name="intensity">The emissive strength; 1 is the colour as given, more than 1 blooms under post effects.</param>
    /// <returns>A new material instance.</returns>
    public static Material CreateOverlayMaterial(this IGame game, Color color, float intensity = 1f)
        => Material.New(game.GraphicsDevice, MaterialDescriptors.Overlay(color, intensity), game.Content);

    /// <summary>
    /// Creates a material with flat colors ideal for 2D rendering, using emissive color unaffected by lighting.
    /// </summary>
    /// <param name="game">The game instance used to access the graphics device.</param>
    /// <param name="color">The color of the material, including alpha. Uses white if not specified.</param>
    /// <returns>A new material instance with flat coloring.</returns>
    public static Material CreateFlatMaterial(this IGame game, Color? color = null)
        => Material.New(game.GraphicsDevice, MaterialDescriptors.Flat(color ?? Color.White), game.Content);
}