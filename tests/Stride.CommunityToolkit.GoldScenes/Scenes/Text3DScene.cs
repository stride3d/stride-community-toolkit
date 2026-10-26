using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// Solid lettering from the embedded font: the whole alphabet in both cases, the digits and the
/// common signs, so a change to any glyph's outline decode, hole classification, bridging or
/// extrusion shows in the picture; plus a centred block and a shape made by Primitive3D.Text for
/// the layout, the anchor and the shape route, and a bevelled word for the rounded edge. Lit from every side, as the setup's one light is
/// behind the lettering. The embedded font is what makes the pixels the same on every machine.
/// </summary>
internal sealed class Text3DScene : IGoldScene
{
    public SceneSetup Setup => GoldSetup.Perspective;

    public void Start(Game game, Scene scene)
    {
        game.SetCameraPosition(new Vector3(0f, 2.2f, 11.5f));
        game.SetCameraRotation(new Vector3(0f, -6f, 0f));

        // The setup's one light shines from behind the lettering and leaves the caps black; six lights
        // down the world axes light solids from every side, as the example does
        game.AddAllDirectionLighting(intensity: 20f, showLightGizmo: false);

        GoldSetup.AddGround(game, scene);

        var gold = game.CreateMaterial(new Color(240, 180, 60), metalness: 0.2f, glossiness: 0.5f);
        var silver = game.CreateMaterial(new Color(210, 215, 225), metalness: 0.1f, glossiness: 0.4f);
        var blue = game.CreateMaterial(new Color(120, 190, 255), metalness: 0f, glossiness: 0.4f);

        // Every glyph a change could break, as one block of five lines hanging from its top anchor
        Place("Alphabet", new Vector3(0f, 5.6f, -1f), gold,
            TextMesh.CreateMesh(game.GraphicsDevice, MeshFont.Default, "ABCDEFGHIJKLMNOPQRSTUVWXYZ\nabcdefghijklmnopqrstuvwxyz\n0123456789 .,;:!?\"'()[]{}\n@#$%&*+-=/<>~^|_\n€£§©®°±µ¶", new TextMeshOptions { Size = 0.62f, Depth = 0.14f, Alignment = TextAlignment.Center, LineSpacing = 0.95f, Anchor = TextAnchor.TopCenter }));

        // A word with holes and a descender, big enough to show the walls and the bevel, standing on the ground
        Place("Word", new Vector3(-2.2f, 0.5f, 1.5f), silver,
            TextMesh.CreateMesh(game.GraphicsDevice, MeshFont.Default, "Bog8@", new TextMeshOptions { Size = 1.1f, Depth = 0.35f, Anchor = TextAnchor.BottomCenter, Bevel = new TextBevel(size: 0.03f, thickness: 0.05f, segments: 4) }));

        // The shape route: a primitive with a material, centred on its entity and turned a little
        var shape = game.CreatePrimitive(Primitive3D.Text("Shape", size: 0.55f, depth: 0.2f), new PrimitiveEntityOptions
        {
            EntityName = "Shape",
            Material = blue,
            Position = new Vector3(2.6f, 0.4f, 2f),
        });
        shape.Transform.Rotation = Quaternion.RotationY(-0.5f);
        shape.Scene = scene;

        void Place(string name, Vector3 position, Material material, Mesh mesh)
        {
            var model = new Model { new MaterialInstance { Material = material }, mesh };

            model.BoundingBox = mesh.BoundingBox;
            model.BoundingSphere = mesh.BoundingSphere;

            var entity = new Entity(name) { new ModelComponent { Model = model } };

            entity.Transform.Position = position;
            entity.Scene = scene;
        }
    }
}