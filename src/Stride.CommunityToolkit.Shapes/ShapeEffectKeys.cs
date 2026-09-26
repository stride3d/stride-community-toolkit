using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders;

namespace Stride.CommunityToolkit.Shapes;

/// <summary>
/// The parameters <c>ShapeEffect</c> permutes on and the base keys a batch's fill source generates
/// its own texture, sampler and value keys from.
/// </summary>
public static class ShapeEffectKeys
{
    /// <summary>The composed fill source of a textured batch, or <c>null</c> for the plain shader.</summary>
    public static readonly PermutationParameterKey<ShaderSource> FillSource = ParameterKeys.NewPermutation<ShaderSource>();

    /// <summary>Base key for the textures a fill source samples; each one gets an indexed key derived from it.</summary>
    public static readonly ObjectParameterKey<Texture> FillMap = ParameterKeys.NewObject<Texture>();

    /// <summary>Base key for the constant colours a fill source carries; each one gets an indexed key derived from it.</summary>
    public static readonly ValueParameterKey<Color4> FillValue = ParameterKeys.NewValue<Color4>();
}