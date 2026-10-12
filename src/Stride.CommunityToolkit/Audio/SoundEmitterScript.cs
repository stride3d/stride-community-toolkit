using Stride.Audio;
using Stride.Engine;

namespace Stride.CommunityToolkit.Audio;

/// <summary>
/// Positions a spatialised <see cref="SoundInstance"/> at its entity every frame, so the sound is
/// heard from where the entity is.
/// </summary>
/// <remarks>
/// <para>
/// The engine's <c>AudioEmitterComponent</c> does this for asset-based <c>Sound</c>s only. A
/// runtime instance - from <see cref="AudioSystemExtensions.CreateProceduralSound"/> or
/// <see cref="WavSound.CreateInstance"/> with <c>spatialized: true</c> - is positioned by calling
/// <see cref="SoundInstance.Apply3D"/> with an <see cref="AudioEmitter"/>, and this script does
/// that with the entity's world transform: position, forward and up from the matrix, velocity as
/// the position change since the previous frame. That last one matches how the engine's listener
/// processor estimates velocity, so Doppler sees both sides the same way.
/// </para>
/// <para>
/// Spatialisation is relative to the listener the instance was created with. By default that is the
/// engine's default listener, which never moves; pass the result of
/// <see cref="AudioSystemExtensions.AttachListener"/> when creating the instance for a listener that
/// follows the camera.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var listener = game.GetAudioSystem().AttachListener(game.GetCameraEntity());
/// var instance = chime.CreateInstance(spatialized: true, listener: listener);
///
/// entity.Add(new SoundEmitterScript { Instance = instance });
/// instance.Play();
/// </code>
/// </example>
[DataContract(nameof(SoundEmitterScript))]
[Display("Sound Emitter (runtime instance)")]
[ComponentCategory("Audio")]
public class SoundEmitterScript : SyncScript
{
    private Vector3? _previousPosition;

    /// <summary>
    /// The instance to position. Must have been created with <c>spatialized: true</c>; swapping it
    /// at runtime is fine.
    /// </summary>
    [DataMemberIgnore]
    public SoundInstance? Instance { get; set; }

    /// <summary>
    /// The emitter handed to <see cref="SoundInstance.Apply3D"/>, for reading back or for
    /// adjusting between frames.
    /// </summary>
    [DataMemberIgnore]
    public AudioEmitter Emitter { get; } = new();

    /// <inheritdoc/>
    public override void Update()
    {
        var world = Entity.Transform.WorldMatrix;
        var position = world.TranslationVector;

        Emitter.Position = position;
        Emitter.Velocity = _previousPosition is { } previous ? position - previous : Vector3.Zero;
        Emitter.Forward = SafeNormalize((Vector3)world.Row3, Vector3.UnitZ);
        Emitter.Up = SafeNormalize((Vector3)world.Row2, Vector3.UnitY);

        // The plain 3D path reads the fields above; the HRTF path reads the world matrix instead,
        // and is silent when it is left at zero.
        AudioInternals.SetWorldTransform(Emitter, world);

        _previousPosition = position;

        Instance?.Apply3D(Emitter);
    }

    private static Vector3 SafeNormalize(Vector3 axis, Vector3 fallback)
        => axis.LengthSquared() > 0 ? Vector3.Normalize(axis) : fallback;
}