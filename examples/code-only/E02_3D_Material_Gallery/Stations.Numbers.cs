using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace E02_3D_Material_Gallery;

// The numbers: what a material is before any texture touches it. A colour, a glossiness, a
// metalness, and the microfacet model that turns them into a highlight.
public static class NumberStations
{
    /// <summary>
    /// The baseline: a diffuse colour under the Lambert model and nothing else - no specular
    /// feature at all, so the surface has no highlight and no reflection, only light spread evenly.
    /// Every other station adds to this. V cycles the colour.
    /// </summary>
    public static void DiffuseColour(MaterialStation s)
    {
        s.Clear();

        var colour = s.Pick("terracotta", "teal", "ochre") switch
        {
            0 => new Color(190, 80, 55),
            1 => new Color(40, 140, 140),
            _ => new Color(200, 150, 50),
        };

        s.PlaceTrio(s.Material(new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(colour)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            },
        }));
    }

    /// <summary>
    /// Five spheres with the same colour and metalness, glossiness 0 to 1: rough spreads the
    /// highlight into a haze, glossy tightens it to a point and the sky comes into focus in it.
    /// V switches the row between a dielectric and a metal, where the same sweep runs from brushed
    /// to mirror.
    /// </summary>
    public static void GlossinessSweep(MaterialStation s)
    {
        s.Clear();

        var metalness = s.Pick("dielectric", "metal") == 0 ? 0f : 1f;
        var colour = metalness > 0f ? new Color(255, 200, 120) : new Color(80, 130, 200);

        s.PlaceRow([.. Enumerable.Range(0, 5).Select(i => s.Material(Recipes.Pbr(colour, glossiness: i / 4f, metalness)))]);
    }

    /// <summary>
    /// Five spheres, metalness 0 to 1 at one glossiness: a dielectric keeps its colour as diffuse
    /// and reflects a colourless 4 percent; a metal has no diffuse at all, and its colour is the
    /// colour of its reflection. Halfway values are what a metalness map paints where paint meets
    /// bare metal. V cycles the colour.
    /// </summary>
    public static void MetalnessSweep(MaterialStation s)
    {
        s.Clear();

        var colour = s.Pick("gold", "copper", "steel blue") switch
        {
            0 => new Color(255, 200, 100),
            1 => new Color(220, 120, 80),
            _ => new Color(120, 150, 200),
        };

        s.PlaceRow([.. Enumerable.Range(0, 5).Select(i => s.Material(Recipes.Pbr(colour, glossiness: 0.85f, metalness: i / 4f)))]);
    }

    /// <summary>
    /// The other way to say the same thing: a specular colour (F0, what the surface reflects head
    /// on) given directly instead of derived from a metalness. Plastic reflects a grey 4 percent
    /// and keeps its diffuse; gold and silver reflect their own colour and have a black diffuse,
    /// which is what a metal is. This is the workflow the Material Package's textures use.
    /// </summary>
    public static void SpecularColour(MaterialStation s)
    {
        s.Clear();

        s.PlaceRow(
        [
            s.Material(Recipes.SpecularWorkflow(diffuse: new Color(200, 40, 40), specular: new Color(10, 10, 10), glossiness: 0.8f)),
            s.Material(Recipes.SpecularWorkflow(diffuse: Color.Black, specular: new Color(255, 180, 75), glossiness: 0.9f)),
            s.Material(Recipes.SpecularWorkflow(diffuse: Color.Black, specular: new Color(242, 237, 224), glossiness: 0.9f)),
        ], spacing: 2.4f);
    }

    /// <summary>
    /// The same sphere under three normal distribution functions. The distribution says how the
    /// microscopic facets are tilted, which is the shape and tail of the highlight: GGX has the long
    /// tail every modern renderer uses, Beckmann falls off sharply, Blinn-Phong is the classic. V
    /// cycles the glossiness, where the three differ most in the middle.
    /// </summary>
    public static void Distributions(MaterialStation s)
    {
        s.Clear();

        var glossiness = s.Pick("glossiness 0.5", "glossiness 0.7", "glossiness 0.9") switch { 0 => 0.5f, 1 => 0.7f, _ => 0.9f };
        var colour = new Color(60, 60, 70);

        s.PlaceRow(
        [
            s.Material(Recipes.Pbr(colour, glossiness, metalness: 0f, new MaterialSpecularMicrofacetNormalDistributionGGX())),
            s.Material(Recipes.Pbr(colour, glossiness, metalness: 0f, new MaterialSpecularMicrofacetNormalDistributionBeckmann())),
            s.Material(Recipes.Pbr(colour, glossiness, metalness: 0f, new MaterialSpecularMicrofacetNormalDistributionBlinnPhong())),
        ], spacing: 2.4f);
    }

    /// <summary>
    /// A mirror: glossiness 1, metalness 1 and no Fresnel term, so the whole environment comes back
    /// at full strength from every angle. It is the community's recipe for checking a cubemap
    /// (Stride discussion 3031), with the implicit visibility and Blinn-Phong distribution it
    /// uses. A metal's diffuse colour is the colour of its reflection, so a gold tint makes a gold
    /// mirror; V switches. There is no Fresnel variation because there would be nothing to see:
    /// the Fresnel function shapes the highlights of lights, pinpoints at this glossiness, while the
    /// reflection of the sky - all of what a mirror shows - is the environment term's business, and
    /// that is the same lookup table whichever Fresnel is chosen. On a white metal even the highlights
    /// agree, since a reflectance of one has nowhere to rise to.
    /// </summary>
    public static void Mirror(MaterialStation s)
    {
        s.Clear();

        var colour = s.Pick("mirror", "gold mirror") == 0 ? Color.White : new Color(255, 200, 90);

        var mirror = s.Material(new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(colour)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(1f)),
                Specular = new MaterialMetalnessMapFeature(new ComputeFloat(1f)),
                SpecularModel = new MaterialSpecularMicrofacetModelFeature
                {
                    Fresnel = new MaterialSpecularMicrofacetFresnelNone(),
                    Visibility = new MaterialSpecularMicrofacetVisibilityImplicit(),
                    NormalDistribution = new MaterialSpecularMicrofacetNormalDistributionBlinnPhong(),
                },
            },
        });

        s.PlaceTrio(mirror);
    }

}