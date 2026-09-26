using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// World text billboarded, flat on the ground and fading with distance, and entity text anchored to
/// an entity and to the screen corners with a shadow and a background - the two text renderers,
/// the default font and the distance fade in one frame.
/// </summary>
internal sealed class TextScene : IGoldScene
{
    public void Start(Game game, Scene scene)
    {
        game.SetupBase3D();
        game.SetCameraPosition(new Vector3(0f, 4f, 12f));
        game.SetCameraRotation(new Vector3(0f, -15f, 0f));

        var ground = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Ground",
            Material = game.CreateMaterial(new Color(38, 41, 47), metalness: 0.04f, glossiness: 0.25f),
            Size = new Vector3(30f, 0.5f, 30f),
            Position = new Vector3(0f, -0.25f, 0f),
        });

        ground.Scene = scene;

        var cube = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Cube",
            Material = game.CreateMaterial(new Color(96, 103, 116), metalness: 0.1f, glossiness: 0.35f),
            Position = new Vector3(0f, 0.5f, 0f),
        });

        cube.Scene = scene;

        game.AddWorldTextRenderer();
        game.AddEntityTextRenderer();

        // World text: a billboard with a glow over the cube
        Place("World text", new Vector3(0f, 2.6f, 0f), Quaternion.Identity, new WorldTextComponent
        {
            Text = "WORLD TEXT",
            Height = 0.6f,
            TextColor = new Color(130, 205, 255),
            GlowColor = new Color(0, 140, 255, 170),
            GlowSize = 4f,
            Alignment = TextAlignment.Center,
            Billboard = true,
        });

        // Flat on the ground, read from the camera side
        Place("Flat text", new Vector3(0f, 0.02f, 3.5f), Quaternion.RotationX(-MathF.PI / 2f), new WorldTextComponent
        {
            Text = "FLAT ON THE GROUND",
            Height = 0.5f,
            TextColor = Color.Orange,
            Alignment = TextAlignment.Center,
            Billboard = false,
        });

        // Far enough to sit inside its fade: about nineteen units from the camera, fading from ten to twenty-four
        Place("Fading text", new Vector3(5f, 1.5f, -6f), Quaternion.Identity, new WorldTextComponent
        {
            Text = "FADING",
            Height = 0.6f,
            TextColor = Color.Cyan,
            Alignment = TextAlignment.Center,
            Billboard = true,
            FadeStartDistance = 10f,
            MaxDistance = 24f,
        });

        // Entity text: a label pinned to the cube, and two pinned to screen corners
        cube.Add(new EntityTextComponent
        {
            Text = "cube",
            FontSize = 18,
            Offset = new Vector2(0f, -30f),
            Anchor = TextAnchor.BottomCenter,
            EnableShadow = true,
            ShadowColor = new Color(0, 0, 0, 200),
        });

        cube.Add(new EntityTextComponent
        {
            Text = "SCREEN TEXT, TOP LEFT",
            FontSize = 20,
            PositionMode = TextPositionMode.Anchored,
            ScreenAnchor = DisplayPosition.TopLeft,
            Offset = new Vector2(16f, 16f),
            TextColor = Color.LightGreen,
        });

        cube.Add(new EntityTextComponent
        {
            Text = "with a background",
            FontSize = 20,
            PositionMode = TextPositionMode.Anchored,
            ScreenAnchor = DisplayPosition.BottomRight,
            Offset = new Vector2(16f, 16f),
            EnableBackground = true,
            BackgroundColor = new Color4(0f, 0f, 0f, 0.6f),
            Padding = new Vector2(8f, 4f),
        });

        void Place(string name, Vector3 position, Quaternion rotation, WorldTextComponent text)
        {
            var entity = new Entity(name) { text };

            entity.Transform.Position = position;
            entity.Transform.Rotation = rotation;
            entity.Scene = scene;
        }
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
    }
}