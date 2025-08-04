using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Example18_Box2DPhysics.Materials;

/// <summary>
/// A material feature that provides Box2D.NET-style SDF rendering with smooth borders and anti-aliasing.
/// This feature replaces the standard diffuse shading with a custom SDF-based shader.
/// </summary>
[DataContract("MaterialBox2DStyleFeature")]
[Display("Box2D Style")]
public class MaterialBox2DStyleFeature : MaterialFeature, IMaterialDiffuseModelFeature
{
    /// <summary>
    /// Gets or sets the base color of the shape.
    /// </summary>
    [DataMember(10)]
    [Display("Base Color")]
    public Color4 BaseColor { get; set; } = Color4.White;

    /// <summary>
    /// Gets or sets the border thickness (0.0 to 1.0).
    /// </summary>
    [DataMember(20)]
    [Display("Border Thickness")]
    [DataMemberRange(0.0, 1.0, 0.001, 0.01, 3)]
    public float BorderThickness { get; set; } = 0.02f;

    /// <summary>
    /// Gets or sets the anti-aliasing amount for smooth edges.
    /// </summary>
    [DataMember(30)]
    [Display("Anti-Aliasing")]
    [DataMemberRange(0.0, 0.1, 0.0001, 0.001, 4)]
    public float AntiAliasing { get; set; } = 0.003f;

    /// <summary>
    /// Gets or sets the shape type for SDF calculation.
    /// 0 = Rectangle, 1 = Circle, 2 = Triangle, 3 = Capsule
    /// </summary>
    [DataMember(40)]
    [Display("Shape Type")]
    [DataMemberRange(0, 3, 1, 1, 0)]
    public int ShapeType { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether to use lighter border color (Box2D.NET style).
    /// </summary>
    [DataMember(50)]
    [Display("Light Border")]
    public bool UseLightBorder { get; set; } = true;

    public override void GenerateShader(MaterialGeneratorContext context)
    {
        var shaderSource = new ShaderMixinSource();

        // Use the Box2DStyleShader we created with parameters
        shaderSource.Mixins.Add(new ShaderClassSource("Box2DStyleShader", BaseColor, BorderThickness, AntiAliasing, ShapeType));

        // Create a shader builder for diffuse model replacement
        var shaderBuilder = context.AddShading(this);
        shaderBuilder.LightDependentSurface = shaderSource;
    }

    public bool Equals(IMaterialShadingModelFeature other)
    {
        return Equals(other as MaterialBox2DStyleFeature);
    }

    public bool Equals(MaterialBox2DStyleFeature other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return BaseColor.Equals(other.BaseColor) && 
               BorderThickness.Equals(other.BorderThickness) && 
               AntiAliasing.Equals(other.AntiAliasing) && 
               ShapeType == other.ShapeType &&
               UseLightBorder == other.UseLightBorder;
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return Equals(obj as MaterialBox2DStyleFeature);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BaseColor, BorderThickness, AntiAliasing, ShapeType, UseLightBorder);
    }
}