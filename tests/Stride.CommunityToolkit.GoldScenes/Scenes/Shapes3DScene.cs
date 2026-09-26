using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// ShapeBatch in 3D: planes on the ground and upright, billboards, pixel-radius markers, thick and
/// pixel lines, a wire box, a space stroke that has to hide behind a pillar, and an overlay batch
/// that must not - the perspective term, the depth test and the per-fragment depth in one frame.
/// </summary>
internal sealed class Shapes3DScene : IGoldScene
{
    private static readonly Vector3 PillarA = new(-3f, 0f, -2f);
    private static readonly Vector3 PillarB = new(4f, 0f, -4f);

    private ShapeBatch? _scene;
    private ShapeBatch? _overlay;

    public void Start(Game game, Scene scene)
    {
        game.SetupBase3D();
        game.SetCameraPosition(new Vector3(0f, 9f, 18f));
        game.SetCameraRotation(new Vector3(0f, -22f, 0f));

        var groundMaterial = game.CreateMaterial(new Color(38, 41, 47), metalness: 0.04f, glossiness: 0.25f);
        var pillarMaterial = game.CreateMaterial(new Color(96, 103, 116), metalness: 0.1f, glossiness: 0.35f);

        var ground = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Ground",
            Material = groundMaterial,
            Size = new Vector3(40f, 0.5f, 40f),
            Position = new Vector3(0f, -0.25f, 0f),
        });

        ground.Scene = scene;

        Pillar("Pillar A", PillarA, 3f);
        Pillar("Pillar B", PillarB, 4f);

        _scene = game.AddShapeBatch(depthTest: true);
        _overlay = game.AddShapeBatch(depthTest: false);

        void Pillar(string name, Vector3 at, float height)
        {
            var pillar = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
            {
                EntityName = name,
                Material = pillarMaterial,
                Size = new Vector3(1.6f, height, 1.6f),
                Position = new Vector3(at.X, height * 0.5f, at.Z),
            });

            pillar.Scene = scene;
        }
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
        if (_scene is not { } shapes || _overlay is not { } overlay) return;

        var seconds = (float)time.Total.TotalSeconds;

        shapes.BorderWidth = 3f;
        shapes.Fill.Alpha = 0.45f;

        // On the ground: a disc the pillar cuts into, a ring, a field-of-view sector
        shapes.DrawDisc(new Vector3(PillarA.X, 0.02f, PillarA.Z + 1.2f), Vector3.UnitY, 2.4f, Color.OrangeRed);
        shapes.DrawRing(new Vector3(PillarB.X, 0.02f, PillarB.Z), Vector3.UnitY, 1.8f, Color.Cyan);
        shapes.DrawSector(new Vector3(0f, 0.02f, 2.5f), Vector3.UnitY, 4f, MathF.PI * 1.25f, 0.9f, Color.Yellow);

        // Upright: a HUD panel facing the camera with corner brackets in pixel lines
        var panel = new Vector3(0f, 2.6f, 1f);

        shapes.Fill.Set(new Color(4, 14, 30), 0.8f);
        shapes.Glow.Set(7f, new Color(0, 150, 255, 160));
        shapes.BorderWidth = 1.5f;
        shapes.DrawRectangle(panel, Vector3.UnitX, Vector3.UnitY, new Vector2(4.5f, 2.4f), new Color(110, 200, 255), cornerRadius: 0.3f);
        shapes.Glow.Clear();
        shapes.Fill.Set(null, 0.45f);

        var topLeft = panel + new Vector3(-2f, 1f, 0.01f);
        var bottomRight = panel + new Vector3(2f, -1f, 0.01f);

        shapes.DrawPixelLine(topLeft, topLeft + Vector3.UnitX * 0.6f, 1.5f, Color.White);
        shapes.DrawPixelLine(topLeft, topLeft - Vector3.UnitY * 0.4f, 1.5f, Color.White);
        shapes.DrawPixelLine(bottomRight, bottomRight - Vector3.UnitX * 0.6f, 1.5f, Color.White);
        shapes.DrawPixelLine(bottomRight, bottomRight + Vector3.UnitY * 0.4f, 1.5f, Color.White);
        shapes.BorderWidth = 3f;

        // Billboards over the pillars: a world-radius disc and pixel-radius markers beside it
        var overA = new Vector3(PillarA.X, 4.2f, PillarA.Z);
        var overB = new Vector3(PillarB.X, 5.2f, PillarB.Z);

        shapes.Fill.Color = Color.LimeGreen;
        shapes.DrawBillboardCircle(overA, 0.45f, Color.White);
        shapes.Fill.Color = null;

        shapes.Glow.Set(8f, new Color(255, 140, 0, 160));
        shapes.DrawPixelDisc(overB - Vector3.UnitX * 1.2f, 6f, Color.Orange);
        shapes.DrawPixelRing(overB + Vector3.UnitX * 1.2f, 10f, Color.Orange);
        shapes.DrawPixelLine(overB - Vector3.UnitX * 1.2f, overB + Vector3.UnitX * 1.2f, 1.5f, new Color(255, 140, 0, 120));
        shapes.Glow.Clear();

        // Lines: a thick world-width line tapering away, a pixel line beside it, a wire box on the pillar
        shapes.DrawLine(new Vector3(-7f, 0.5f, 6f), new Vector3(7f, 3.5f, -8f), 0.15f, Color.Gold);
        shapes.DrawPixelLine(new Vector3(-7f, 0.5f, 7f), new Vector3(7f, 3.5f, -7f), 2f, Color.White);
        shapes.DrawWireBox(new Vector3(PillarB.X, 2f, PillarB.Z), new Vector3(2.4f, 4.8f, 2.4f), 0.06f, Color.Yellow);

        // A space stroke: a helix around pillar A that the pillar has to hide per fragment
        Span<Vector3> helix = stackalloc Vector3[48];

        for (var i = 0; i < helix.Length; i++)
        {
            var t = (float)i / (helix.Length - 1);
            var angle = t * MathF.Tau * 2f + seconds;

            helix[i] = new Vector3(PillarA.X + MathF.Cos(angle) * 1.5f, 0.4f + t * 3.2f, PillarA.Z + MathF.Sin(angle) * 1.5f);
        }

        shapes.Glow.Set(8f, new Color(255, 120, 40, 140));
        shapes.DrawPixelPolyline(helix, 3f, Color.Orange);
        shapes.Glow.Clear();

        // A world-width space stroke arching over the scene, thin where it is far
        Span<Vector3> arch = stackalloc Vector3[24];

        for (var i = 0; i < arch.Length; i++)
        {
            var t = (float)i / (arch.Length - 1);

            arch[i] = new Vector3(-8f + 16f * t, 1f + MathF.Sin(t * MathF.PI) * 5f, -10f + 6f * t);
        }

        shapes.DrawPolyline(arch, 0.12f, Color.DeepSkyBlue);

        // A neon ring with a wide glow, and a dashed ring turning on the ground
        shapes.Glow.Set(24f);
        shapes.DrawRing(new Vector3(-6f, 3f, -3f), Vector3.UnitZ, 1.2f, Color.Magenta);
        shapes.Glow.Clear();

        shapes.Dash.Set(8f, 6f, seconds * 25f);
        shapes.DrawRing(new Vector3(0f, 0.02f, -6f), Vector3.UnitY, 3f, Color.Orange);
        shapes.Dash.Clear();

        // Soft depth fade: a ring standing a hand in front of pillar B's face dims over the face and
        // stays solid where the ground is far behind it; a billboard disc sunk in the floor melts
        // into it over a longer fade, instead of the hard slice the depth test gives the helix
        shapes.DepthFade = 0.6f;
        shapes.DrawRing(new Vector3(PillarB.X, 2f, PillarB.Z + 1f), Vector3.UnitZ, 1.5f, Color.LightGreen);
        shapes.DepthFade = 1.5f;
        shapes.DrawBillboardCircle(new Vector3(-6f, 0.5f, 3f), 0.8f, Color.Gold);
        shapes.DepthFade = 0f;

        // The overlay batch: a ring drawn through pillar B, which must stay whole
        overlay.BorderWidth = 3f;
        overlay.DrawRing(new Vector3(PillarB.X, 1.2f, PillarB.Z), Vector3.UnitZ, 1.4f, Color.HotPink);
    }
}