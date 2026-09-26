using Stride.Core;
using Stride.Core.Annotations;
using Stride.Rendering.Images;
using System.ComponentModel;

namespace Stride.CommunityToolkit.Effects.PostProcessing;

/// <summary>
/// A thermal-camera look: luminance stands in for temperature and is painted onto a false-colour
/// ramp, from the dark blue of cold through violet and red to the orange, yellow and white of hot.
/// Add it to a <see cref="ColorTransformGroup"/>'s transforms, on the main camera or on a camera
/// that draws into a texture.
/// </summary>
/// <remarks>
/// It runs on HDR colour before any tone map. <see cref="Exposure"/> decides how much of the scene
/// reads as hot: raise it in a dark scene, lower it in a bright one.
/// </remarks>
[DataContract("Thermal")]
public sealed class Thermal : ColorTransform
{
    /// <summary>Creates the transform with its defaults.</summary>
    public Thermal() : base("ThermalShader")
    {
    }

    /// <summary>Scales the luminance before it is mapped. Defaults to 2.5.</summary>
    [DataMember(10)]
    [DefaultValue(2.5f)]
    [DataMemberRange(0.0, 10.0, 0.1, 0.5, 2)]
    public float Exposure { get; set; } = 2.5f;

    /// <inheritdoc/>
    public override void UpdateParameters(ColorTransformContext context)
    {
        Parameters.Set(ThermalShaderKeys.Exposure, Exposure);

        base.UpdateParameters(context);
    }
}