using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;

namespace Example.Common.Galleries;

/// <summary>
/// A ring of stations. Given an ordered registry of exhibits it places every station on a circle
/// facing the centre, builds the ground, the pillars each exhibit asked for, the numbered labels
/// with their dotted lines and the index board, flies the camera between them, and runs every
/// exhibit's update each frame. Nothing here is positioned by hand: add an exhibit to the registry
/// and the ring grows to fit.
/// </summary>
/// <typeparam name="TStation">The kind of station handed to the exhibits: <see cref="GalleryStation"/>, or an example's own with more on it.</typeparam>
/// <remarks>
/// The gallery draws its own furniture - pads, dotted lines, the board's frame - through two shape
/// batches of its own, so an example's batches and their per-draw state are never touched by it.
/// What an exhibit draws or animates is the exhibit's business, through <see cref="Prepare"/> and
/// the exhibit's update.
/// </remarks>
public sealed class Gallery<TStation> where TStation : GalleryStation, new()
{
    /// <summary>How high above the pad the label pin floats.</summary>
    private const float PinHeight = 5.25f;

    /// <summary>
    /// One line of the index board, in world units; the frame is the lines plus a margin. The list's
    /// size on screen is set by <see cref="BoardShare"/>, not by this or by the text's font size,
    /// which is only how finely it is rasterised: the board hangs at whatever distance makes it that
    /// share of the home view, so a taller line just means a bigger board at a greater distance.
    /// </summary>
    private const float LineHeight = 0.45f;

    /// <summary>
    /// The home view: how high above and how far outside the ring the camera sits, as fractions of
    /// the radius, and how far it looks down - the whole ring in view at once.
    /// </summary>
    private const float HomeHeight = 0.66f;
    private const float HomeDistance = 1.85f;
    private const float HomePitchDegrees = -17f;

    /// <summary>The flight home: longer than a hop between stations, facing the first station on the way - from there it is the flight out, reversed; from anywhere else the camera turns to it as it rises.</summary>
    private const float HomeFlightSeconds = 3.5f;

    /// <summary>The radius of the ring on the ground under every station.</summary>
    private const float PadRadius = 5.8f;

    /// <summary>The little ring where a leader line touches its exhibit, and the dot where it hangs its label, in pixels.</summary>
    private const float LeaderEndRadius = 4f;
    private const float LeaderWidth = 1f;

    /// <summary>The index board's width in world units; its height follows the registry.</summary>
    private const float BoardWidth = 10f;

    /// <summary>How much of the home view's height the board takes, and the margin it keeps from the view's top and left edges, in world units at its distance.</summary>
    private const float BoardShare = 0.48f;
    private const float BoardMargin = 0.6f;

    /// <summary>How far in from the frame the corner brackets sit and the list starts.</summary>
    private const float BoardInset = 0.3f;

    /// <summary>The field of view the board's placement assumes: the default camera's, at a 16:9 window.</summary>
    private const float ViewFovDegrees = 45f;
    private const float ViewAspect = 16f / 9f;

    private readonly Vector3 _boardCentre;
    private readonly Vector3 _boardRight;
    private readonly Vector3 _boardUp;
    private readonly SpriteFont? _labelFont;

    private readonly Game _game;
    private readonly Scene _scene;
    private readonly IReadOnlyList<Exhibit<TStation>> _exhibits;
    private readonly List<TStation> _stations = [];
    private readonly List<EntityTextComponent> _labels = [];
    private readonly Material _pillarMaterial;
    private readonly ShapeBatch _furniture;
    private readonly ShapeBatch _overlay;
    private readonly GalleryCamera _camera;
    private int _destination;
    private bool _atHome;
    private int _hovered = -1;

    /// <summary>
    /// Builds the ring: the ground, every station with its pillars and label, the index board,
    /// and runs each exhibit's setup. The camera is left where it is; call <see cref="GoHome"/> or
    /// <see cref="GoTo"/> to place it.
    /// </summary>
    /// <param name="game">The game, with its compositor and camera already set up.</param>
    /// <param name="scene">The scene the furniture and the exhibits' entities go into.</param>
    /// <param name="exhibits">The registry, in gallery order.</param>
    /// <param name="configure">Runs on each station after its frame is set and before its exhibit's setup: where an example puts its own things on the station.</param>
    /// <param name="options">Layout figures; the shape gallery's by default.</param>
    public Gallery(Game game, Scene scene, IReadOnlyList<Exhibit<TStation>> exhibits, Action<TStation>? configure = null, GalleryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(exhibits);

        _game = game;
        _scene = scene;
        _exhibits = exhibits;
        Options = options ?? GalleryOptions.Default;
        Radius = MathF.Max(Options.MinimumRadius, exhibits.Count * Options.Spacing / MathF.Tau);
        _camera = new GalleryCamera(game);
        // A regular face for the labels: Stride's built-in font is bold
        _labelFont = SystemFonts.LoadFirst(game.Services, SystemFonts.SansSerifCandidates, 20f);
        _pillarMaterial = game.CreateMaterial(Options.PillarColor ?? new Color(96, 103, 116), metalness: 0.1f, glossiness: 0.35f);

        // Depth-tested for the pads and the board, over everything for the dotted lines and pins -
        // and after the post effects, or the tone mapper would turn their white into the same grey
        // it makes of any unit colour in the scene, next to labels that are truly white
        _furniture = game.AddShapeBatch(depthTest: true);
        _overlay = game.AddShapeBatch(afterPostEffects: true);

        BuildGround();

        for (var i = 0; i < exhibits.Count; i++)
        {
            _stations.Add(BuildStation(i, exhibits.Count, exhibits[i], configure));
        }

        // The board hangs in the air where the home view has its top-left corner: in front of the
        // home camera, offset left and up in its frame, turned to face it. Steer the camera away
        // and it stays put; Home brings it back to the corner
        (_boardCentre, _boardRight, _boardUp) = PlaceBoard();

        BuildBoard();
    }

    /// <summary>The layout figures the ring was built with.</summary>
    public GalleryOptions Options { get; }

    /// <summary>The ring's radius, from the registry's length.</summary>
    public float Radius { get; }

    /// <summary>The stations, in registry order.</summary>
    public IReadOnlyList<TStation> Stations => _stations;

    /// <summary>What the labels say: the number, the number and the method, or everything.</summary>
    public int LabelDetail { get; set; }

    /// <summary>When set, only the station nearest the camera runs and shows its label - one exhibit at a time.</summary>
    public bool Solo { get; set; }

    /// <summary>The station nearest the camera, updated every frame; next and previous count from it. 0 when the registry is empty, when there is no station to index.</summary>
    public int Current { get; private set; }

    /// <summary>
    /// The station the visitor is at: the nearest one, or <c>null</c> at home, where the nearest station is
    /// not where the visitor is, and <c>null</c> when the registry is empty - the frame stands with its
    /// board and its ground either way.
    /// </summary>
    public TStation? CurrentStation => _stations.Count > 0 && !_atHome ? _stations[Current] : null;

    /// <summary>Whether the visitor is at the home spot, above and outside the ring: no station is current there.</summary>
    public bool AtHome => _atHome;

    /// <summary>The station whose pad is under the mouse, or -1: the pad fills, and a click flies there.</summary>
    public int Hovered => _hovered;

    /// <summary>Whether the camera is flying itself somewhere rather than being steered.</summary>
    public bool Flying => _camera.Flying;

    /// <summary>
    /// Where the next and previous keys count from: the station being flown to while a flight is
    /// running, so pressing next twice quickly skips two stations rather than re-aiming at the one
    /// the camera happens to be passing.
    /// </summary>
    public int Focus => Flying ? _destination : Current;

    /// <summary>
    /// Runs on every station before its exhibit's update each frame: where an example puts the
    /// frame's state on the station - which batch to draw through, the visitor's style - so that
    /// every exhibit starts from the same place.
    /// </summary>
    public Action<TStation>? Prepare { get; set; }

    /// <summary>Sends the visitor home: above and outside the ring, every station in view, the index board in the top-left corner. From there, next and previous both go to the first station.</summary>
    /// <param name="instant">Put the camera there at once, rather than flying it.</param>
    public void GoHome(bool instant = false)
    {
        _destination = Current;
        _atHome = true;
        _camera.FlyTo(HomePosition, HomeRotation, instant, HomeFlightSeconds, _stations.Count > 0 ? _stations[0].At(0f, 2f, 0f) : null);
    }

    private Vector3 HomePosition => new(0f, Radius * HomeHeight, Radius * HomeDistance);

    private static Quaternion HomeRotation => Quaternion.RotationYawPitchRoll(0f, MathUtil.DegreesToRadians(HomePitchDegrees), 0f);

    /// <summary>Next or previous: one station on from <see cref="Focus"/> - or, from home, the first station whichever way the visitor turns.</summary>
    /// <param name="direction">+1 for the next station, -1 for the previous.</param>
    public void Step(int direction) => GoTo(_atHome ? 0 : Focus + direction);

    /// <summary>Sends the visitor to a station: a little way towards the centre from its pad, looking at it.</summary>
    /// <param name="index">The station, counted from 0 and wrapped, so one past the last is the first.</param>
    /// <param name="instant">Put the camera there at once, rather than flying it.</param>
    public void GoTo(int index, bool instant = false)
    {
        if (_stations.Count == 0) return;

        _atHome = false;

        _destination = (index % _stations.Count + _stations.Count) % _stations.Count;

        var station = _stations[_destination];
        var eye = station.At(0f, 4.5f, 13f);
        var target = station.At(0f, 1.6f, -1f);
        _camera.FlyTo(eye, GalleryCamera.LookRotation(target - eye), instant);
    }

    /// <summary>
    /// Advances whatever is in flight, finds the station the visitor is nearest, runs every shown
    /// exhibit's update and draws the furniture. Immediate mode: this is the frame, called from
    /// the game's update.
    /// </summary>
    /// <param name="time">The frame's time.</param>
    public void Update(GameTime time)
    {
        _camera.Update((float)time.Elapsed.TotalSeconds);

        var seconds = (float)time.Total.TotalSeconds;
        var camera = _game.GetCameraEntity().Transform.Position;

        // Steered off the home spot - by more than a nudge, so a settled flight still counts as home -
        // and next and previous count from the nearest station again
        if (_atHome && !Flying && Vector3.DistanceSquared(camera, HomePosition) > 0.25f) _atHome = false;

        // A pad under the mouse lights up, and a click on it is a flight there
        _hovered = Flying ? -1 : PadUnderMouse();
        if (_hovered >= 0 && _game.Input.IsMouseButtonPressed(MouseButton.Left)) GoTo(_hovered);

        Current = NearestStation(camera);

        for (var i = 0; i < _stations.Count; i++)
        {
            var station = _stations[i];
            var current = i == Current && !_atHome;

            station.Seconds = seconds;
            station.IsCurrent = current;

            DrawPad(station, current, i == _hovered);

            if (Solo && !current) continue;

            Prepare?.Invoke(station);
            var update = _exhibits[i].Update;
            if (update is not null) Guarded(station, () => update(station));
        }

        DrawLabels();
        DrawBoardFrame();
    }

    /// <summary>The label texts follow the detail level; call it after changing <see cref="LabelDetail"/>.</summary>
    public void UpdateLabels()
    {
        for (var i = 0; i < _stations.Count; i++)
        {
            var exhibit = _stations[i].Exhibit;

            _labels[i].Text = LabelDetail switch
            {
                0 => $"{i + 1}",
                1 => $"{i + 1}  {exhibit.Method}",
                _ => $"{i + 1}  {exhibit.Method}\n{exhibit.Title}: {exhibit.Summary}",
            };
        }
    }

    private TStation BuildStation(int index, int count, Exhibit<TStation> exhibit, Action<TStation>? configure)
    {
        // Station 1 straight ahead of the starting camera, the rest clockwise as seen from above
        var angle = -MathF.PI * 0.5f + index * MathF.Tau / count;
        var (sin, cos) = MathF.SinCos(angle);
        var outward = new Vector3(cos, 0f, sin);
        var forward = -outward;
        var right = Vector3.Cross(Vector3.UnitY, forward);

        var station = new TStation
        {
            Number = index + 1,
            Exhibit = exhibit,
            Game = _game,
            Scene = _scene,
            Origin = outward * Radius,
            Right = right,
            Forward = forward,
        };

        // The standard pillars: one left and back, a taller one right and further back
        ReadOnlySpan<(Vector3 Local, float Height)> spots = [(new Vector3(-3.5f, 0f, -1.5f), 4.5f), (new Vector3(3.5f, 0f, -3.5f), 6f)];

        for (var i = 0; i < Math.Min(exhibit.Pillars, spots.Length); i++)
        {
            var (local, height) = spots[i];
            var basePoint = station.At(local);

            var pillar = _game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
            {
                EntityName = $"Station {station.Number} pillar {i + 1}",
                Material = _pillarMaterial,
                Size = new Vector3(1.8f, height, 1.8f),
                Position = basePoint + Vector3.UnitY * (height * 0.5f),
            });

            pillar.Scene = _scene;
            station.Pillars.Add(new Pillar(basePoint, height));
        }

        configure?.Invoke(station);
        Guarded(station, () => exhibit.Setup?.Invoke(station));

        // The label: screen-space text pinned to a point above the pad, so it reads at any distance
        // ToDo: Test this component as well
        //var label = new WorldTextComponent
        //{
        //    Text = $"{station.Number}",
        //    FontSize = 12,
        //    Font = _labelFont,
        //    TextColor = Color.White,
        //    Anchor = TextAnchor.MiddleLeft,
        //    AutoScale = true,
        //    Offset = new Vector3(1, 0f, 0f),
        //    Billboard = false
        //};
        var label = new EntityTextComponent
        {
            Text = $"{station.Number}",
            FontSize = 20,
            Font = _labelFont,
            TextColor = Color.White,
            Anchor = TextAnchor.MiddleLeft,
            Offset = new Vector2(10f, 0f),
            EnableBackground = true,
            BackgroundColor = new Color4(0.03f, 0.05f, 0.09f, 0.85f),
            EnableShadow = false,
        };

        var entity = new Entity($"Station {station.Number} label") { Transform = { Position = PinOf(station) } };

        entity.Add(label);
        entity.Scene = _scene;
        _labels.Add(label);

        return station;
    }

    /// <summary>
    /// Runs a station's own code with the ring standing by: an exhibit that throws - a feature the
    /// GPU or the engine refuses - leaves its pad empty with the message on the station, and the
    /// other stations stand. Once a station has failed its update is not run again. A gallery that
    /// rebuilds a station itself - a variation on V - runs the rebuild through here for the same reason.
    /// </summary>
    public static void Guarded(TStation station, Action action)
    {
        if (station.Error is not null) return;

        try
        {
            action();
        }
        catch (Exception exception)
        {
            station.Error = exception.Message;
            Console.Error.WriteLine($"Station {station.Number} ({station.Exhibit.Title}) failed: {exception}");
        }
    }

    private void BuildGround()
    {
        // Dark and matte, so exhibits read against it instead of fighting a specular hotspot
        var groundMaterial = _game.CreateMaterial(new Color(38, 41, 47), metalness: 0.04f, glossiness: 0.25f);

        // Room for the ring, and for the exhibits that run outwards
        var side = (Radius + Options.GroundMargin) * 2f;

        var ground = _game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Ground",
            Material = groundMaterial,
            Size = new Vector3(side, 0.5f, side),
            Position = new Vector3(0f, -0.25f, 0f),
        });

        ground.Scene = _scene;
    }

    /// <summary>
    /// The index board at the centre: a HUD panel listing every station, drawn each frame like any
    /// other shape, with the list as world text placed once.
    /// </summary>
    private void BuildBoard()
    {
        // An empty registry keeps its board, with nothing listed: the frame is the same whatever is on it
        var lines = _stations.Count > 0 ? string.Join('\n', _stations.Select(s => $"{s.Number,2}  {s.Exhibit.Title}")) : " ";

        var board = new WorldTextComponent
        {
            Text = lines,
            FontSize = 40,
            // The height is the whole block's, every line of it; hung from the top of the frame
            Height = _stations.Count * LineHeight,
            TextColor = new Color(130, 205, 255),
            GlowColor = new Color(0, 140, 255, 120),
            GlowSize = 3f,
            Anchor = TextAnchor.TopLeft,
            Alignment = TextAlignment.Left,
            Billboard = false,
        };

        // Hung from the top-left of the frame, inset like the corner brackets, a hair in front, in the board's own plane
        var towardsCamera = Vector3.Cross(_boardRight, _boardUp);
        var entity = new Entity("Index board")
        {
            Transform =
            {
                Position = _boardCentre - _boardRight * (BoardSize.X * 0.5f - BoardInset) + _boardUp * (BoardSize.Y * 0.5f - 0.5f) + towardsCamera * 0.01f,
                Rotation = HomeRotation,
            },
        };

        entity.Add(board);
        entity.Scene = _scene;
    }

    private Vector2 BoardSize => new(BoardWidth, MathF.Max(4f, _stations.Count * LineHeight + 1f));

    /// <summary>
    /// Where the board hangs: the home camera's frame, far enough in front that the board takes
    /// <see cref="BoardShare"/> of the view's height, then across to the view's top-left corner.
    /// </summary>
    private (Vector3 Centre, Vector3 Right, Vector3 Up) PlaceBoard()
    {
        var rotation = HomeRotation;
        var right = Vector3.Transform(Vector3.UnitX, rotation);
        var up = Vector3.Transform(Vector3.UnitY, rotation);
        var forward = Vector3.Transform(-Vector3.UnitZ, rotation);

        var halfTan = MathF.Tan(MathUtil.DegreesToRadians(ViewFovDegrees) * 0.5f);
        var distance = BoardSize.Y / (2f * halfTan * BoardShare);
        var viewHalf = new Vector2(distance * halfTan * ViewAspect, distance * halfTan);
        var half = BoardSize * 0.5f;

        var centre = HomePosition
            + forward * distance
            + right * (-viewHalf.X + BoardMargin + half.X)
            + up * (viewHalf.Y - BoardMargin - half.Y);

        return (centre, right, up);
    }

    private void DrawBoardFrame()
    {
        var shapes = _furniture;
        var hudBlue = new Color(110, 200, 255);

        shapes.Fill.Set(new Color(4, 14, 30), 0.8f);
        shapes.BorderWidth = 1.5f;
        shapes.Glow.Set(7f, new Color(0, 150, 255, 160));
        shapes.DrawRectangle(_boardCentre, _boardRight, _boardUp, BoardSize, hudBlue, cornerRadius: 0.35f);
        shapes.Glow.Clear();

        // The corner brackets, the HUD cliche
        var half = BoardSize * 0.5f;
        var topLeft = _boardCentre - _boardRight * (half.X - BoardInset) + _boardUp * (half.Y - BoardInset);
        var bottomRight = _boardCentre + _boardRight * (half.X - BoardInset) - _boardUp * (half.Y - BoardInset);

        shapes.DrawPixelLine(topLeft, topLeft + _boardRight * 0.8f, 1.5f, hudBlue);
        shapes.DrawPixelLine(topLeft, topLeft - _boardUp * 0.5f, 1.5f, hudBlue);
        shapes.DrawPixelLine(bottomRight, bottomRight - _boardRight * 0.8f, 1.5f, hudBlue);
        shapes.DrawPixelLine(bottomRight, bottomRight + _boardUp * 0.5f, 1.5f, hudBlue);

        shapes.Fill.Set(null, 0.45f);
        shapes.BorderWidth = 3f;
    }

    /// <summary>A faint ring under every station, so a pad reads as a place even when its exhibit is small.</summary>
    private void DrawPad(GalleryStation station, bool current, bool hovered)
    {
        var shapes = _furniture;
        var centre = station.Origin + Vector3.UnitY * 0.01f;

        // Drawn under the station as its tag, so the batch can say which pad the mouse is over
        shapes.Tag = station;

        if (hovered)
        {
            // Lit and filled: the whole disc is the button
            shapes.BorderWidth = 2f;
            shapes.Fill.Set(null, 0.3f);
            shapes.DrawDisc(centre, Vector3.UnitY, PadRadius, new Color(170, 220, 255));
            shapes.Fill.Set(null, 0.45f);
        }
        else
        {
            // A disc with a transparent fill paints the same ring, but picks as the whole disc: a ring
            // picks on its band alone, which would make the pad a button one pixel wide
            shapes.BorderWidth = current ? 2f : 1f;
            shapes.Opacity = current ? 0.9f : 0.35f;
            shapes.Fill.Set(null, 0f);
            shapes.DrawDisc(centre, Vector3.UnitY, PadRadius, current ? new Color(150, 210, 255) : new Color(110, 140, 170));
            shapes.Fill.Set(null, 0.45f);
            shapes.Opacity = 1f;
        }

        shapes.Tag = null;
        shapes.BorderWidth = 3f;
    }

    /// <summary>The station whose pad the mouse is over, as the batch drew it last frame, or -1.</summary>
    private int PadUnderMouse()
        => _furniture.TryPick(_game.Input.MousePosition, out var hit) && hit.Tag is TStation station ? _stations.IndexOf(station) : -1;

    /// <summary>
    /// The dotted line from each exhibit up to its pin, through the overlay batch so a pillar never
    /// hides it: a small ring where it touches the exhibit, a dot where the label hangs.
    /// </summary>
    private void DrawLabels()
    {
        var shapes = _overlay;

        shapes.BorderWidth = LeaderWidth;
        shapes.Dash.Set(2f, 5f);

        foreach (var station in _stations)
        {
            var visible = !Solo || station.Number - 1 == Current;

            _labels[station.Number - 1].IsVisible = visible;

            if (!visible) continue;

            var anchor = AnchorOf(station);
            var pin = PinOf(station);

            // The line starts at the ring's outer edge, not its centre: the ring is a pixel radius,
            // so the gap is that many pixels' worth of world at the anchor's distance, plus the stroke
            var direction = Vector3.Normalize(pin - anchor);
            var start = anchor + direction * ((LeaderEndRadius + LeaderWidth) * shapes.WorldPerPixel(anchor));

            shapes.DrawPixelLine(start, pin, LeaderWidth, Color.White);
        }

        shapes.Dash.Clear();

        foreach (var station in _stations)
        {
            if (Solo && station.Number - 1 != Current) continue;

            shapes.DrawPixelRing(AnchorOf(station), LeaderEndRadius, Color.White);
            shapes.DrawPixelDisc(PinOf(station), LeaderEndRadius, Color.White);
        }

        shapes.BorderWidth = 3f;
    }

    private static Vector3 AnchorOf(GalleryStation station) => station.At(station.Exhibit.Anchor ?? new Vector3(0f, 0.3f, 0f));

    private static Vector3 PinOf(GalleryStation station)
    {
        var anchor = station.Exhibit.Anchor ?? new Vector3(0f, 0.3f, 0f);

        return station.At(anchor.X + 1.2f, PinHeight, anchor.Z);
    }

    private int NearestStation(Vector3 position)
    {
        var nearest = 0;
        var best = float.MaxValue;

        for (var i = 0; i < _stations.Count; i++)
        {
            var distance = Vector3.DistanceSquared(position, _stations[i].Origin);

            if (distance < best)
            {
                best = distance;
                nearest = i;
            }
        }

        return nearest;
    }
}