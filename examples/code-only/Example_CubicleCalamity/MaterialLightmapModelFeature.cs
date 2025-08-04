using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Example_CubicleCalamity;

/// <summary>
/// The baked Lightmap light for the diffuse material model attribute.
/// </summary>
[DataContract("MaterialLightmapModelFeature")]
[Display("Lightmap")]
public class MaterialLightmapModelFeature : MaterialFeature, IMaterialDiffuseModelFeature
{
    private static readonly ObjectParameterKey<Texture> Map = ParameterKeys.NewObject<Texture>();
    private static readonly ValueParameterKey<Color4> Value = ParameterKeys.NewValue<Color4>();

    //[DataMemberIgnore]
    //bool IEnergyConservativeDiffuseModelFeature.IsEnergyConservative { get; set; }

    //private bool IsEnergyConservative => ((IEnergyConservativeDiffuseModelFeature)this).IsEnergyConservative;

    [DataMember(10)]
    [Display("LightMap")]
    [NotNull]
    public IComputeColor LightMap { get; set; } = new ComputeTextureColor();

    [DataMember(20)]
    [NotNull]
    [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
    public float Intensity { get; set; } = 1.0f; // Added default value

    public override void GenerateShader(MaterialGeneratorContext context)
    {
        var shaderSource = new ShaderMixinSource();
        shaderSource.Mixins.Add(new ShaderClassSource("MaterialSurfaceShadingLightmap", Intensity));

        if (LightMap != null)
        {
            Console.WriteLine("[DEBUG] Adding LightMap composition");
            shaderSource.AddComposition("LightMap", LightMap.GenerateShaderSource(context, new MaterialComputeColorKeys(Map, Value, Color.White)));
        }

        var shaderBuilder = context.AddShading(this);
        shaderBuilder.LightDependentSurface = shaderSource;

        Console.WriteLine("[DEBUG] MaterialSurfaceShadingLightmap shader generation completed");
    }

    public bool Equals(MaterialLightmapModelFeature other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return LightMap.Equals(other.LightMap) && Intensity.Equals(other.Intensity);
    }

    public bool Equals(IMaterialShadingModelFeature other)
    {
        return Equals(other as MaterialLightmapModelFeature);
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return Equals(obj as MaterialLightmapModelFeature);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LightMap, Intensity);
    }
}