# Release Notes

Welcome to the Release Notes for the **Stride Community Toolkit**. This section aims to provide you with an organized, high-level summary of changes, enhancements, and fixes made in each version release. If you're looking to understand what has changed from one version to the next, you're in the right place.

## What to Expect

The Stride Community Toolkit is developed with rapid iteration in mind. It moves at a faster development pace compared to the Stride Game Engine. As a result, you should expect frequent updates that may introduce breaking changes. This fast-paced approach allows us to incorporate community feedback quickly and continue improving the toolkit.

## 1.0.0.0-preview.67

## What's Changed

<!-- If needed add more categories -->

### 💥 Breaking Changes

- One package to start: `Stride.CommunityToolkit` brings in `Stride.AssetCompiler` (build-only) and holds `WindowsDpiManager`.
  - `<PackageReference Include="Stride.CommunityToolkit.Windows" />` → `<PackageReference Include="Stride.CommunityToolkit" />`, or any package that depends on it, such as `Stride.CommunityToolkit.Bepu`. `Stride.CommunityToolkit.Windows` ships once more as an empty package that depends on the core, so a project still naming it keeps building; it will be unlisted. `Stride.CommunityToolkit.Linux` is unchanged.
  - `using Stride.CommunityToolkit.Windows;` → `using Stride.CommunityToolkit.Engine;` for `WindowsDpiManager`.
- Shapes are records that carry their dimensions: `Primitive3D` and `Primitive2D` in `Stride.CommunityToolkit.Rendering.ProceduralModels`, created through static factories whose arguments are named for what they are. The `PrimitiveModelType` and `Primitive2DModelType` enums and the `Size` and `Vertices` members of every options class are gone, and the three `Create3DPrimitive`/`Create2DPrimitive` pairs are one `CreatePrimitive` overloaded on the shape.
  - `game.Create3DPrimitive(PrimitiveModelType.Sphere)` → `game.CreatePrimitive(Primitive3D.Sphere())`; the same for `Create2DPrimitive` and `Primitive2D`.
  - `game.Create3DPrimitive(PrimitiveModelType.Sphere, new() { Size = new Vector3(2, 0, 0) })` → `game.CreatePrimitive(Primitive3D.Sphere(2))`. The dimensions are the factory's arguments: `Sphere(radius)`, `Capsule(radius, length)`, `Cylinder(radius, height)`, `Cone(radius, height)`, `Torus(radius, thickness)`, `Plane(width, length)`, `Teapot(size)`, `TriangularPrism(size)`; each round shape also takes a `tessellation`.
  - `PrimitiveModelType.Cube` with `Size = new Vector3(2)` → `Primitive3D.Cube(2)`; with `Size = new Vector3(1, 2, 3)` → `Primitive3D.Box(new Vector3(1, 2, 3))`. `PrimitiveModelType.RectangularPrism` → `Primitive3D.Box(size)`.
  - `PrimitiveModelType.InfinitePlane` → gone; `Add3DGround` and Bullet's `AddInfinite3DGround` build their plane themselves.
  - `Primitive2DModelType.Circle` with `Size = new Vector2(0.5f)` → `Primitive2D.Circle(0.5f)`; likewise `Rectangle(width, height)`, `Square(size)`, `Capsule(radius, height)` and `Triangle(width, height)`.
  - `Primitive2DModelType.Polygon` with `Size = new Vector2(radius, sides)` → `Primitive2D.RegularPolygon(radius, sides)`; with `Vertices = points` → `Primitive2D.Polygon(points)`.
  - `entity.AddBepu3DPhysics(PrimitiveModelType.Cube, new() { Size = size })` → `entity.AddBepu3DPhysics(Primitive3D.Box(size))`; the same for `AddBepu2DPhysics`, `AddBullet3DPhysics` and `AddBullet2DPhysics`.
  - `game.Add3DGround(new() { Size = new Vector3(40, 1, 40) })` → `game.Add3DGround(new Vector2(40))`: `Add2DGround`, `Add3DGround` and `AddInfinite3DGround` take a `Vector2? size` first and the options second.
  - `ShapeFixtureBuilder.AttachShape(Primitive2DModelType.Circle, new Vector2(r, r), body)` → `ShapeFixtureBuilder.AttachShape(Primitive2D.Circle(r), body)`.
  - Bepu colliders: `ConeCollider.Create(Vector3? size)` → `ConeCollider.Create(radius, height)`; `TorusCollider.Create(size)` → `TorusCollider.Create(radius, thickness)`; `TeapotCollider.Create(float? size)` → `TeapotCollider.Create(size)`; `TriangularPrismCollider.Create(Vector3? size)` → `TriangularPrismCollider.Create(size)`; `PolygonCollider.Create(vertices, radius, sides, depth)` → `PolygonCollider.Create(vertices, depth)` or `PolygonCollider.Create(radius, sides, depth)`. Each also takes its `*Shape` record.
  - Telling shapes apart: `type == PrimitiveModelType.Sphere` → `shape is Primitive3D.SphereShape sphere`, which also gives `sphere.Radius`; `type.ToString()` for a label → `shape.Name`, which says `Cube` for a box with three equal extents and `Square` for a rectangle with equal sides.
### 🎉 New Features

- `Primitive3D` and `Primitive2D`, closed sets of shape records with factories; the mesh builder and every physics package switch over the same set, so a shape missed by one is a compile error, not a mesh that does not match its collider. The round shapes take a `tessellation`, which the enums could not express.
- `EntityExtensions.ColliderFor(Primitive3D)` and `ColliderFor(Primitive2D, depth)` in the Bepu and Bullet packages: the collider fitted to a shape on its own, for a body built by hand.

### 🐞 Bug Fixes

### 🎨 Rendering

### ✨ Enhancement

### 📄 Docs

- Create Project, Create Project - Engine Only, Game Extensions, Physics Extensions, Bepu transform ownership, Entity Text, Engine patterns, the Glossary and the ShapeBatch HUD tutorial use `CreatePrimitive` and the shape factories; the example pages are regenerated.
- Create Project: the command-line steps are Windows and Linux tabs. The Linux tab adds the `linux-x64` runtime identifier, the first-build workaround for the asset compiler's signature check (`DOTNET_NUGET_SIGNATURE_VERIFICATION=false`), and the route that builds for Linux from Windows with `StridePlatform`.
### 🎓 Examples

- Every example uses the shape factories; a size that was a `Size` option is now the factory's argument, so `Primitive3D.Cylinder(0.3f, 2.5f)` reads as a radius and a height where `new Vector3(0.3f, 0, 2.5f)` did not.
- `E06_Box2D`: the keys spawn the factory's named models (`Square`, `Rectangle`, `Circle`, `LargeCircle`, `Triangle`, `Capsule`) instead of looking a kind of shape up; a square and a rectangle are the same kind.
- `E09_3D_PostEffects`: film grain at `Amount = 0.6f` and `GrainSize = 2f` (was 0.35 and 1), so the G key shows in a screenshot.
### 🔧 Engineering

### 💪 Other Changes