using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Rendering;

/// <summary>
/// Non-billboard world text takes the entity's orientation and nothing else: a scaled entity must not
/// scale or skew the text, whose size is <see cref="WorldTextComponent.Height"/> alone.
/// </summary>
public class WorldTextOrientationTests
{
    [Fact]
    public void AScaledRotatedEntity_GivesAPureRotation()
    {
        var component = new WorldTextComponent { Text = "Scaled", Billboard = false };
        var rotation = Quaternion.RotationY(MathUtil.PiOverTwo);
        var world = Matrix.Scaling(2f) * Matrix.RotationQuaternion(rotation) * Matrix.Translation(3f, 4f, 5f);

        var orientation = WorldTextRenderer.GetOrientation(component, in world, Vector3.Zero, Vector3.UnitZ);
        var expected = Matrix.RotationQuaternion(rotation);

        // The axes keep their unit length, so Height is the text's only size
        Assert.Equal(1f, orientation.Right.Length(), Tolerances.Geometry);
        Assert.Equal(1f, orientation.Up.Length(), Tolerances.Geometry);
        Assert.Equal(1f, orientation.Backward.Length(), Tolerances.Geometry);

        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                Assert.Equal(expected[row, column], orientation[row, column], Tolerances.Geometry);
            }
        }

        Assert.Equal(Vector3.Zero, orientation.TranslationVector);
    }

    [Fact]
    public void AZeroScaleEntity_StaysWorldAligned()
    {
        // No rotation can be read off a matrix with a flattened axis; the text keeps its Height and loses the turn
        var component = new WorldTextComponent { Text = "Flat", Billboard = false };
        var world = Matrix.Scaling(0f, 1f, 1f) * Matrix.RotationY(MathUtil.PiOverTwo) * Matrix.Translation(1f, 2f, 3f);

        var orientation = WorldTextRenderer.GetOrientation(component, in world, Vector3.Zero, Vector3.UnitZ);

        Assert.Equal(Matrix.Identity, orientation);
    }

    [Fact]
    public void AnUnscaledEntity_IsUnchanged()
    {
        var component = new WorldTextComponent { Text = "Plain", Billboard = false };
        var rotation = Quaternion.RotationYawPitchRoll(0.4f, 0.3f, 0.2f);
        var world = Matrix.RotationQuaternion(rotation);

        var orientation = WorldTextRenderer.GetOrientation(component, in world, Vector3.Zero, Vector3.UnitZ);

        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                Assert.Equal(world[row, column], orientation[row, column], Tolerances.Geometry);
            }
        }
    }
}