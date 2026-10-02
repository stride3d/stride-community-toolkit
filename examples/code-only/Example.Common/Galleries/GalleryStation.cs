using Stride.Engine;

namespace Example.Common.Galleries;

/// <summary>
/// One place in a gallery: a frame on the ground facing the centre, and what an exhibit's methods
/// need to work in it. A method works in station coordinates - X to its right as the visitor sees
/// it, Y up, Z towards the visitor - and never learns where on the ring it stands.
/// </summary>
/// <remarks>
/// A gallery makes one of these per exhibit and fills the frame in; an example that needs more on
/// a station - a batch to draw through, a particle system to restart - derives from it and adds
/// what it needs, and the gallery's <c>configure</c> callback sets it before the exhibit's setup runs.
/// </remarks>
public class GalleryStation
{
    /// <summary>The station's number as the labels and the index board print it, counted from 1.</summary>
    public int Number { get; internal set; }

    /// <summary>The words of the exhibit on this station.</summary>
    public ExhibitInfo Exhibit { get; internal set; } = null!;

    /// <summary>The game, for anything an exhibit builds.</summary>
    public Game Game { get; internal set; } = null!;

    /// <summary>The scene entities go into.</summary>
    public Scene Scene { get; internal set; } = null!;

    /// <summary>The centre of the station's pad, on the ground.</summary>
    public Vector3 Origin { get; internal set; }

    /// <summary>The station's X axis: to the right, as seen from the gallery's centre.</summary>
    public Vector3 Right { get; internal set; }

    /// <summary>The station's Z axis: towards the gallery's centre, where the visitor stands.</summary>
    public Vector3 Forward { get; internal set; }

    /// <summary>The station's Y axis, the world's up.</summary>
    public Vector3 Up { get; } = Vector3.UnitY;

    /// <summary>Elapsed time, for anything that moves.</summary>
    public float Seconds { get; internal set; }

    /// <summary>The standard pillars the station asked for, in the order they were placed.</summary>
    public List<Pillar> Pillars { get; } = [];

    /// <summary>Whatever the exhibit's setup made for its update to use.</summary>
    public object? State { get; set; }

    /// <summary>Whether the visitor is nearest this station this frame - what a station with screen-space content draws only for.</summary>
    public bool IsCurrent { get; internal set; }

    /// <summary>What went wrong the last time the station's setup or update ran, or null: a station that throws is an empty pad with a message, not a dead gallery.</summary>
    public string? Error { get; internal set; }

    /// <summary>A point in station coordinates, in the world.</summary>
    public Vector3 At(float x, float y, float z) => Origin + Right * x + Vector3.UnitY * y + Forward * z;

    /// <summary>A point in station coordinates, in the world.</summary>
    public Vector3 At(Vector3 local) => At(local.X, local.Y, local.Z);

    /// <summary>A direction in station coordinates, in the world; not normalised.</summary>
    public Vector3 Direction(float x, float y, float z) => Right * x + Vector3.UnitY * y + Forward * z;

    /// <summary>
    /// The rotation of an entity whose X and Y lie in the station's upright plane and whose Z faces
    /// the visitor - what a world-text component with billboarding off needs to read from the front.
    /// </summary>
    public Quaternion FacingRotation()
    {
        var basis = Matrix.Identity;

        basis.Right = Right;
        basis.Up = Vector3.UnitY;
        basis.Backward = Forward;

        return Quaternion.RotationMatrix(basis);
    }
}