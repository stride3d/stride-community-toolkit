# MeshBuilder

## Introduction

The `MeshBuilder` is a utility class allowing dynamic creation of meshes at runtime. Acting as a wrapper around the `Mesh` class, it provides a simpler API for defining the mesh layout and data.

## Once there was a triangle
Like in all rendering examples let's start with a simple triangle.

## `VertexElement`
A vertex element is a value assigned to a specific vertex.
In Stride we can use a lot of different types to define our data some of them are `Vector3`, `Vector4`, `Color`, `Int`, `Float`, `Half` and more.

We need to tell Stride how we want layout this data so our shader can read our various values.
This is where vertex elements come in. They define a `semanticName`, `semanticIndex`, `offset` and a `size`.

By default Stride allows you to define structs for your data or to use custom buffers for your data.
For most use-cases this is probably fine and the easier approach compared to a mesh builder.

However if you need to define your data dynamically or want a generalized method here we go.

## `MeshBuilder`
The @Stride.CommunityToolkit.Rendering.Utilities.MeshBuilder abstracts away a lot of complexity behind custom buffers, type erasure, memory allocation and instead
provides you with a simpler builder API to define your mesh dynamically.

It uses the same concepts as Stride so you still have to define your layout manually but the process is easier and you don't need to worry about the memory layout yourself.
To use a `MeshBuilder` just create a new instance.

```csharp
using var meshBuilder = new MeshBuilder()
```

> [!NOTE]
> Notice the `using` at the front. This part is crucial as you should always dispose of your mesh builder when it's no longer needed. The builder utilizes pooling behind the scenes, and failing to dispose of it prevents the return of internal buffers, which can lead to significant performance degradation.

## Layout
As we discussed earlier we need to tell the `MeshBuilder` which data types and fields we want to use.
For this we use the various `With...` methods.

### Primitive Type
First we need to select a primitive type in our example we create a bunch of triangles so we use this code:

```csharp
meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);
```

### Indexing
The mesh builder supports three types of indexing, `None`, `Int16` or `Int32`.
In our case we definitely don't need more than 32k indices so we can safely use the `Int16` version.

```csharp
meshBuilder.WithIndexType(IndexingType.Int16);
```
## Elements
For our example we will use a vertex with a `position` and a `color` element.

```csharp
var position = meshBuilder.WithPosition<Vector3>();
var color = meshBuilder.WithColor<Color>();
```

These methods return the element index we need when we set our actual values.
We use a `Vector3` as our vertex position and a `Color` for our vertex color.
Other types would work as well but these are very common so we will use them as well.

## Vertices
Next we define our vertices. For that we use a new method @Stride.CommunityToolkit.Rendering.Utilities.MeshBuilder.AddVertex.
This will add a new vertex to our builder and allows us to use the `Get/SetElement` methods.

You can also declare multiple vertices before setting the actual values but this is the simplest way for now.

```csharp
meshBuilder.AddVertex();
```

After this we can set our element data. For this we use the [SetElement()](xref:Stride.CommunityToolkit.Rendering.Utilities.MeshBuilder.SetElement``1(System.Int32,System.Int32,``0)) method.
It expects an element index (which we received from `WithPosition` and `WithColor` earlier) as well as your desired value.

```csharp
meshBuilder.SetElement(position, new Vector3(0, 0, 0));
meshBuilder.SetElement(color, Color.Red);
```

We repeat this for the other two triangle points as well.

```csharp
meshBuilder.AddVertex();
meshBuilder.SetElement(position, new Vector3(1, 0, 0));
meshBuilder.SetElement(color, Color.Green);

meshBuilder.AddVertex();
meshBuilder.SetElement(position, new Vector3(.5f, 1, 0));
meshBuilder.SetElement(color, Color.Blue);
```

## Indices
Next we need to tell the `MeshBuilder` how we want to connect the vertices.
We configured indexing for our builder so we need to do this explicitly.
For a simple example like this you could also completely skip the indexing part and use `IndexingType.None` instead.

The winding order in Stride is counter-clockwise so we use these indices.

```csharp
meshBuilder.AddIndex(0);
meshBuilder.AddIndex(2);
meshBuilder.AddIndex(1);
```

## Mesh
The only thing left is building the actual mesh. For this we use this method.
It expects a graphics device as an argument. If you call this from a script this is usually available for you using the `GraphicsDevice` property of your script.
```csharp
meshBuilder.ToMeshDraw(GraphicsDevice);
```

### Display
To show this `MeshDraw` on screen we create a `ModelComponent` and add our `MeshDraw` as new model.
We also need to define a material to use our vertex colors on screen. Else the Triangle would just be white.
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
        Draw = meshBuilder.ToMeshDraw(graphicsDevice),
        MaterialIndex = 0
    }
}
```

Congrats 🥳 you got a triangle.

## Example

For a more comprehensive usage of `MeshBuilder`, explore our [Procedural Geometry](../code-only/examples/procedural-geometry.md) example where you'll find detailed code on creating complex geometries dynamically.