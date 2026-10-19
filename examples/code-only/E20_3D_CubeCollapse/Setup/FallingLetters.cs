using CubeCollapse.Components;
using CubeCollapse.Shared;
using Stride.BepuPhysics;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

namespace CubeCollapse.Setup;

/// <summary>
/// Drops a word into the scene as solid 3D letters with physics, one tumbling body per letter.
/// </summary>
/// <remarks>
/// <para>
/// This is the decorative use of the toolkit's extruded letter meshes: real geometry that falls,
/// bounces off whatever is left of the platform and comes to rest among the cubes. Each letter is
/// its own entity with a <see cref="BodyComponent"/>, so the word breaks apart naturally as it lands.
/// </para>
/// <para>
/// The collider is a plain box around the glyph. A box is deliberate: a letter-shaped collider means
/// convex hulls, and extruded-polygon hulls colliding with each other are exactly the shape class
/// behind two Bepu failures seen in this repository (a NaN contact between extruded hulls, and a
/// hull pair that never separates). A pile of
/// letters jostling one another is the risky configuration, so they collide as boxes and nobody can
/// tell once they are lying in a heap.
/// </para>
/// </remarks>
public static class FallingLetters
{
    /// <summary>World units per EM of the letters, so a capital comes out about 0.9 tall.</summary>
    private const float Size = 1.3f;

    /// <summary>Depth of the letter bodies, in world units.</summary>
    private const float Depth = 0.3f;

    /// <summary>
    /// Spawns a word above a point, letters spread across it, each falling under gravity.
    /// </summary>
    /// <param name="game">The running game.</param>
    /// <param name="scene">The scene the letters are added to.</param>
    /// <param name="word">The word to drop, in the toolkit's embedded font.</param>
    /// <param name="centre">Where the middle of the word starts, in world units. Height included - drop from high for drama.</param>
    /// <param name="material">The material every letter shares.</param>
    /// <param name="seed">Seed for the small per-letter jitter, so a run is reproducible.</param>
    /// <param name="yaw">
    /// Rotation of the whole word about Y, in radians. Pass the direction toward the camera so the
    /// word spawns readable from wherever the player has orbited to - the letters face the camera at
    /// the moment they appear, and physics owns them from then on.
    /// </param>
    /// <remarks>
    /// Letters get a small random height offset and yaw so they land staggered and tumble
    /// individually instead of falling as a rigid signboard.
    /// </remarks>
    public static void SpawnWord(Game game, Scene scene, string word, Vector3 centre, Material material, float yaw = 0f, int seed = 7)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(word);
        ArgumentNullException.ThrowIfNull(material);

        var random = new Random(seed);

        // One mesh per glyph, each centred on itself and told where it sits in the word, so the box
        // collider below and the visible letter share a centre, which is also what makes the
        // tumbling look right. The word is laid out along its own X axis and then turned as one
        // piece, so the line of letters stays perpendicular to the viewer whichever way it faces
        var glyphs = TextMesh.CreateGlyphs(MeshFont.Default, word, new TextMeshOptions { Size = Size, Depth = Depth, LetterSpacing = 0.1f, Anchor = TextAnchor.MiddleCenter });
        var wordRotation = Quaternion.RotationY(yaw);
        var right = Vector3.Transform(Vector3.UnitX, wordRotation);

        foreach (var glyph in glyphs)
        {
            var entity = new Entity($"{EntityNames.LetterPrefix}{(char)glyph.Codepoint}")
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        new MaterialInstance { Material = material },
                        glyph.Mesh.ToMesh(game.GraphicsDevice)
                    }
                }
            };

            // SlowFallComponent rather than a plain BodyComponent: at full gravity the words drop
            // past the camera before anyone reads them. It falls under a fraction of gravity while
            // still accelerating, bouncing and tumbling like any rigid body.
            entity.Add(new SlowFallComponent
            {
                Collider = new CompoundCollider
                {
                    Colliders =
                    {
                        new BoxCollider
                        {
                            Size = BoxOf(glyph.Mesh),
                            Mass = 1f,
                        }
                    }
                }
            });

            entity.Transform.Position = centre
                + right * glyph.Position.X
                + new Vector3(0, glyph.Position.Y + (float)random.NextDouble() * 1.5f, 0);

            entity.Transform.Rotation = wordRotation * Quaternion.RotationY((float)(random.NextDouble() - 0.5) * 0.6f);

            entity.Scene = scene;
        }
    }

    /// <summary>
    /// Spawns one line of 3D menu text: a static letter mesh with no physics that keeps turning to
    /// face the camera, so it stays readable while the player orbits.
    /// </summary>
    /// <param name="game">The running game.</param>
    /// <param name="scene">The scene the line is added to.</param>
    /// <param name="text">The line to show, in the toolkit's embedded font.</param>
    /// <param name="position">Where the line's centre hovers, in world units.</param>
    /// <param name="material">The line's material.</param>
    /// <param name="scale">Letter height in world units. Defaults to 0.45.</param>
    /// <returns>The created entity, named <see cref="EntityNames.GameOverMenu"/> so it can be removed on restart.</returns>
    public static Entity SpawnMenuLine(Game game, Scene scene, string text, Vector3 position, Material material, float scale = 0.45f)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(material);

        var entity = new Entity(Shared.EntityNames.GameOverMenu)
        {
            new ModelComponent
            {
                Model = TextMesh.CreateModel(game.GraphicsDevice, material, MeshFont.Default, text, new TextMeshOptions { Size = Size, Depth = 0.12f, Anchor = TextAnchor.MiddleCenter })
            },
            new FaceCameraScript()
        };

        entity.Transform.Position = position;
        entity.Transform.Scale = new Vector3(scale);
        entity.Scene = scene;

        return entity;
    }

    /// <summary>
    /// Disposes the GPU buffers behind an entity's letter meshes and removes it from its scene.
    /// </summary>
    /// <param name="entity">An entity whose meshes this class created.</param>
    /// <remarks>
    /// Letter meshes are built per entity and tracked by no content manager, so a restart that only
    /// removed the entities would leak a buffer pair per letter, every game. Only for entities whose
    /// meshes were built here - shared primitive models must never be disposed like this.
    /// </remarks>
    public static void ReleaseAndRemove(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var model = entity.Get<ModelComponent>()?.Model;

        if (model is not null)
        {
            foreach (var mesh in model.Meshes)
            {
                foreach (var vertexBuffer in mesh.Draw.VertexBuffers)
                {
                    vertexBuffer.Buffer.Dispose();
                }

                mesh.Draw.IndexBuffer?.Buffer.Dispose();
            }
        }

        entity.Scene = null;
    }

    /// <summary>The extents of a glyph mesh, which is centred on its origin, for the box that collides in its place.</summary>

    private static Vector3 BoxOf(GeometricMeshData<VertexPositionNormalTexture> mesh)
    {
        var max = Vector3.Zero;

        foreach (var vertex in mesh.Vertices) max = Vector3.Max(max, new Vector3(MathF.Abs(vertex.Position.X), MathF.Abs(vertex.Position.Y), MathF.Abs(vertex.Position.Z)));

        return max * 2f;
    }
}