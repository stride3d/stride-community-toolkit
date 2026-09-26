using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Rendering.Images;
using System.ComponentModel;

namespace Stride.CommunityToolkit.Effects.PostProcessing;

/// <summary>
/// A night-vision look: the image's luminance amplified and drawn in one colour, with bright sources
/// blooming to white, film grain, scanlines and a vignette. Add it to a
/// <see cref="ColorTransformGroup"/>'s transforms, on the main camera or on a camera that draws into
/// a texture.
/// </summary>
/// <remarks>
/// It runs on HDR colour before any tone map, so the amplification acts on real light and the
/// tone map that follows handles what comes out. On a camera drawing into a texture there is no tone
/// map of its own, and the main view's tone map does that job when it draws the texture.
/// </remarks>
[DataContract("NightVision")]
public sealed class NightVision : ColorTransform
{
    /// <summary>Creates the transform with its defaults.</summary>
    public NightVision() : base("NightVisionShader")
    {
    }

    /// <summary>How much the luminance is amplified. Defaults to 4.</summary>
    [DataMember(10)]
    [DefaultValue(4f)]
    [DataMemberRange(0.0, 20.0, 0.1, 1.0, 2)]
    public float Gain { get; set; } = 4f;

    /// <summary>The phosphor's colour. Defaults to the classic green.</summary>
    [DataMember(20)]
    public Color3 Tint { get; set; } = new(0.2f, 1f, 0.35f);

    /// <summary>Strength of the grain, 0 for none. Defaults to 0.08.</summary>
    [DataMember(30)]
    [DefaultValue(0.08f)]
    [DataMemberRange(0.0, 0.5, 0.01, 0.05, 2)]
    public float NoiseAmount { get; set; } = 0.08f;

    /// <summary>Where the vignette begins, 0 at the centre to 1 at the corners. Defaults to 0.55.</summary>
    [DataMember(40)]
    [DefaultValue(0.55f)]
    [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
    public float VignetteRadius { get; set; } = 0.55f;

    /// <inheritdoc/>
    public override void UpdateParameters(ColorTransformContext context)
    {
        Parameters.Set(NightVisionShaderKeys.Gain, Gain);
        Parameters.Set(NightVisionShaderKeys.Tint, Tint);
        Parameters.Set(NightVisionShaderKeys.NoiseAmount, NoiseAmount);
        Parameters.Set(NightVisionShaderKeys.VignetteRadius, VignetteRadius);
        Parameters.Set(NightVisionShaderKeys.Time, (float)(context.RenderContext.Time?.Total.TotalSeconds ?? 0.0));

        base.UpdateParameters(context);
    }
}