using Stride.BepuPhysics;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Example20_BepuFirstPersonCharacter;

// First-person controller state + tunables. All per-frame logic lives in
// FirstPersonControllerProcessor. The [DefaultEntityComponentProcessor] attribute is the key ECS
// idiom here: Stride instantiates and registers the processor automatically the first time this
// component enters a scene - no manual AddProcessor call and no SyncScript needed.
//
// The component goes on the CAMERA entity and drives the linked CharacterComponent (walk mode)
// or the camera transform directly (fly mode).
[DataContract]
[DefaultEntityComponentProcessor(typeof(FirstPersonControllerProcessor), ExecutionMode = ExecutionMode.Runtime)]
public class FirstPersonControllerComponent : EntityComponent
{
    // The Bepu physics body this camera drives (wired in code at startup).
    [DataMemberIgnore] public CharacterComponent? Character;

    // Tunables.
    public float MouseSensitivity = 1.5f;
    public float EyeHeight = 0.7f;        // camera height above the capsule entity's origin
    public float SprintMultiplier = 1.7f; // Shift speed factor (Move() treats vector length as a speed factor)
    public float FlySpeed = 15f;          // metres/sec in fly mode
    public float FlyBoost = 5f;           // Shift multiplier in fly mode

    // Runtime state, mutated by the processor.
    [DataMemberIgnore] public float Yaw;
    [DataMemberIgnore] public float Pitch;
    [DataMemberIgnore] public bool Fly;
    [DataMemberIgnore] public Vector3 FlyPosition;
    [DataMemberIgnore] public bool GravityApplied;
    [DataMemberIgnore] public bool Initialized;
    [DataMemberIgnore] public bool MouseLocked;

    // Jump buffering: seconds left in which a pressed jump keeps re-arming (see the processor).
    [DataMemberIgnore] public float JumpBuffer;
}
