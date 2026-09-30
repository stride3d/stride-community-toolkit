using Example.Common.Galleries;
using Stride.CommunityToolkit.Rendering;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace E02_3D_Material_Gallery;

/// <summary>
/// The exhibits, in the order the ring shows them: the numbers first, then the maps, then the
/// inputs, then the surfaces and the shading models that change what light does. Every station is
/// one static setup method that builds its materials once and places them in station coordinates;
/// a station with variations reads <see cref="MaterialStation.Variation"/> and V runs its setup
/// again. Add a station here and the gallery grows to fit.
/// </summary>
public static class Stations
{
    public static IReadOnlyList<Exhibit<MaterialStation>> All { get; } =
    [
        new("Diffuse colour", "Lambert and one colour: the baseline everything else adds to", nameof(MaterialDiffuseLambertModelFeature), Setup: NumberStations.DiffuseColour, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Glossiness sweep", "five spheres from rough to mirror: what one number does", nameof(MaterialGlossinessMapFeature), Setup: NumberStations.GlossinessSweep, Anchor: new Vector3(0f, 1.6f, 0f)),
        new("Metalness sweep", "dielectric to metal: why a metal has no diffuse colour", nameof(MaterialMetalnessMapFeature), Setup: NumberStations.MetalnessSweep, Anchor: new Vector3(0f, 1.6f, 0f)),
        new("Specular colour", "the other workflow: F0 given as a colour instead of metalness", nameof(MaterialSpecularMapFeature), Setup: NumberStations.SpecularColour, Anchor: new Vector3(0f, 1.6f, 0f)),
        new("Three distributions", "GGX, Beckmann and Blinn-Phong: the shape of the highlight", nameof(MaterialSpecularMicrofacetModelFeature.NormalDistribution), Setup: NumberStations.Distributions, Anchor: new Vector3(0f, 1.6f, 0f)),
        new("Mirror", "glossiness 1, metalness 1, no Fresnel: the whole environment back, the cubemap check", nameof(MaterialSpecularMicrofacetFresnelNone), Setup: NumberStations.Mirror, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Albedo texture", "a texture where the colour was, tiled by its scale", nameof(ComputeTextureColor), Setup: MapStations.AlbedoTexture, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Normal map", "the same albedo with and without its normal map", nameof(MaterialNormalMapFeature), Setup: MapStations.NormalMap, Anchor: new Vector3(1.6f, 1.8f, 0f)),
        new("Gloss and metal maps", "wood, iron and gold: the numbers varying per texel", nameof(MaterialMetalnessMapFeature.MetalnessMap), Setup: MapStations.GlossAndMetalMaps, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Occlusion", "ambient occlusion darkens the crevices, left without, right with", nameof(MaterialOcclusionMapFeature), Setup: MapStations.Occlusion, Anchor: new Vector3(1.6f, 1.8f, 0f)),
        new("Emissive", "light from the surface itself, its intensity changed every frame", nameof(MaterialEmissiveMapFeature), MapStations.EmissivePulse, MapStations.Emissive, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Animated parameters", "a tint, a scroll and a glossiness driven through keys every frame, no rebuild", nameof(MaterialParameters), MapStations.AnimatedParametersUpdate, MapStations.AnimatedParameters, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Vertex colours", "a colour per vertex from a MeshBuilder mesh, no texture", nameof(ComputeVertexStreamColor), Setup: MapStations.VertexColours, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Node arithmetic", "two textures combined by an operator before the material sees them", nameof(ComputeBinaryColor), Setup: MapStations.NodeArithmetic, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Custom shader node", "a ComputeColor class of your own in any slot, twenty lines of shader", nameof(ComputeShaderClassColor), Setup: InputStations.CustomNode, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Custom feature", "a feature of your own adds shaders to a stage: a vertex wobble, a pixel dissolve", nameof(MaterialFeature), InputStations.CustomFeatureUpdate, InputStations.CustomFeature, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Runtime textures", "a height map and its normal map computed in C#, no asset", nameof(Stride.Graphics.Texture.New2D), Setup: InputStations.RuntimeTexturesStation, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Texture loading", "the same files loaded wrong on the left, right on the right: colour space, green, alpha, mipmaps", nameof(TextureLoader), Setup: InputStations.TextureLoading, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Transparency", "blend, additive, cutoff and a dithered cutoff, on one shape", nameof(MaterialTransparencyBlendFeature), Setup: SurfaceStations.Transparency, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Thin glass", "the thin-glass model: a refractive index, its own Fresnel, the sky through it", nameof(MaterialSpecularThinGlassModelFeature), Setup: SurfaceStations.ThinGlass, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Clear coat", "car paint: base, metal flakes and a coat with its own gloss", nameof(MaterialClearCoatFeature), Setup: SurfaceStations.ClearCoat, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Cel shading", "light quantised into bands, by a function or a ramp texture", nameof(MaterialDiffuseCelShadingModelFeature), Setup: SurfaceStations.CelShading, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Hair", "Kajiya-Kay: the highlight runs along the strand, not round the normal", nameof(MaterialSpecularHairModelFeature), SurfaceStations.HairSway, SurfaceStations.Hair, Anchor: new Vector3(0f, 2.2f, 0f)),
        new("Hair passes and functions", "the three passes in colour, scattering shadows, per-strand noise, the other direction", nameof(MaterialSpecularHairModelFeature.DebugRenderPasses), Setup: SurfaceStations.HairParts, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Subsurface scattering", "light in at one point, out at another: skin and wax", nameof(MaterialSubsurfaceScatteringFeature), Setup: SurfaceStations.Subsurface, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Displacement", "a height map moves the vertices: real bumps, real silhouette", nameof(MaterialDisplacementMapFeature), Setup: ModelStations.Displacement, Anchor: new Vector3(0f, 2.4f, 0f)),
        new("Tessellation", "PN triangles round off a five-segment sphere on the GPU", nameof(MaterialTessellationPNFeature), Setup: ModelStations.Tessellation, Anchor: new Vector3(0f, 2f, 0f)),
        new("Overrides", "a UV scale for every map at once, and which side of a face is drawn", nameof(MaterialOverrides), Setup: ModelStations.Overrides, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Layers", "a material over a material, mixed by a mask: painted and rusted iron", nameof(MaterialBlendLayer), Setup: ModelStations.Layers, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Highlight shell", "the model drawn again a little larger with a glow: a hover highlight from a material alone", nameof(HighlightShell), ModelStations.HighlightShellUpdate, ModelStations.HighlightShellStation, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("Materials at runtime", "one model, three teapots: an override, the shared material, a slot's shadow, culling, a rebuild", nameof(ModelComponent.Materials), ModelStations.AtRuntimeUpdate, ModelStations.AtRuntime, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("The Material Package, in code", "every material of Game Studio's pack, from its .sdmat", nameof(PackMaterials), Setup: PackStations.ThePack, Anchor: new Vector3(0f, 1.6f, 0f)),
        new("Sample recipes", "five tunings from the engine's samples: a mirrored gloss map, a map reused, a normal weakened, a neon layer, a tinted grid", nameof(MaterialBlendLayer.BlendMap), Setup: SampleStations.SampleRecipes, Anchor: new Vector3(0f, 1.8f, 0f)),
        new("The lot", "every slot filled: what a full PBR material is in code", nameof(MaterialDescriptor), Setup: PackStations.TheLot, Anchor: new Vector3(0f, 1.8f, 0f)),
    ];
}