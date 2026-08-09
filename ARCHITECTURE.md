# Architecture notes

A running list of API-design observations for the Stride Community Toolkit: places where the shape of
the API, rather than a bug in it, is what trips people up.

This is a **backlog of observations, not a decision record.** Nothing here is agreed or scheduled.
Items get added when something is noticed in passing — usually while writing an example, which is
where API friction shows up first — and removed when they are resolved or rejected.

The toolkit is in Preview, so breaking changes are on the table where they buy a cleaner long-term
API. Each item below states the impact if changed.

> [!NOTE]
> Add to this file when you notice friction, even if you are not going to act on it. An observation
> written down is worth more than one rediscovered three months later.

---

## 1. `Size` means different things for different primitives

**Observation.** `Primitive3DEntityOptions.Size` is a single `Vector3?` whose interpretation changes
per primitive type. Box-like shapes read it as a **full extent**; round shapes read `X` as a
**radius**, which is a half extent.

```csharp
game.Create3DPrimitive(PrimitiveModelType.Cube,   new() { Size = new Vector3(1f) });   // 1 unit across
game.Create3DPrimitive(PrimitiveModelType.Sphere, new() { Size = new Vector3(1f) });   // 2 units across
```

**Impact.** Silent and visual-only. Passing a diameter to a sphere produces a model at twice the
intended size with no error. It is worst when a collider is supplied by hand, because the mesh and
collider then disagree and objects appear to pass through one another — encountered while writing
`Example15_Constraint_Rope`.

The generated mesh and generated collider *do* read the value identically, so the toolkit is
internally consistent. The trap is entirely in the caller's expectation.

**How other engines avoid it.** None of the major engines expose one polymorphic size; each names the
property after the convention it uses. Godot went as far as making a breaking rename for exactly this
reason.

| Engine | Box | Sphere | Capsule |
|---|---|---|---|
| Unity | `BoxCollider.size` (full) | `SphereCollider.radius` | `radius` + `height` |
| Godot 4 | `BoxShape3D.size` (full) | `SphereShape3D.radius` | `radius` + `height` |
| Unreal | `UBoxComponent::BoxExtent` (**half**) | `USphereComponent::SphereRadius` | `CapsuleRadius` + `CapsuleHalfHeight` |

Two things worth copying. First, **the name carries the semantics** — Unreal says `Extent` and
`HalfHeight` precisely because they are halves. Second, Godot 3 called the box field `extents` and
meant half-extents; Godot 4 renamed it to `size` meaning full extents, a deliberate breaking change
to remove this ambiguity. That is the same choice facing this API.

*(Engine comparisons above are from general knowledge of those APIs, not verified against their
sources in this repository.)*

**Options.**

1. **Document only** — done for now: the XML docs on `Size` list every primitive's convention.
   Zero impact, but the trap remains.
2. **Normalise to bounding-box semantics.** `Size` always means the axis-aligned box the shape fits
   in, so a sphere of `Size = (1,1,1)` is one unit across. Intuitive and matches Unity's unit
   primitives. Breaking: every call site passing a radius silently halves. Loud but mechanical.
3. **Per-primitive option types** — `SphereOptions { Radius }`, `CubeOptions { Size }`. Impossible to
   misread, but multiplies the options surface and complicates the generic
   `Create3DPrimitive(type, options)` entry point.
4. **Make the primitive itself a closed set that carries its own dimensions.** Replace the
   `PrimitiveModelType` enum plus separate `Size` with one argument per shape, each naming exactly
   the parameters that shape needs. A sphere then cannot be handed a `Vector3` at all, and the entry
   point stays single and generic.

   ```csharp
   public abstract record Primitive
   {
       private Primitive() { }                                 // closed: no cases from outside
       public sealed record Sphere(float Radius) : Primitive;
       public sealed record Cube(Vector3 Size) : Primitive;
       public sealed record Capsule(float Radius, float Length) : Primitive;
   }

   game.Create3DPrimitive(new Primitive.Sphere(0.5f), options);
   ```

   Note this fixes the ambiguity **structurally** rather than by documentation, and it subsumes
   item 7: the mesh and collider switches both match over the same closed set, so a shape added to
   one and not the other is caught at the switch rather than by a test.

   Works on C# 14 / net10.0 today. Breaking, and broad — `PrimitiveModelType` appears across nearly
   every example.

   **C# 15 union types would be the same design, stated more directly**, and would add compiler-
   enforced exhaustiveness so a missing case is a build error rather than a `_ => throw`:

   ```csharp
   public union Primitive(Sphere, Cube, Capsule);
   ```

   Not adoptable yet: unions need .NET 11 Preview 2 and `<LangVersion>preview</LangVersion>`, and are
   early preview. Requiring a preview language version of every consumer is a high price for a
   shipped package. The record hierarchy above is the same shape and upgrades to a union later
   without changing call sites much, so waiting for unions is not a reason to defer the decision.
   (Union syntax and requirements verified against the C# 15 union types announcement; the toolkit
   has not been built against .NET 11.)

---

## 2. `IncludeCollider = false` leaves a half-configured body

**Observation.** Setting `Bepu3DPhysicsOptions.IncludeCollider = false` still attaches a
`BodyComponent`, holding a `CompoundCollider` with no shapes. That never attaches to the simulation,
so the entity ends up with a physics component that does nothing.

**Impact.** Two different intentions collide on one flag: "no physics at all" and "physics, but I
will supply the collider myself". The first is served by the non-physics `Create3DPrimitive`
overload; the second works but leaves an inert component if the caller forgets to add shapes.

**Options.** Rename to something intent-revealing (`SuppliesOwnCollider`); or validate and warn when
an attached body has no shapes; or leave as-is and rely on the documented gotcha.

---

## 3. Two `Create3DPrimitive` overloads separated only by their options type

**Observation.** There is a Bepu overload taking `Bepu3DPhysicsOptions` and a plain one taking
`Primitive3DEntityOptions`. With both namespaces imported, a bare `new()` cannot choose between them
and the call fails with `CS0121`.

**Impact.** A confusing compiler error for a common call shape, resolved only by naming the options
type explicitly. Overload resolution is carrying meaning that the method name could carry instead.

**Options.** Distinct names for the physics-creating helper; or a required explicit options argument.
Both are breaking, both are mechanical.

---

## 4. Mass is reachable only by abandoning the generated collider

**Observation.** Mass lives on the collider shape (`ColliderBase.Mass`), not on the body or on the
options. To set it, a caller must switch off `IncludeCollider` and build the whole
`CompoundCollider` by hand — which re-exposes item 1, since the hand-built collider must match the
mesh convention.

**Impact.** Setting one common property costs the entire convenience of the helper.
`Example15_Constraint_Rope` does this, and it is the longest part of `RopeBuilder`.

**Options.** Surface `Mass` (or `Density`) on `Bepu3DPhysicsOptions` and apply it to the generated
shapes. Additive, not breaking.

---

## 5. Fluent return values are inconsistent

**Observation.** The contributor guidance asks extensions to return the modified instance where
natural, but several do not — `SetupBase3DScene` returns `void`, while `AddSkybox` returns an entity.

**Impact.** Small, but it makes chaining unpredictable, so callers stop trying.

**Options.** Return `Game` from the scene-setup helpers. Additive and non-breaking, since a discarded
return value compiles unchanged.

---

## 6. `DebugTextPrinter` invites index-based mutation

**Observation.** The type exposes an `Instructions` list plus `Print()`, which encourages writing
`Instructions[2] = ...` each frame to update a line in place. There is also
`Print(IReadOnlyCollection<TextElement>)`, which replaces the whole list.

**Impact.** The index form requires startup placeholders and per-frame writes to agree on line
ordering, and they drift apart the moment a line is renamed or reordered — a live label ends up
describing the wrong key. Hit while writing `Example15_Constraint_Motors`.

**Options.** Document the collection overload as the preferred usage for anything with live values;
or make `Instructions` init-only so the collection overload is the only route for dynamic text.

---

## 7. No test asserts that generated meshes and colliders agree

**Observation.** For every `PrimitiveModelType`, the procedural model and the Bepu collider derive
their dimensions from the same `Size` in two separate switch statements
(`Procedural3DModelBuilder` and `EntityExtensions`). Nothing enforces that they stay in step.

**Impact.** They currently agree. A future primitive added to one switch but not the other, or with a
different convention, would produce a mesh that does not match its collider — a defect that is
invisible until something falls through the world.

**Options.** A test that, for each primitive type and a fixed `Size`, asserts the model bounds and the
collider bounds match. Cheap, and it pins down the convention that item 1 documents.

Superseded if item 1 option 4 is taken: matching both switches over a closed set of primitive records
moves this from a test to a compile-time check.
