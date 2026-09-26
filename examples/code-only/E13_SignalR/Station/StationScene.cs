using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Images;
using Stride.Rendering.Lights;

namespace E13_SignalR.Station;

/// <summary>
/// The station itself, in orbit: black space and a starfield behind a deck with a lip on three
/// sides and one open edge, a bulkhead and two towers behind it, a crane arm from the right tower
/// holding the hatch. The ShapeBatch markings - hatch ring, landing pad, deck grid, corner ticks,
/// the warning line along the drop - take their colour from the console scheme, so a scheme change
/// shows in the world and not only on the boards.
/// </summary>
public sealed class StationScene(Game game)
{
    /// <summary>Half the deck's width and depth. The open edge is at +Z, facing the camera.</summary>
    public const float DeckHalf = 12f;

    public const float LipThickness = 0.4f;

    /// <summary>How far above the deck the markings sit, so they are not swallowed by its surface.</summary>
    public const float Lift = 0.03f;

    /// <summary>Where the camera starts. The boards face this point, so they are seen square-on from here.</summary>
    public static readonly Vector3 CameraPosition = new(0, 19, 24);

    /// <summary>The station board hangs over the back of the deck, in front of the bulkhead.</summary>
    public static readonly Vector3 BoardCenter = new(0, 6.6f, -11.5f);

    /// <summary>The feed stands off the left side of the deck.</summary>
    public static readonly Vector3 FeedCenter = new(-16.5f, 3.2f, 3f);

    private const float CameraPitch = -36f;
    private const float LipHeight = 1.2f;
    private const float PadRadius = 6f;
    private const float StarDistance = 380f;
    private const int StarCount = 420;

    private readonly Star[] _stars = MakeStars(StarCount, seed: 7);

    private ShapeBatch? _shapes;
    private Labels? _labels;
    private float _hatchPulse;

    /// <summary>The one batch everything in the scene draws with. Created by <see cref="Build"/>.</summary>
    public ShapeBatch? Shapes => _shapes;

    public void Build(Scene scene, Labels labels, StationConsole console)
    {
        _labels = labels;

        // Space: a near-black clear colour instead of a skybox, and a faint cool ambient so the
        // sides the sun does not reach are dim rather than missing. The exposure is pinned: the
        // default tone map adapts to the average brightness, and against a black sky that drives
        // the deck to white.
        game.AddGraphicsCompositor(clearColor: new Color(3, 5, 9))
            .AddCleanUIStage()
            .ConfigurePostEffects(fx =>
            {
                foreach (var toneMap in fx.ColorTransforms.Transforms.OfType<ToneMap>())
                {
                    toneMap.AutoExposure = false;
                    toneMap.Exposure = 1f;
                }
            });

        game.Add3DCamera();

        // The one line. Left mouse grabs, the wheel changes the carry distance, T + mouse turns the body.
        var grabber = new GrabberScript();
        game.GetCameraEntity().Add(grabber);

        // The sun from high on the camera's side, so shadows fall back towards the bulkhead and
        // stay short. The default lights from behind the station and lays the bulkhead's shadow
        // across half the deck.
        var sun = game.AddDirectionalLight(intensity: 2.2f);

        sun.Transform.Rotation = Quaternion.RotationX(MathUtil.DegreesToRadians(-58f)) * Quaternion.RotationY(MathUtil.DegreesToRadians(22f));

        AddAmbient(scene);

        game.Add3DCameraController();

        // Looking down at the pad from the open side, so lost cargo slides towards the viewer; a
        // little wider than the default so the hatch and the board both fit in the frame
        game.SetCameraPosition(CameraPosition);
        game.SetCameraRotation(new Vector3(0, CameraPitch, 0));
        game.GetCameraEntity().Get<CameraComponent>().VerticalFieldOfView = 60f;

        // Depth-tested, so a container sitting on the pad ring covers it like anything else would.
        // Added before the text renderers: the panels are drawn first, and the text sits on them.
        _shapes = game.AddShapeBatch(depthTest: true);
        game.AddWorldTextRenderer();
        game.AddEntityTextRenderer();

        var deckMaterial = game.CreateMaterial(new Color(34, 37, 44), specular: 0.05f, microSurface: 0.25f);
        var lipMaterial = game.CreateMaterial(new Color(58, 62, 72), specular: 0.08f, microSurface: 0.3f);
        var hullMaterial = game.CreateMaterial(new Color(74, 80, 94), specular: 0.1f, microSurface: 0.35f);

        AddStatic(scene, "Deck", deckMaterial, new Vector3(DeckHalf * 2f, 0.5f, DeckHalf * 2f), new Vector3(0, -0.25f, 0));

        // The lip: back, left and right. No lip at the front - that is where cargo is lost.
        AddStatic(scene, "Lip back", lipMaterial, new Vector3(DeckHalf * 2f, LipHeight, LipThickness), new Vector3(0, LipHeight / 2f, -DeckHalf + LipThickness / 2f));
        AddStatic(scene, "Lip left", lipMaterial, new Vector3(LipThickness, LipHeight, DeckHalf * 2f), new Vector3(-DeckHalf + LipThickness / 2f, LipHeight / 2f, 0));
        AddStatic(scene, "Lip right", lipMaterial, new Vector3(LipThickness, LipHeight, DeckHalf * 2f), new Vector3(DeckHalf - LipThickness / 2f, LipHeight / 2f, 0));

        // The wall the board hangs in front of, and the towers either side of it
        AddStatic(scene, "Bulkhead", hullMaterial, new Vector3(18f, 12f, 2f), new Vector3(0, 6f, -17.5f));
        AddStatic(scene, "Tower left", hullMaterial, new Vector3(3f, 9f, 3f), new Vector3(-10.5f, 4.5f, -17f));
        AddStatic(scene, "Tower right", hullMaterial, new Vector3(3f, 13.2f, 3f), new Vector3(10.5f, 6.6f, -17f));

        // The crane: a post up from the right tower, a rail along the right edge, an arm across to
        // the hatch and a hoist down to it. High enough that, from the camera, the arm passes
        // above the board rather than across its header, and low enough to stay in the frame.
        var armHeight = Deck.HatchHeight + 3.6f;

        AddStatic(scene, "Crane post", hullMaterial, new Vector3(0.6f, armHeight - 13.2f, 0.6f), new Vector3(10.5f, (armHeight + 13.2f) / 2f, -17f));
        AddStatic(scene, "Crane rail", hullMaterial, new Vector3(0.6f, 0.6f, 17.6f), new Vector3(10.5f, armHeight, -8.5f));
        AddStatic(scene, "Crane arm", hullMaterial, new Vector3(10.8f, 0.6f, 0.6f), new Vector3(5.4f, armHeight, 0));
        AddStatic(scene, "Hoist", hullMaterial, new Vector3(0.25f, armHeight - Deck.HatchHeight - 1.1f, 0.25f), new Vector3(0, (armHeight + Deck.HatchHeight + 0.8f) / 2f, 0));

        labels.Add("edge", 0.42f, labels.Bold, (t, c) => t.TextColor = Hex.WithAlpha(c.Accent, 210));
    }

    /// <summary>Lights the hatch ring for a moment. Called on every release.</summary>
    public void Pulse() => _hatchPulse = 1f;

    /// <summary>The stars and the markings, resubmitted every frame in the scheme's colours.</summary>
    public void Draw(StationConsole console, float deltaSeconds, float time)
    {
        if (_shapes is null || _labels is null) return;

        _hatchPulse = MathF.Max(0f, _hatchPulse - deltaSeconds * 2f);

        DrawStars(time);

        var accent = console.Accent;
        var glow = console.Glow;
        var dim = Hex.WithAlpha(accent, 90);

        _shapes.BorderWidth = 1.5f;
        _shapes.Fill.Set(null, 0f);

        // A faint grid, so the deck reads as a surface against the black
        var inset = DeckHalf - LipThickness - 0.3f;
        var grid = Hex.WithAlpha(accent, 22);

        for (var line = -10f; line <= 10f; line += 2f)
        {
            _shapes.DrawPixelLine(new Vector3(line, Lift, -inset), new Vector3(line, Lift, inset), 1f, grid);
            _shapes.DrawPixelLine(new Vector3(-inset, Lift, line), new Vector3(inset, Lift, line), 1f, grid);
        }

        // Landing pad: a ring, and a dashed ring outside it that turns. The pad is lit, so its glow
        // adds light to the deck rather than painting over it
        _shapes.Glow.Set(6f, Hex.WithAlpha(glow, 140));
        _shapes.Glow.Additive = true;
        _shapes.DrawRing(new Vector3(0, Lift, 0), Vector3.UnitY, PadRadius, accent);
        _shapes.Glow.Clear();

        _shapes.Dash.Set(10f, 8f, time * 20f);
        _shapes.DrawRing(new Vector3(0, Lift, 0), Vector3.UnitY, PadRadius + 1.2f, dim);
        _shapes.Dash.Clear();

        // Corner ticks on the deck: each a three-point stroke on the deck plane, one round join
        const float Tick = 1.5f;

        foreach (var (signX, signZ) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
        {
            var corner = new Vector2(signX * inset, signZ * inset);

            ReadOnlySpan<Vector2> tick = [corner - new Vector2(signX * Tick, 0), corner, corner - new Vector2(0, signZ * Tick)];

            _shapes.DrawPixelPolyline(tick, new Vector3(0, Lift, 0), Vector3.UnitX, Vector3.UnitZ, 2f, accent);
        }

        // The open edge, marked as the hazard it is - in the scheme's own colour, dashed and marching
        _shapes.Dash.Set(14f, 10f, -time * 40f);
        _shapes.DrawPixelLine(new Vector3(-inset, Lift, DeckHalf - 0.3f), new Vector3(inset, Lift, DeckHalf - 0.3f), 2.5f, accent);
        _shapes.Dash.Clear();

        // The hatch: a ring that brightens and widens for a moment when cargo drops through it
        var hatch = new Vector3(0, Deck.HatchHeight, 0);

        _shapes.Glow.Set(6f + _hatchPulse * 18f, Hex.WithAlpha(glow, (byte)(120 + _hatchPulse * 135)));
        _shapes.Glow.Additive = true;
        _shapes.DrawRing(hatch, Vector3.UnitY, 3f + _hatchPulse * 0.4f, accent);
        _shapes.Glow.Clear();

        _shapes.Dash.Set(6f, 6f, -time * 15f);
        _shapes.DrawRing(hatch, Vector3.UnitY, 3.6f, dim);
        _shapes.Dash.Clear();

        // A guide from the hatch to the pad, faint, so the drop reads as a path
        _shapes.Dash.Set(4f, 12f, time * 30f);
        _shapes.DrawPixelLine(hatch, new Vector3(0, Lift, 0), 1f, dim);
        _shapes.Dash.Clear();

        // The hazard, spelled out on the deck along the drop. The hatch gets no label: from the
        // camera it sits in front of the board's header, and the ring says enough.
        _labels.Set("edge", "OPEN EDGE · CARGO LOST BELOW", new Vector3(0, Lift + 0.03f, DeckHalf - 1.1f), Board.Orientation(Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitY));
    }

    /// <summary>
    /// Stars as tiny billboard discs far out, in the same batch as everything else. Each twinkles on
    /// its own phase; the batch is depth-tested, so the station simply covers them.
    /// </summary>
    private void DrawStars(float time)
    {
        _shapes!.BorderWidth = 0f;
        _shapes.Fill.Set(null, 1f);

        foreach (var star in _stars)
        {
            var twinkle = 0.6f + 0.4f * MathF.Sin(time * star.Speed + star.Phase);

            _shapes.DrawBillboardCircle(star.Direction * StarDistance, star.Size, Hex.WithAlpha(star.Tint, (byte)(star.Alpha * twinkle)));
        }

        _shapes.Fill.Set(null, 0f);
    }

    private static Star[] MakeStars(int count, int seed)
    {
        var random = new Random(seed);
        var stars = new Star[count];

        for (var i = 0; i < count; i++)
        {
            // Uniform over the sphere: a random direction, not random angles
            var z = random.NextSingle() * 2f - 1f;
            var angle = random.NextSingle() * MathF.Tau;
            var ring = MathF.Sqrt(1f - z * z);
            var direction = new Vector3(ring * MathF.Cos(angle), z, ring * MathF.Sin(angle));

            // Mostly white, a few warm and a few blue, like a real sky
            var tint = random.NextSingle() switch
            {
                < 0.12f => new Color(255, 214, 170),
                < 0.3f => new Color(180, 205, 255),
                _ => Color.White,
            };

            stars[i] = new Star(direction, 0.8f + random.NextSingle() * random.NextSingle() * 2.2f, tint,
                (byte)(120 + random.Next(136)), 0.4f + random.NextSingle() * 1.6f, random.NextSingle() * MathF.Tau);
        }

        return stars;
    }

    private static void AddAmbient(Scene scene)
    {
        var entity = new Entity("Ambient")
        {
            new LightComponent
            {
                Type = new LightAmbient { Color = new ColorRgbProvider(new Color(120, 140, 190)) },
                Intensity = 0.09f,
            },
        };

        entity.Scene = scene;
    }

    private void AddStatic(Scene scene, string name, Material material, Vector3 size, Vector3 position)
    {
        var entity = game.Create3DPrimitive(PrimitiveModelType.Cube, new Bepu3DPhysicsOptions
        {
            EntityName = name,
            Material = material,
            Size = size,
            Position = position,
            Component = new StaticComponent { Collider = new CompoundCollider() },
        });

        entity.Scene = scene;
    }

    private readonly record struct Star(Vector3 Direction, float Size, Color Tint, byte Alpha, float Speed, float Phase);
}