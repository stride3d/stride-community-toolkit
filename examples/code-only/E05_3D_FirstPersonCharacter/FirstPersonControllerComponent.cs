using Stride.BepuPhysics;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace E05_3D_FirstPersonCharacter;

// First-person controller state + tunables. All per-frame logic lives in
// FirstPersonControllerProcessor. The [DefaultEntityComponentProcessor] attribute is the key ECS
// idiom here: Stride instantiates and registers the processor automatically the first time this
// component enters a scene - no manual AddProcessor call and no SyncScript needed.
//
// The component goes on the CAMERA entity and drives the linked CharacterComponent (walk mode)
// or the camera transform directly (fly mode).
//
// Properties rather than public fields, so the processor's writes can be breakpointed. Stride's
// serializer treats public properties with a setter exactly as it would public fields.
[DataContract]
[DefaultEntityComponentProcessor(typeof(FirstPersonControllerProcessor), ExecutionMode = ExecutionMode.Runtime)]
public class FirstPersonControllerComponent : EntityComponent
{
    // The Bepu physics body this camera drives (wired in code at startup).
    [DataMemberIgnore] public CharacterComponent? Character { get; set; }

    // Tunables.
    public float MouseSensitivity { get; set; } = 1.5f;
    public float EyeHeight { get; set; } = 0.7f;        // camera height above the capsule entity's origin
    public float SprintMultiplier { get; set; } = 1.7f; // Shift speed factor
    public float FlySpeed { get; set; } = 15f;          // metres/sec in fly mode
    public float FlyBoost { get; set; } = 5f;           // Shift multiplier in fly mode

    // Runtime state, mutated by the processor.
    [DataMemberIgnore] public float Yaw { get; set; }
    [DataMemberIgnore] public float Pitch { get; set; }
    [DataMemberIgnore] public bool Fly { get; set; }
    [DataMemberIgnore] public Vector3 FlyPosition { get; set; }
    [DataMemberIgnore] public bool GravityApplied { get; set; }
    [DataMemberIgnore] public bool Initialized { get; set; }
    [DataMemberIgnore] public bool MouseLocked { get; set; }

    // Jump buffering: seconds left in which a pressed jump keeps re-arming (see the processor).
    [DataMemberIgnore] public float JumpBuffer { get; set; }
}