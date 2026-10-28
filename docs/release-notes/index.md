# Release Notes

Welcome to the Release Notes for the **Stride Community Toolkit**. This section aims to provide you with an organized, high-level summary of changes, enhancements, and fixes made in each version release. If you're looking to understand what has changed from one version to the next, you're in the right place.

## What to Expect

The Stride Community Toolkit is developed with rapid iteration in mind. It moves at a faster development pace compared to the Stride Game Engine. As a result, you should expect frequent updates that may introduce breaking changes. This fast-paced approach allows us to incorporate community feedback quickly and continue improving the toolkit.

## 1.0.0.0-preview.69

### 💥 Breaking Changes

#### The Debug names go

Three families were named for debugging and are used by games: the on-screen text panel, the key-driven list, and the renderer that labels entities. Their names now say what they do. Namespaces are unchanged.

| Before | After | Note |
|---|---|---|
| `DebugOverlay`, `DebugOverlaySection`, `DebugOverlayFontFamily`, `DebugOverlayFontResolver` | `TextOverlay`, `TextOverlaySection`, `TextOverlayFontFamily`, `TextOverlayFontResolver` | `DebugOverlay.GetOrCreate(game)` becomes `TextOverlay.GetOrCreate(game)`; every member keeps its name. |
| `SceneSetup.DebugOverlay` | `SceneSetup.TextOverlay` | The setup flag that creates the overlay before the first frame. |
| `DebugTextDropdown`, `DebugTextDropdownItem` | `KeyMenu`, `KeyMenuItem` | A key picks an item; nothing drops down. Members unchanged. |
| `EntityDebugSceneRenderer`, `EntityDebugSceneRendererOptions`, `EntityDebugRendererExtensions` | `EntityInfoRenderer`, `EntityInfoRendererOptions`, `EntityInfoRendererExtensions` | It labels entities with their name and position. |
| `game.AddEntityDebugSceneRenderer(options)`, `compositor.AddEntityDebugRenderer(options)` | `game.AddEntityInfoRenderer(options)`, `compositor.AddEntityInfoRenderer(options)` | One name on both receivers. |
| `rendering/debug-overlay.md` | `rendering/text-overlay.md` | The manual page; the old address is gone. |

Unchanged, because they are debugging aids: `AddPhysicsDebug`, the collider gizmos, the `DebugShapes` package, `DebugRenderComponentScript`, and Stride's own `DebugTextSystem`.

#### Three namespaces for text

`Stride.CommunityToolkit.Rendering.Text` held three subjects: text drawn with a font, lettering built as meshes, and the overlay. Each now has a namespace and a folder of its own, so one `using` no longer brings in the lot.

| Before | After | Note |
|---|---|---|
| `using Stride.CommunityToolkit.Rendering.Text;` for `EntityTextComponent`, `WorldTextComponent`, their renderers, `TextElement`, `TextPositionMode`, `SystemFonts` | unchanged | Text drawn with a font stays. |
| `using Stride.CommunityToolkit.Rendering.Text;` for `MeshFont`, `TextMesh`, `TextMeshOptions`, `TextBevel`, `GlyphMesh`, `GlyphOutline`, `GlyphContour`, `GlyphShape`, `GlyphPolygon`, `OutlineSegment`, `OutlineSegmentKind` | `using Stride.CommunityToolkit.Rendering.Text.Meshes;` | Lettering as meshes. `Primitive3D.Text` and `TextCollider` stay where they were. |
| `using Stride.CommunityToolkit.Rendering.Text;` for `TextOverlay`, `TextOverlaySection`, `TextOverlayFontFamily`, `TextOverlayFontResolver`, `KeyMenu`, `KeyMenuItem`, `EntityInfoRenderer`, `EntityInfoRendererOptions`, `AddEntityInfoRenderer()` | `using Stride.CommunityToolkit.Rendering.Overlay;` | The overlay and the entity labels. `TextElement`, which sections return, stays in `Rendering.Text`, so an overlay section usually needs both. |

The compiler names every call site that needs a new `using`; nothing else changes.

#### One body mode on the physics options

Four option types with three overlapping knobs (`IncludePhysics`, `Component`, `Mass`, and `Depth` on the 2D pair) are two, and the kind of physics a primitive gets is one property.

| Before | After | Note |
|---|---|---|
| `Primitive3DEntityOptions`, `Primitive2DEntityOptions`, `PrimitiveEntityOptionsBase` | `PrimitiveEntityOptions` | One core type for both dimensions. |
| `Bepu3DPhysicsOptions`, `Bepu2DPhysicsOptions` | `BepuPhysicsOptions` | One Bepu type; `Component`, `Mass` and the new `Interpolation` on it. |
| `Bullet3DPhysicsOptions`, `Bullet2DPhysicsOptions` | `BulletPhysicsOptions` | One Bullet type; `PhysicsComponent` has no default any more, the mode makes one. |
| `IncludePhysics = false` | `Body = BodyMode.None` | `Body` is `Dynamic` by default; `Static`, `Kinematic` and `None` are the other modes. |
| `Component = new StaticComponent { Collider = new CompoundCollider() }` | `Body = BodyMode.Static` | The same for Bullet's `StaticColliderComponent`. A supplied component still wins over the mode. |
| `Component = new BodyComponent { Kinematic = true, Collider = new CompoundCollider() }` | `Body = BodyMode.Kinematic` | |
| `Component = new BodyComponent { Collider = new CompoundCollider(), InterpolationMode = InterpolationMode.Interpolated }` | nothing | A body the options make is interpolated by default; `Interpolation = false` turns it off. A supplied component keeps its own setting. |
| `new Bepu2DPhysicsOptions { Depth = 4 }` | `Primitive2D.RegularPolygon(1, 5, depth: 4)` | The collider depth is on the 2D shape; every factory takes it last. |
| `PhysicsSetupBase.AddStatic(...)` | `AddBody(...)` with `Body = BodyMode.Static` | The ground helpers set the mode; a package implements `AddBody` for both shape kinds only. |
| the ground helpers wrote the default name and material into the options they were given | `options.WithBody(mode)` | The grounds build from a copy; the options you pass come back as they were. `WithBody` is public, a shallow copy of any options type. |

A supplied component together with `Body = BodyMode.None` throws, since the two contradict each other. `Mass` with `Static` is ignored with a line in the log, since a static has none.

#### One entity search family

`EntitySearchExtensions` had every search twice: one overload for any `EntityComponent`, one for `ActivableEntityComponent` with `includeDisabled`. A call with no argument on a component that can be disabled bound to the first and found disabled ones; a call with `false` bound to the second and skipped them. There is one overload now, for any `EntityComponent`, and one rule.

| Before | After | Note |
|---|---|---|
| `GetComponentInChildren<T>()`, `GetComponentsInDescendants<T>()` and the other seven, each with a second overload taking `bool includeDisabled` | One overload each, `where T : EntityComponent`, with `bool includeDisabled = false` | Source-compatible. A disabled `ActivableEntityComponent` is skipped unless `includeDisabled: true`; a component that cannot be disabled is always found. |
| `entity.GetComponentInChildren<CameraComponent>()` found a disabled camera | skips it | Pass `includeDisabled: true` for the old result. `Scene.GetCamera`, `GetComponentsInScene` and `GetComponentInScene` still include disabled components, as their remarks say. |

The rest of the Engine folder lost a fifth of its lines without changing what it does: the timer and event helpers on `ScriptSystem` share one core per family, `InstantiateSingle` is `Instantiate` plus the count check, the three ways of getting a camera's pick ray share one implementation, and the by-value twin of every `ref` overload documents itself by `inheritdoc`. `AddTask(action, delay, repeatEvery)` now rejects a zero or negative `repeatEvery`, as `AddAction` always did.

#### Camera controllers

The three camera scripts share one shift check and one aspect-ratio helper, `GraphicsDevice.GetAspectRatio()`, and lost their duplicated branches. Two of those branches behaved differently from their twins, so the fix is visible:

| Before | After | Note |
|---|---|---|
| `Basic2DCameraController` with `FollowTarget` and `FollowSmoothing = 0` put the camera at the target's Z | keeps the camera's own Z, as the smoothed follow always did | A 2D camera that jumped onto its target's plane saw nothing. |
| `MouseDragButton = MouseButton.Extended1` (or `Extended2`) never panned | pans | The button check is `IsMouseButtonDown(MouseDragButton)` instead of a switch over three buttons. |

#### Random helpers

One family of helpers on the caller's own `Random`, so a world built from `new Random(seed)` comes out the same for the same seed.

| Before | After | Note |
|---|---|---|
| `VectorHelper.RandomVector3(xRange: [-4, 4], yRange: [4, 8], zRange: [-4, 4])` | `random.NextVector3(new(-4, 4, -4), new(4, 8, 4))` | `VectorHelper` is gone, with its process-wide static `Random` and `SeedRandom`. `NextVector3(float min, float max)` covers the one-range form, and `NextSingle(min, max)` is new beside them. |
| `random.NextColor()` built the colour from a random unit direction | a hue spread evenly round the wheel, saturation and brightness from `ColorTone.Vivid` | The old code clamped negative components to zero, which happened to give vivid colours; the default keeps that look with an even spread of hues. `NextColor(ColorTone.Light)`, `Dark` and `Any` draw from other ranges. |

#### Triangular prism lighting

| Before | After | Note |
|---|---|---|
| `Primitive3D.TriangularPrism` lit its two slanted faces from the inside | the normals point outwards | The face normal was the cross product of the edges in the wrong order. Found by a new geometry test; the bottom and the two triangles were right all along. |

#### Gizmos

| Before | After | Note |
|---|---|---|
| `new TranslationGizmo(device)` had a constructor of its own | binds to `TranslationGizmo(device, xColor, yColor, zColor)`, whose colours default | Source-compatible; the gizmo also lost five private lists that were filled and never read, and builds each mesh once rather than once per axis. |

#### One anchor enum

`ScreenAnchor`, `TextAnchor` and the texture canvas's `Anchor` named the same nine points of a rectangle three times, two of them with identical members. One `Anchor` enum in `Stride.CommunityToolkit.Rendering` replaces all three.

| Before | After | Note |
|---|---|---|
| `ScreenAnchor.TopLeft` (the overlay, anchored text, the shape batch, the orientation gizmo, the camera controller's help) | `Anchor.TopLeft` | The same nine members; replace the type name, and add `using Stride.CommunityToolkit.Rendering;` where only `.Rendering.Text` was imported. |
| `TextAnchor.MiddleCenter` (the text components, mesh text, the entity info renderer) | `Anchor.MiddleCenter` | The same nine members; replace the type name and the using as above. |
| `TextureCanvas` anchors `Anchor.Top`, `Left`, `Center`, `Right`, `Bottom` | `Anchor.TopCenter`, `MiddleLeft`, `MiddleCenter`, `MiddleRight`, `BottomCenter` | The corner names are unchanged, and the numeric values are the same, so an `(Anchor)int` cast still means the same point. |
| `ScreenAnchorExtensions.At` | `AnchorExtensions.At` | The same method. |
| `EntityTextComponent.ScreenAnchor` and `.Anchor` | unchanged property names, both typed `Anchor` | Which point of the screen the text is pinned to, and which point of the text sits there. |
| `shapeBatch.Anchor(Anchor.BottomLeft)` | `shapeBatch.AnchorPoint(Anchor.BottomLeft)` | The shape batch's method that answers where an anchor is; its name no longer repeats the enum's. |

#### Core helpers: names and contracts

The review of the core package renamed the helpers whose names said how they worked or abbreviated what they read, and fixed the contracts that misled.

| Before | After | Note |
|---|---|---|
| `script.GetGCCamera()`, `GetGCCamera(name)`, `GetFirstGCCamera()` | `script.GetCamera()`, `GetCamera(name)`, `GetFirstCamera()` | They read the graphics compositor's camera slots, which `GC` abbreviated. `GetFirstCamera()` returns `null` for a compositor with no camera slot instead of throwing. |
| `camera.CalculateRayPlaneIntersectionPoint(screenPosition)` | `camera.ScreenToWorldPointOnXYPlane(screenPosition)` | The same method: the world point where the screen ray meets the XY plane. Every screen-to-world helper now starts `ScreenToWorld`. |
| `camera.GetPickRay(screenPosition)` | `camera.ScreenToWorldRay(screenPosition)` | The same ray. |
| `camera.CalculateRayFromScreenPosition(screenPosition)` | `[Obsolete]`; use `camera.ScreenToWorldRaySegment(screenPosition)` | The tuple was the segment's `Start` and `End`. Removed in a later release. |
| `modelComponent.GetMeshHWL()` returned `(height, width, length)` | `modelComponent.GetModelSize()` returns the bounding box's extent as `(X, Y, Z)` | `.X` was the height and `.Y` the width; now each component is its own axis. |
| `entity.GetComponent<T>()`, `GetComponents<T>()`, `TryGetComponent<T>(out)` for any `T` | `where T : EntityComponent`, over Stride's own `Get<T>()` and `GetAll<T>()` | A type argument that is not a component no longer compiles; it never found anything. `GetComponent` and `GetComponents` stay for readers who look for those names and may go in a later release. |
| `scriptSystem.Delay(seconds)` waited on a thread-pool timer | counts the game's clock frame by frame, like `DelayWarped` without the warp | It pauses with the game and ends when the game stops. |
| `Basic2DCameraController.OrthographicSizeDefault` | `DefaultOrthographicSize` | Named like every other default. |
| `Basic2DCameraController.ShowInstructions`; `Basic3DCameraController.ShowHelp` and `ShowInstructions` | `ShowHelp` on both | One property that works before the script starts and after: the help section is hidden and shown in place. |
| `Stride.CommunityToolkit.Collections.RandomListExtensions` (`random.Choose(...)`, `random.Shuffle(...)`) | `Stride.CommunityToolkit.Mathematics.RandomListExtensions` | Beside the other `Random` helpers; change the `using`. |
| `Capsule2DProceduralModel.TotalHeight` | `Height` | The same value, the whole shape end to end, under the name `Primitive2D.Capsule(height)` uses. |
| `AddParticleStagesAndFeatures()` threw `NullReferenceException` for a missing stage | `InvalidOperationException` naming the stage and what creates it | |
| `TextOverlay.CharacterWidth` (`[Obsolete]`, no effect) | removed | The overlay measures text with its font. |
| `AxialGizmoBase.GetRedColor()`, `GetGreenColor()`, `GetBlueColor()` | `RedColor`, `GreenColor`, `BlueColor` properties | Protected; a derived gizmo drops the parentheses. |
| `texture.Resize(width, height, graphicsContext, format)` and `ReFormat(graphicsContext, format)` returned `null` on any failure and repainted the window's back buffer | `texture.Resize(width, height, services, format)` and `ReFormat(services, format)` draw through a `TextureCanvas` and return the new texture | Nothing is drawn on screen, and an error throws instead of returning `null`. `services` is `game.Services` or a script's `Services`. |
| `DisplayScale.MinScale` as `static readonly` | `const` | Recompile against the new version. |

#### Physics packages

| Before | After | Note |
|---|---|---|
| Bullet `heightmap.ToMesh(device, m_QuadSideWidthX, m_QuadSideWidthZ, TEXTURE_REPEAT, out points, tessellation)`, and the same names on `IntersectsRay` and `ToWorldPoints` | parameters `quadWidthX`, `quadWidthZ`, `textureRepeat` | Only a call with named arguments changes. |
| Bullet `heightmap.ToMesh(..., tessellation)` with a tessellation above 1 read the heightmap's first rows and columns for every vertex | reads every `tessellation`-th sample | The mesh spanned the terrain, with one corner's heights stretched over it. |
| Bullet `HeightmapExtensions.HeightMultiplier`, `VertexTypePosTexNormColor.Size` as `static readonly` | `const` | Recompile against the new version. |
| Bullet `simulation.Raycast(entityPosition, ...)`, both overloads | parameter `entity` | Only a call with the named argument changes. |
| Bullet `camera.RaycastMouse(simulation, screenPosition, ...)` | `[Obsolete]`; use `camera.Raycast(simulation, screenPosition, ...)` | The same cast. `RaycastMouse(scriptComponent, ...)`, which reads the mouse itself, stays. |
| `game.AddSkybox(entityName: null)` | `entityName` is `string`, default `"Skybox"` | An entity needs a name. |

#### Box2D

| Before | After | Note |
|---|---|---|
| `simulation.GetWorldId()` | `simulation.WorldId` | A property for a value that never changes. |
| `simulation.GetAllBodyIds()` returned a new list | `simulation.Bodies`, a live view | Enumerate it between creating and removing bodies; `ToList()` gives the old copy. |
| `PhysicsQueries2D.RaycastClosest(...)` returned a six-field tuple | returns `QueryRaycastHit?` | The same fields, named; `null` is a miss. |
| `IBox2DSimulationUpdate`: both methods abstract | both have empty default bodies | Implement the one you need; the empty other one can go. |

#### Shapes, Charts, DebugShapes, ImGui

| Before | After | Note |
|---|---|---|
| `ShapeComponent.Inherit`, `ShapeFill.TestbedAlpha` as `static readonly` | `const` | Usable as a default parameter value; recompile against the new version. |
| `ShapeBatchFeature` could be derived from | `sealed` | Nothing in the toolkit derives from it, and its render stages are its own. |
| `ChartOptions.Range`, `Axes`, `Grid`, `Labels`, `Legend`, `Title`, `Series`, `Cursor` had setters | `init` | They were read at construction, so a group swapped in later was half applied. Set them in the initializer. |
| `ImmediateDebugRenderObject.Draw*(ref Vector3, ..., ref Color, ...)` | `in` parameters | The values were never written. A call that passed `ref` passes `in`, or nothing. |
| `ImmediateDebugRenderSystem` dropped the newest shape when a queue was full | drops the oldest | What is drawn is always the latest asked for; `MaxPrimitives` and `MaxPrimitivesWithLifetime` say so. |
| `AddImmediateDebugRenderFeature()` threw `NullReferenceException` for a missing stage | `InvalidOperationException` naming the stage and what creates it | |
| `ImGuiSystem.ImGuiContext` public field | read-only property | The same value. |
| ImGui text fields did not copy or paste | they use dear imgui's own clipboard hooks, the Win32 clipboard on Windows | The toolkit's empty hooks had replaced them. |

### 🎉 New Features

- `SceneSetup.WindowTitle`, `Resizable` and `DpiAware`: the window title, whether the window resizes, and per-monitor DPI awareness, set on the setup instead of in every example's start callback and above every `new Game()`. The examples lost those lines.
- `camera.TryWorldToScreenPoint(position, graphicsDevice, out screenPoint)`: where a world position lands on screen, in pixels, and `false` when it is behind the camera or off screen; an overload gives the normalized position. The two E09 renderers wrote the clip test by hand.
- `QuaternionUtil.LookAt(eye, target, up)` in `Stride.CommunityToolkit.Mathematics`: the rotation of a camera, or anything that looks down its -Z, at a point. Two examples derived it themselves, differently.
- `ShapeComponent.Box(size)`: the four corners of a rectangle centred on the origin, for a shape component's `Vertices` or a shape batch; the Box2D examples wrote them by hand in nine places.
- Box2D collision groups: `simulation.CollisionMatrix` is now consulted by the simulation, for the shapes put in a group with `shapeDef.InCollisionGroup(group)`. The matrix existed before, and nothing read it.
- `game.AddGrabber2D(simulation)` puts the `Grabber2DScript` on the camera entity and returns it, the way `game.AddGrabber()` does for Bepu; the Box2D examples use it.
- `body.TeleportAtRest(position, rotation = null)` in `Stride.CommunityToolkit.Bepu`: teleports a Bepu body and leaves it still and awake, which is what a reset key or a respawn wants; `Teleport` alone keeps the velocity, the spin and the sleep. The seven examples that wrote the four lines by hand use it.
- `scene.FindEntity(name, recursive: false)`: the first entity with a name among a scene's top-level entities, or with `recursive: true` among their descendants too. `entity.FindEntity` and `entity.FindEntityRecursive` now call it on the entity's scene, and an entity that is not in a scene gets an `InvalidOperationException` that names the entity and says what to do, instead of a `NullReferenceException`.