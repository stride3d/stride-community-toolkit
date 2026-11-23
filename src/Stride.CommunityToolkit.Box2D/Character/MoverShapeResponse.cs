namespace Stride.CommunityToolkit.Box2D;

/// <summary>
/// How a shape pushes a <see cref="CharacterMover2D"/> that runs into it. Stored in the shape's Box2D
/// user data by <see cref="CharacterMover2D.SetResponse"/>; a shape without one is rigid and clips
/// the mover's velocity, the sample's default.
/// </summary>
/// <param name="MaxPush">
/// The furthest the shape may push the mover out per step, in metres. A small value makes a soft
/// obstacle the mover can walk through slowly; <see cref="float.MaxValue"/> is rigid.
/// </param>
/// <param name="ClipVelocity">
/// Whether the mover's velocity is clipped against the contact plane. On for anything the mover
/// should ride or slide along, like an elevator; off for a soft obstacle it should keep pushing into.
/// </param>
public sealed record MoverShapeResponse(float MaxPush, bool ClipVelocity);