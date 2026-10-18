# MeshBuilder

@Stride.CommunityToolkit.Rendering.MeshBuilder builds a `MeshDraw` at runtime from a vertex layout you
declare and the vertices and indices you add. It allocates and lays out the buffers for you. This
page builds one triangle.

A vertex element is one value per vertex: a `Vector3` position, a `Color`, a `Half` and so on. Stride
reads it through a `VertexElement` with a semantic name, a semantic index, an offset and a size. For
a fixed layout, a vertex struct is simpler. Use `MeshBuilder` when the layout is decided at runtime.

## Create a builder

```csharp
using var meshBuilder = new MeshBuilder();
```

> [!NOTE]
> Dispose the builder. It rents its buffers from `ArrayPool<byte>.Shared`, and a builder that is not
> disposed does not return them.

## Layout

The `With...` methods declare the layout. They must be called before the first vertex is added.

### Primitive type

```csharp
meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);
```

### Indexing

`IndexingType` is `None`, `Int16` or `Int32`. An `Int16` index addresses vertices 0 to 32,767.

```csharp
meshBuilder.WithIndexType(IndexingType.Int16);
```

### Elements

Each `With...` method returns the element's index, which `SetElement` takes.

```csharp
var position = meshBuilder.WithPosition<Vector3>();
var color = meshBuilder.WithColor<Color>();
```

## Vertices

@Stride.CommunityToolkit.Rendering.MeshBuilder.AddVertex adds a vertex.
[SetElement](xref:Stride.CommunityToolkit.Rendering.MeshBuilder.SetElement``1(System.Int32,``0)) sets
an element of the last vertex added; the
[three-argument overload](xref:Stride.CommunityToolkit.Rendering.MeshBuilder.SetElement``1(System.Int32,System.Int32,``0))
takes a vertex index, so you can add every vertex first and set the values afterwards.

```csharp
meshBuilder.AddVertex();
meshBuilder.SetElement(position, new Vector3(0, 0, 0));
meshBuilder.SetElement(color, Color.Red);
```

The other two corners follow the same pattern.

```csharp
meshBuilder.AddVertex();
meshBuilder.SetElement(position, new Vector3(1, 0, 0));
meshBuilder.SetElement(color, Color.Green);

meshBuilder.AddVertex();
meshBuilder.SetElement(position, new Vector3(.5f, 1, 0));
meshBuilder.SetElement(color, Color.Blue);
```

## Indices

With indexing configured, `AddIndex` connects the vertices. With `IndexingType.None` the vertices are
drawn in the order they were added, three per triangle, and a vertex that two triangles share is
repeated.

Front faces in Stride wind **clockwise** as seen by the camera - the Direct3D convention
(`RasterizerStateDescription.DefaultFrontFaceCounterClockwise` is `false`) - so these indices order
the corners clockwise when viewed from the front. Wind a triangle the other way and it is not drawn
wrong, it is not drawn at all: back-face culling removes it, which shows up as geometry that is
invisible from one side or, on a closed shape, as looking *into* a hollow shell.

```csharp
meshBuilder.AddIndex(0);
meshBuilder.AddIndex(2);
meshBuilder.AddIndex(1);
```

## Mesh

`ToMeshDraw` builds the `MeshDraw`. It clears the builder afterwards unless `clear` is `false`.

```csharp
var meshDraw = meshBuilder.ToMeshDraw(GraphicsDevice);
```

### Display

Put the `MeshDraw` in a `Model` with a material that reads the vertex colour. Without
`ComputeVertexStreamColor` the triangle is white.

```
var model = new Model
{
    new MaterialInstance {
        Material = Material.New(graphicsDevice, new MaterialDescriptor {
            Attributes = new MaterialAttributes {
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Diffuse = new MaterialDiffuseMapFeature {
                    DiffuseMap = new ComputeVertexStreamColor()
                },
            }
        })
    },
    new Mesh {
        Draw = meshDraw,
        MaterialIndex = 0
    }
}
```


## Who owns the buffers

`ToMeshDraw` creates two GPU buffers - vertex and, when indexing is configured, index - and hands
them to you. **No content manager tracks them**, so if you rebuild a mesh (an animated shape rebuilt
per frame, say) you must dispose the previous draw's buffers yourself, or each rebuild leaks GPU
memory:

```csharp
foreach (var vertexBuffer in mesh.Draw.VertexBuffers)
    vertexBuffer.Buffer.Dispose();

mesh.Draw.IndexBuffer?.Buffer.Dispose();
```

The Procedural Geometry example shows this pattern on its animated circle.

## Extruded shapes and 3D letters

`AddExtrudedPolygon` turns any simple 2D outline - convex or concave - into a solid: front cap,
back cap and crisp flat-shaded side walls, triangulated internally by ear clipping
(`EarClipping.Triangulate`, which is public and usable on its own):

```csharp
var position = meshBuilder.WithPosition<Vector3>();
var normal = meshBuilder.WithNormal<Vector3>();

meshBuilder.AddExtrudedPolygon(outline, depth: 0.25f, position, normal);
```

Lettering from a real font, holes and all, is [3D Text](text-3d.md): `TextMesh` reads a font's
glyph outlines, triangulates them with `EarClipping.Triangulate(outer, holes)` and extrudes them the
same way.

## Custom pixel formats

Every `With*` method infers the GPU format from `T` - `Vector3` becomes `R32G32B32_Float`, `Color`
becomes `R8G8B8A8_UNorm`, and so on. For a type the inference does not know, or to store data in a
different format than the CLR type suggests, pass the format explicitly:

```csharp
var position = meshBuilder.WithPosition<Vector3>(pixelFormat: PixelFormat.R32G32B32A32_Float);
```

## See also

- [Procedural Geometry](../code-only/examples/procedural-geometry.md) example (`E07_3D_ProceduralGeometry`)
- [3D Text](text-3d.md)