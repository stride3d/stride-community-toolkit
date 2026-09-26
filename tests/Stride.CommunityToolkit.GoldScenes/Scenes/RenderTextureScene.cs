using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Effects.PostProcessing;
using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// Four cameras drawing into textures shown on monitors: an overhead orthographic map, a fixed
/// corner camera, and the same corner through the night-vision and thermal colour transforms.
/// Pins AddRenderTextureCamera - the slot, the second forward renderer over the shared stages,
/// the HDR texture tone-mapped by the main view - and both shaders of the Effects package.
/// The ball's orbit and the night-vision grain follow the fixed timestep, so the frame is exact.
/// </summary>
internal sealed class RenderTextureScene : IGoldScene
{
    private Entity? _ball;

    public void Start(Game game, Scene scene)
    {
        // The main camera looks at the row of monitors over the scene, so the capture holds both
        game.SetupBase3D();
        game.SetCameraPosition(new Vector3(0f, 6f, 17f));
        game.SetCameraRotation(new Vector3(0f, -12f, 0f));

        // Something for the feeds to look at: a floor, three pillars of different heights and
        // colours, and a bright emissive ball on an orbit - the thing the thermal feed calls hot
        var ground = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
        {
            EntityName = "Ground",
            Material = game.CreateMaterial(new Color(52, 56, 64), metalness: 0.05f, glossiness: 0.3f),
            Size = new Vector3(40f, 0.5f, 40f),
            Position = new Vector3(0f, -0.25f, 0f),
        });

        ground.Scene = scene;

        ReadOnlySpan<(Vector3 At, float Height, Color Colour)> pillars =
        [
            (new Vector3(-6f, 0f, -4f), 3f, Color.OrangeRed),
            (new Vector3(6f, 0f, -4f), 4.2f, Color.DodgerBlue),
            (new Vector3(0f, 0f, 4f), 2.4f, Color.MediumSeaGreen),
        ];

        foreach (var (at, height, colour) in pillars)
        {
            var pillar = game.Create3DPrimitive(PrimitiveModelType.Cube, new Primitive3DEntityOptions
            {
                EntityName = "Pillar",
                Material = game.CreateMaterial(colour, metalness: 0.2f, glossiness: 0.5f),
                Size = new Vector3(1.4f, height, 1.4f),
                Position = at + Vector3.UnitY * (height * 0.5f),
            });

            pillar.Scene = scene;
        }

        _ball = game.Create3DPrimitive(PrimitiveModelType.Sphere, new Primitive3DEntityOptions
        {
            EntityName = "Ball",
            Material = Material.New(game.GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(new Color(255, 200, 120))),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor(new Color(255, 200, 120))) { Intensity = new ComputeFloat(6f) },
                },
            }),
            Size = new Vector3(0.6f),
            Position = new Vector3(4f, 1f, 0f),
        });

        _ball.Scene = scene;

        // The feeds: straight down over the middle, then one corner camera three times over - plain,
        // through the night-vision transform, through the thermal one
        var map = new Entity("Map camera") { new CameraComponent { Projection = CameraProjectionMode.Orthographic, OrthographicSize = 20f } };

        map.Transform.Position = new Vector3(0f, 30f, 0f);
        map.Transform.Rotation = Quaternion.RotationYawPitchRoll(0f, -MathF.PI * 0.5f, 0f);
        map.Scene = scene;

        ReadOnlySpan<(string Name, RenderTextureCamera Feed)> feeds =
        [
            ("Map", game.AddRenderTextureCamera(map, 256, 256)),
            ("Corner", game.AddRenderTextureCamera(Corner(scene, "Corner"), 256, 256)),
            ("Night vision", game.AddRenderTextureCamera(Corner(scene, "Night vision"), 256, 256, postEffects: fx => fx.ColorTransforms.Transforms.Add(new NightVision()))),
            ("Thermal", game.AddRenderTextureCamera(Corner(scene, "Thermal"), 256, 256, postEffects: fx => fx.ColorTransforms.Transforms.Add(new Thermal()))),
        ];

        // One monitor per feed in a row: a plane stood up by a quarter turn, its emissive map the
        // feed's texture, so the picture is shown and not lit again
        for (var i = 0; i < feeds.Length; i++)
        {
            var monitor = game.Create3DPrimitive(PrimitiveModelType.Plane, new Primitive3DEntityOptions
            {
                EntityName = $"Monitor {feeds[i].Name}",
                Material = Material.New(game.GraphicsDevice, new MaterialDescriptor
                {
                    Attributes =
                    {
                        Emissive = new MaterialEmissiveMapFeature(new ComputeTextureColor(feeds[i].Feed.Texture)
                        {
                            AddressModeU = TextureAddressMode.Clamp,
                            AddressModeV = TextureAddressMode.Clamp,
                        })
                        {
                            Intensity = new ComputeFloat(1f),
                        },
                    },
                }),
                Size = new Vector3(3.6f, 0f, 3.6f),
                Position = new Vector3((i - 1.5f) * 4.2f, 4f, -9f),
            });

            monitor.Transform.Rotation = Quaternion.RotationX(MathF.PI * 0.5f);
            monitor.Scene = scene;
        }

        static Entity Corner(Scene scene, string name)
        {
            var eye = new Vector3(12f, 7f, 12f);
            var direction = Vector3.Normalize(new Vector3(0f, 1f, 0f) - eye);
            var camera = new Entity(name);

            camera.Transform.Position = eye;
            camera.Transform.Rotation = Quaternion.RotationYawPitchRoll(MathF.Atan2(-direction.X, -direction.Z), MathF.Asin(direction.Y), 0f);
            camera.Scene = scene;

            return camera;
        }
    }

    public void Update(Game game, Scene scene, GameTime time)
    {
        if (_ball is null) return;

        var seconds = (float)time.Total.TotalSeconds;
        var angle = seconds * 0.7f;

        _ball.Transform.Position = new Vector3(MathF.Cos(angle) * 4f, 1f + MathF.Sin(seconds * 2.3f) * 0.4f, MathF.Sin(angle) * 4f);
    }
}