using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace E02_3D_Material_Gallery;

// The inputs a slot can take beyond a texture: a shader class of your own, a feature of your own that
// adds shaders to a stage, a texture computed at runtime with no asset behind it, and what loading a
// texture file right takes.
public static class InputStations
{
    /// <summary>
    /// The extension point: a shader class that derives from <c>ComputeColor</c> and overrides
    /// <c>Compute()</c> sits in any material slot through <c>ComputeShaderClassColor</c>, referenced
    /// by name. The two classes here are in this example's <c>Effects</c> folder, twenty lines
    /// each, compiled by the asset compiler at build like any shader. V switches the class.
    /// </summary>
    public static void CustomNode(MaterialStation station)
    {
        station.Clear();

        var shader = station.Pick("GalleryChecker", "GalleryStripes") == 0 ? "GalleryChecker" : "GalleryStripes";

        station.PlaceTrio(station.Compile(Recipes.Mapped(new ComputeShaderClassColor { MixinReference = shader }, glossiness: new ComputeFloat(0.45f))));
    }

    /// <summary>
    /// A material feature of your own, the next step past a node: where a node fills one slot's value,
    /// a feature decides which shaders a stage runs. Two of the gallery's own - a wobble in the
    /// displacement slot, the vertex stage, and a dissolve in the transparency slot, the pixel stage,
    /// whose amount is driven every frame through a parameter. Each is a small class in
    /// <c>CustomFeatures.cs</c> and a shader in <c>Effects</c>. V switches them.
    /// </summary>
    public static void CustomFeature(MaterialStation station)
    {
        station.Clear();
        station.State = null;

        if (station.Pick("wobble", "dissolve") == 0)
        {
            // Smooth shapes: on a cube's split normals the faces would move apart
            var wobble = Recipes.Pbr(new Color(90, 170, 230), glossiness: 0.7f, metalness: 0f);

            wobble.Attributes.Displacement = new WobbleFeature();

            var material = station.Compile(wobble);

            station.Place(Primitive3D.Sphere(1f), material, MaterialStation.SphereSpot);
            station.Place(Primitive3D.Capsule(), material, new Vector3(0f, 1.1f, 0f));
            station.Place(Primitive3D.Teapot(2.4f), material, MaterialStation.TeapotSpot);

            return;
        }

        // The dissolve writes its edge into the emissive stream, so the material carries an emissive
        // feature - black, so it adds nothing but the edge - to shade it
        var dissolve = Recipes.Mapped(Recipes.Colour(station.Textures.Color("brick/brick_dif.png")), glossiness: new ComputeFloat(0.4f), emissive: new ComputeColor(Color.Black));

        dissolve.Attributes.Transparency = new DissolveFeature();

        var burning = station.Compile(dissolve);

        station.State = burning;
        station.PlaceTrio(burning);
    }

    /// <summary>The dissolve's amount, from untouched to gone and back, through the key its shader declares.</summary>
    public static void CustomFeatureUpdate(MaterialStation station)
    {
        if (station.State is not Material burning) return;

        var amount = 0.5f - 0.6f * MathF.Cos(station.Seconds * 0.7f);

        burning.Set(GalleryDissolveKeys.DissolveAmount, amount);
    }

    /// <summary>
    /// Textures made in C# a moment ago: a height map of ripples, and the normal map derived from
    /// it by finite differences - both a <c>Color[]</c> handed to <c>Texture.New2D</c>. The
    /// sphere wears the height map as its colour, the teapot the normal map as its surface, and
    /// the cube both. No file was involved; a texture is just pixels.
    /// </summary>
    public static void RuntimeTexturesStation(MaterialStation station)
    {
        station.Clear();

        var device = station.Game.GraphicsDevice;
        var heights = station.Textures.Generated("ripples", () => RuntimeTextures.Ripples(device));
        var normals = station.Textures.Generated("ripple-normals", () => RuntimeTextures.NormalFromHeight(device, RuntimeTextures.RipplePixels(), 256));

        var painted = station.Compile(Recipes.Mapped(Recipes.Colour(heights), glossiness: new ComputeFloat(0.5f)));
        var bumped = station.Compile(Recipes.Mapped(new ComputeColor(new Color(200, 170, 120)), glossiness: new ComputeFloat(0.7f), normal: Recipes.Colour(normals)));
        var both = station.Compile(Recipes.Mapped(Recipes.Colour(heights), glossiness: new ComputeFloat(0.7f), normal: Recipes.Colour(normals)));

        station.PlaceTrio(painted, both, bumped);
    }

    /// <summary>
    /// The same files loaded wrong on the left and right on the right: what the toolkit's
    /// <c>TextureLoader</c> does at runtime that Game Studio's pipeline does at import, and what each
    /// step is for. V cycles the five ways a runtime load goes wrong without anything failing: a colour
    /// read as data comes out pale, a normal map read as a colour leans every normal one way, a green
    /// channel the wrong way turns bumps into dents (Game Studio's default import does this to the
    /// pack's own maps), straight alpha fills the clear part with the colour the file kept under it,
    /// and no mipmaps make a receding floor crawl. Press M for the normal views on the normal-map pairs.
    /// </summary>
    public static void TextureLoading(MaterialStation station)
    {
        station.Clear();

        var loader = station.Textures.Loader;
        var albedo = loader.Color("brick/brick_dif.png");
        var gloss = Recipes.Scalar(loader.Data("brick/brick_gls.png"));

        switch (station.Pick("a colour loaded as data", "a normal map loaded as a colour", "the green channel the wrong way", "straight alpha", "no mipmaps"))
        {
            case 0:
                // Read as linear, the sRGB bytes skip the decode: every mid-tone comes out brighter
                Pair(station,
                    station.Compile(Recipes.Mapped(Recipes.Colour(loader.Data("brick/brick_dif.png")), glossiness: gloss)),
                    station.Compile(Recipes.Mapped(Recipes.Colour(albedo), glossiness: gloss)));
                break;

            case 1:
                // One call for every file: the normal map gamma-decoded, so flat 0.5 reads as 0.2 and every normal leans
                Pair(station,
                    station.Compile(Recipes.Mapped(Recipes.Colour(albedo), glossiness: gloss, normal: Recipes.Colour(loader.Color("brick/brick_nml.png")))),
                    station.Compile(Recipes.Mapped(Recipes.Colour(albedo), glossiness: gloss, normal: Recipes.Colour(loader.NormalMap("brick/brick_nml.png")))));
                break;

            case 2:
                // Inverted, as the importer's default would: the pack's maps are green-down already
                Pair(station,
                    station.Compile(Recipes.Mapped(Recipes.Colour(albedo), glossiness: gloss, normal: Recipes.Colour(loader.NormalMap("brick/brick_nml.png", invertY: true)))),
                    station.Compile(Recipes.Mapped(Recipes.Colour(albedo), glossiness: gloss, normal: Recipes.Colour(loader.NormalMap("brick/brick_nml.png")))));
                break;

            case 3:
                StraightAlpha(station);
                break;

            default:
                NoMipmaps(station, loader);
                break;
        }
    }

    private static void Pair(MaterialStation station, Material wrong, Material right)
    {
        station.Place(Primitive3D.Cube(1.8f), wrong, new Vector3(-1.6f, 0.9f, 0f));
        station.Place(Primitive3D.Cube(1.8f), right, new Vector3(1.6f, 0.9f, 0f));
    }

    /// <summary>
    /// A round decal in a PNG made a moment ago, the way paint programs export one: green where it is
    /// drawn, and white under the fully transparent part, since no one sees what is under alpha 0. The
    /// engine's blend state expects premultiplied colour, so loaded straight that white is added to
    /// whatever is behind; premultiplied, it is black and adds nothing.
    /// </summary>
    private static void StraightAlpha(MaterialStation station)
    {
        var device = station.Game.GraphicsDevice;
        var straight = station.Textures.Generated("decal-straight", () => LoadDecal(device, premultiply: false));
        var premultiplied = station.Textures.Generated("decal-premultiplied", () => LoadDecal(device, premultiply: true));

        Material Decal(Texture texture)
        {
            var descriptor = Recipes.Mapped(Recipes.Colour(texture), glossiness: new ComputeFloat(0.3f));

            descriptor.Attributes.Transparency = new MaterialTransparencyBlendFeature();

            return station.Compile(descriptor);
        }

        var behind = station.Compile(Recipes.Pbr(new Color(230, 120, 60), 0.5f, 0f));

        foreach (var (x, texture) in new[] { (-1.6f, straight), (1.6f, premultiplied) })
        {
            station.Place(Primitive3D.Sphere(0.7f), behind, new Vector3(x, 0.8f, -1.6f));
            station.Place(Primitive3D.Box(new Vector3(1.8f, 1.8f, 0.02f)), Decal(texture), new Vector3(x, 1f, 0f));
        }
    }

    private static Texture LoadDecal(GraphicsDevice device, bool premultiply)
    {
        const int size = 256;

        using var image = Image.New2D(size, size, 1, PixelFormat.R8G8B8A8_UNorm);
        var pixels = new Color[size * size];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f)) / (size / 2f);
                var coverage = Math.Clamp((0.8f - r) / 0.08f, 0f, 1f);

                // What sits under alpha 0 in a file is whatever the program left there; white is common
                pixels[y * size + x] = coverage > 0f ? new Color(60, 170, 70, (byte)(coverage * 255f)) : new Color(255, 255, 255, 0);
            }
        }

        image.PixelBuffer[0].SetPixels(pixels);

        using var png = new MemoryStream();

        image.Save(png, ImageFileType.Png);
        png.Position = 0;

        return TextureLoader.Load(device, png, new TextureLoadOptions(TextureRole.Color) { PremultiplyAlpha = premultiply });
    }

    /// <summary>
    /// Two brick floors running away from the camera, tiled twelve times: without mipmaps each screen pixel
    /// in the distance samples one texel of dozens, and the far half turns to noise that crawls as the
    /// camera moves; with them it fades to the average colour of the brick.
    /// </summary>
    private static void NoMipmaps(MaterialStation station, TextureLoader loader)
    {
        var single = loader.Load("brick/brick_dif.png", new TextureLoadOptions(TextureRole.Color) { GenerateMipmaps = false });
        var mipmapped = loader.Color("brick/brick_dif.png");

        station.Place(Primitive3D.Box(new Vector3(2.8f, 0.04f, 7f)), station.Compile(Recipes.Mapped(Recipes.Colour(single, 12f), glossiness: new ComputeFloat(0.3f))), new Vector3(-1.5f, 0.02f, -2f));
        station.Place(Primitive3D.Box(new Vector3(2.8f, 0.04f, 7f)), station.Compile(Recipes.Mapped(Recipes.Colour(mipmapped, 12f), glossiness: new ComputeFloat(0.3f))), new Vector3(1.5f, 0.02f, -2f));
    }
}