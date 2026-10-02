using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The ship that flies itself. Every figure is a function of time, so freezing the clock freezes
/// the ship. What the pilot chooses with the pointer is kept here too: the target, the selected
/// wingman, the modes and the power setting.
/// </summary>
public sealed class ShipState
{
    /// <summary>The radar's range in kilometres.</summary>
    public const float RadarRange = 5f;

    // A new comms line arrives this often, in seconds
    private const float CommsInterval = 2.4f;

    /// <summary>How many decoys the ship carries when full.</summary>
    private const int DecoyLoad = 48;


    public FlightState Flight { get; } = new();

    public SystemsState Systems { get; } = new();

    public PowerState Power { get; } = new();

    public Contact[] Contacts { get; } =
    [
        new("RAIDER-1", "Corvette", hostile: true),
        new("BADGER", "Fighter", hostile: false),
        new("VIPER", "Fighter", hostile: false),
        new("MAKO", "Interceptor", hostile: false),
        new("HAULER-7", "Freighter", hostile: false),
    ];

    public Wingman[] Wing { get; } =
    [
        new("BADGER", 1f),
        new("VIPER", 0.62f),
        new("MAKO", 0.87f),
        new("KESTREL", 0.3f),
    ];

    public Mode[] Modes { get; } =
    [
        new("VTOL", available: false),
        new("GEAR") { Engaged = true },
        new("LOCK"),
        new("DECOY"),
    ];

    public CommsLine[] Comms { get; } =
    [
        new("BADGER", "wait, first fight me"),
        new("VIPER", "I am not waiting ten minutes"),
        new("BADGER", "I am here, outside Grim"),
        new("SYSTEM", "Jump beacon in range", CommsKind.System),
        new("VIPER", "ok, give me a minute"),
        new("MAKO", "copy, holding at the gate"),
        new("RAIDER-1", "you are in our lane", CommsKind.Hostile),
        new("BADGER", "switching ships?"),
        new("VIPER", "we are having a real fight"),
        new("SYSTEM", "Shield cells at half charge", CommsKind.System),
        new("MAKO", "that one is a transport snub"),
        new("RAIDER-1", "last warning", CommsKind.Hostile),
        new("KESTREL", "fuel is low, heading back"),
        new("BADGER", "look at your six"),
    ];

    /// <summary>Which contact is the target.</summary>
    public int TargetIndex { get; set; }

    public Contact Target => Contacts[TargetIndex];

    /// <summary>Which wingman is selected.</summary>
    public int SelectedWing { get; set; } = 1;

    /// <summary>The angle of the radar's sweep in radians.</summary>
    public float RadarSweep { get; private set; }

    /// <summary>Where the target sits in the sight, from -1 to 1 on each axis.</summary>
    public Vector2 TargetInSight { get; private set; }

    /// <summary>How many comms lines have arrived, with the fraction of the next one's wait.</summary>
    public float CommsPosition { get; private set; }

    public int Decoys { get; private set; } = DecoyLoad;

    public float[] Spectrum { get; } = new float[14];

    /// <summary>The traced signal at a moment in time, from 0 to 1.</summary>
    public static float Signal(float time)
        => MathUtil.Clamp(0.5f + 0.3f * MathF.Sin(time * 4f) + 0.15f * MathF.Sin(time * 13f) + 0.05f * MathF.Sin(time * 41f), 0f, 1f);

    /// <summary>The slower signal drawn behind it, from 0 to 1.</summary>
    public static float Reference(float time)
        => MathUtil.Clamp(0.45f + 0.28f * MathF.Sin(time * 1.7f + 1f) + 0.08f * MathF.Sin(time * 6f), 0f, 1f);

    /// <summary>Moves the ship to a moment in time.</summary>
    /// <param name="time">The ship's clock in seconds.</param>
    public void Advance(float time)
    {

        Flight.Advance(time);
        Systems.Advance(time, Flight.Speed);

        RadarSweep = -time * 1.4f;
        TargetInSight = new Vector2(0.55f + 0.25f * MathF.Sin(time * 0.5f), 0.5f + 0.3f * MathF.Sin(time * 0.33f + 1f));
        CommsPosition = time / CommsInterval;
        Decoys = DecoyLoad - (int)(time / 15f) % 20;

        for (var i = 0; i < Contacts.Length; i++)
        {
            Contacts[i].Advance(time, i);
        }

        for (var i = 0; i < Spectrum.Length; i++)
        {
            Spectrum[i] = MathUtil.Clamp(0.45f + 0.4f * MathF.Sin(time * (1.5f + i * 0.3f) + i) + 0.1f * MathF.Sin(time * 9f + i * 3f), 0.02f, 1f);
        }

        Wing[1].Value = 0.5f + 0.5f * MathF.Sin(time * 0.7f);
    }
}

/// <summary>Where the ship is going: the four figures the sight, the tapes and the compass show.</summary>
public sealed class FlightState
{
    public float Speed { get; private set; }

    public float Altitude { get; private set; }

    public float Heading { get; private set; }

    public float Pitch { get; private set; }

    internal void Advance(float time)
    {
        Speed = 150f + 90f * MathF.Sin(time * 0.35f) + 15f * MathF.Sin(time * 2.1f);
        Altitude = 1100f + 600f * MathF.Sin(time * 0.21f + 1f);
        Heading = 150f + 40f * MathF.Sin(time * 0.17f) + 6f * MathF.Sin(time * 0.9f);
        Pitch = 8f * MathF.Sin(time * 0.4f) + 3f * MathF.Sin(time * 1.3f);
    }
}

/// <summary>How the ship is doing: the six figures the gauges, the bars and the warning strip show.</summary>
public sealed class SystemsState
{
    public float Hull { get; private set; } = 0.87f;

    public float Shield { get; private set; }

    public float Fuel { get; private set; }

    public float Power { get; private set; }

    public float Temperature { get; private set; }

    public float Thrust { get; private set; }

    internal void Advance(float time, float speed)
    {
        // The shield drains for ten seconds, then recharges over the next ten
        var cycle = time % 20f;

        Shield = cycle < 10f ? 1f - cycle / 10f * 0.9f : 0.1f + (cycle - 10f) / 10f * 0.9f;
        Fuel = 0.9f - time % 240f / 240f * 0.8f;
        Power = 0.55f + 0.35f * MathF.Sin(time * 0.6f);
        Temperature = 0.5f + 0.4f * MathF.Sin(time * 0.25f + 2f);
        Thrust = MathUtil.Clamp(speed / 300f + 0.1f * MathF.Sin(time * 3f), 0f, 1f);
    }
}

/// <summary>
/// How the reactor's output is shared between weapons, shields and engines. The shares add up to
/// one. Focusing a system gives it the most, and the shares move there over a moment.
/// </summary>
public sealed class PowerState
{
    public const int Balanced = -1;

    private const float FocusedShare = 0.6f;

    public static IReadOnlyList<string> Names { get; } = ["WEP", "SHD", "ENG"];

    /// <summary>The share of each system, in the order of <see cref="Names"/>.</summary>
    public float[] Shares { get; } = [1f / 3f, 1f / 3f, 1f / 3f];

    /// <summary>The focused system, or <see cref="Balanced"/>.</summary>
    public int Focus { get; set; } = Balanced;

    /// <summary>Moves the shares towards the focus.</summary>
    /// <param name="elapsed">The real time since the last frame, in seconds.</param>
    public void Advance(float elapsed)
    {
        var blend = 1f - MathF.Exp(-5f * elapsed);

        for (var i = 0; i < Shares.Length; i++)
        {
            var target = Focus == Balanced ? 1f / 3f : i == Focus ? FocusedShare : (1f - FocusedShare) / 2f;

            Shares[i] += (target - Shares[i]) * blend;
        }
    }
}

/// <summary>A ship on the radar.</summary>
public sealed class Contact(string callsign, string type, bool hostile)
{
    public string Callsign { get; } = callsign;

    public string Type { get; } = type;

    public bool Hostile { get; } = hostile;

    /// <summary>The angle on the scope in radians, counter-clockwise from the right.</summary>
    public float Angle { get; private set; }

    /// <summary>The distance as a fraction of the radar's range.</summary>
    public float Distance { get; private set; }

    public float Hull { get; private set; }

    public float Shield { get; private set; }

    /// <summary>The speed the contact closes at, in metres per second. Negative when it opens.</summary>
    public float Closing { get; private set; }

    /// <summary>The bearing in degrees, clockwise from the top of the scope.</summary>
    public float Bearing => ((90f - MathUtil.RadiansToDegrees(Angle)) % 360f + 360f) % 360f;

    /// <summary>The range in kilometres.</summary>
    public float Range => Distance * ShipState.RadarRange;

    internal void Advance(float time, int index)
    {
        Angle = time * (0.08f + 0.04f * index) + index * 1.3f;
        Distance = 0.3f + 0.6f * (0.5f + 0.5f * MathF.Sin(time * 0.15f + index * 2f));

        // The derivative of the distance, in metres per second
        Closing = -0.6f * 0.5f * 0.15f * MathF.Cos(time * 0.15f + index * 2f) * ShipState.RadarRange * 1000f;

        Hull = 0.55f + 0.4f * MathF.Sin(time * 0.05f + index * 1.7f);
        Shield = 0.5f + 0.5f * MathF.Sin(time * 0.3f + index);
    }
}

/// <summary>A wingman and how much of its load is left.</summary>
public sealed class Wingman(string callsign, float value)
{
    public const int Capacity = 63;

    public string Callsign { get; } = callsign;

    public float Value { get; set; } = value;
}

/// <summary>A mode the pilot can switch on, if the ship has it.</summary>
public sealed class Mode(string name, bool available = true)
{
    public string Name { get; } = name;

    public bool Available { get; } = available;

    public bool Engaged { get; set; }
}

/// <summary>Who a comms line is from.</summary>
public enum CommsKind
{
    Friendly,
    System,
    Hostile,
}

/// <summary>A line in the comms log.</summary>
public sealed record CommsLine(string Speaker, string Text, CommsKind Kind = CommsKind.Friendly);