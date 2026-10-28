# Camera Controllers

Every code-only example lets you fly or pan the camera straight away, without writing an input
script first. That comes from two ready-made `SyncScript`s in the `Stride.CommunityToolkit.Scripts`
namespace - [`Basic2DCameraController`](xref:Stride.CommunityToolkit.Scripts.Basic2DCameraController)
and [`Basic3DCameraController`](xref:Stride.CommunityToolkit.Scripts.Basic3DCameraController) - which
the setup helpers attach to the main camera.

The keys are the same in every scene, so the controllers also print them on screen: press
<kbd>F2</kbd> to expand the camera's help, <kbd>F3</kbd> to move the overlay to another corner and
<kbd>F4</kbd> to hide the whole overlay.

## Where they come from

```csharp
game.Run(new SceneSetup2D(), start: Start);   // orthographic camera + Basic2DCameraController
game.Run(new SceneSetup3D(), start: Start);   // perspective camera + Basic3DCameraController
```

The setups' default cameras come with a controller: `CameraSetup.Orthographic().WithController()` and
`CameraSetup.Perspective().WithController()`. A camera without `WithController()` has none, and then
you add the controller yourself - either from the game, which finds the main camera for you, or from
the camera entity if you already hold it:

```csharp
game.Run(new SceneSetup3D { Camera = CameraSetup.Perspective() }, start: Start);

void Start(Scene scene)
{
    game.Add3DCameraController();                 // finds the camera named "Main"

    // or, on an entity you created
    cameraEntity.Add2DCameraController();
}
```

Both helpers accept the help overlay's toggle key and whether it starts collapsed; the 3D one also
takes an optional `Anchor` for the overlay's corner, edge or centre - leave it out and the
overlay keeps whatever position it already has - and `showHelp: false` registers no help at all.
These must be decided up front - the overlay section is created in `Start()` - which is why they are
parameters rather than properties you set later.

## The 2D controller

`Basic2DCameraController` moves an orthographic camera around the XY plane and zooms it by changing
`OrthographicSize`.

| Input | Effect | Tune with |
|---|---|---|
| Arrow keys | Move | `CameraMoveSpeed` (5 units/s) |
| <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd> | Move - **off by default**, see below | `EnableWasdMovement` |
| Hold <kbd>Shift</kbd> | Move and zoom faster | `SpeedFactor` (×5) |
| Mouse wheel | Zoom toward the cursor, 10 % of the view per notch | `ZoomStep` (0.1), `ZoomToCursor` (on), `MinOrthographicSize` (0.1), `MaxOrthographicSize` (100) |
| Right mouse drag | Pan - the point under the cursor stays under the cursor | `EnableMouseDragPan` (on), `MouseDragButton` (right) |
| <kbd>H</kbd> | Reset to where the camera *started*, at the zoom it started with (or `DefaultOrthographicSize` if set) | `DefaultOrthographicSize` |

Three defaults to know:

- **WASD is opt-in**, so a game built on the toolkit keeps those keys. The arrow keys are always
  live; turn WASD on for tools and playgrounds where nothing else needs it.
- **Zoom is per notch, not per second**, and multiplicative: every notch changes the visible area by
  the same fraction however far in or out you are.
- **Drag is cursor-locked.** A drag moves the world by exactly the cursor's movement in world units.
  There is no speed to tune, and <kbd>Shift</kbd> has no effect on it.

### Optional behaviours

All off unless stated, and all plain properties on the script:

- **`EnableSmoothing`** (`SmoothingSpeed` 10) - eases the camera towards its target position *and*
  zoom instead of applying input directly.
- **`EnableBounds`** (`MinBounds`, `MaxBounds`) - clamps the camera's XY position to a rectangle,
  for levels with an edge.
- **`EnableScreenEdgeMovement`** (`ScreenEdgeBorderWidth` 10 px) - RTS-style panning when the
  cursor sits at a window edge.
- **`FollowTarget`** (`FollowOffset`, `FollowSmoothing` 5) - tracks an entity; while set, the
  manual movement controls are ignored, though zoom, bounds and reset still work.

### Changing settings after setup

The helpers return the camera entity, and the script reads its properties every frame, so anything
except the overlay parameters can be changed at any time:

```csharp
var camera = game.Add2DCameraController();
var controller = camera.Get<Basic2DCameraController>();

controller.EnableWasdMovement = true;
controller.ZoomStep = 0.2f;
controller.FollowTarget = player;
```

## The 3D controller

`Basic3DCameraController` is a free-look camera in the style of Stride's editor: move in the camera's
own axes, look around with the right mouse button, and never roll.

| Input | Effect | Tune with |
|---|---|---|
| <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd> or arrow keys | Forward / left / back / right, relative to where the camera looks | `KeyboardMovementSpeed` (5 units/s) |
| <kbd>Q</kbd> / <kbd>E</kbd> | Descend / ascend | `KeyboardMovementSpeed.Y` |
| Hold <kbd>Shift</kbd> | Move faster | `SpeedFactor` (×5) |
| Right mouse drag | Look around; the cursor is locked and hidden while the button is held | `MouseRotationSpeed` |
| Numpad <kbd>8</kbd> <kbd>2</kbd> / <kbd>4</kbd> <kbd>6</kbd> | Pitch / yaw from the keyboard | `KeyboardRotationSpeed` (3 rad/s) |
| <kbd>H</kbd> | Reset to the position *and* rotation the camera started with | - |
| Gamepad | Left stick moves, triggers descend/ascend, right stick looks, <kbd>A</kbd> or a shoulder button sprints | `Gamepad` (off) |
| Touch (non-desktop) | One-finger drag looks, two-finger drag pans and pinch moves along the view axis | `TouchRotationSpeed`, `TouchMovementSpeed` |

Two details:

- **Pitch is clamped** just short of straight up and straight down, and yaw always turns around the
  world's up axis, so the horizon stays level. The camera cannot roll.
- **The help overlay prints the live position and rotation** while expanded, and the rotation is
  yaw/pitch/roll in degrees - the same order and unit that `Add3DCamera()`'s `initialRotation` and
  `SetCameraRotation()` take. Fly to a view you like, press <kbd>F2</kbd>, and the numbers on screen
  paste straight into the setup code.

The 2D controller has no `helpPosition` parameter because the overlay belongs to the whole scene, not
to the camera; move it with <kbd>F3</kbd> or set `TextOverlay.GetOrCreate(game).Position` directly.
Size, font and background of that block are the overlay's own settings - see
[Text Overlay](../rendering/text-overlay.md).

## The orbit controller

`Basic3DOrbitCameraController` circles a target point instead of flying - the right camera for
inspecting one thing: a scene centrepiece, a model viewer, a 3D chart. Attach it to a camera entity
and set `Target`; the entity's starting position relative to the target provides the initial distance
and angles, so framing the camera first (for example with a chart's `FrameCamera`) also frames the
orbit.

| Input | Action | Configure with |
| --- | --- | --- |
| Left mouse drag | Orbit around the target; pitch clamped to about ±85° | `OrbitButton`, `OrbitSensitivity`, `MinPitch`/`MaxPitch` |
| Mouse wheel | Dolly in and out, 10 % of the distance per notch; ignored while middle-drag panning | `ZoomStep` (0.1), `MinDistance` (0.5), `MaxDistance` (500) |
| Middle mouse drag | Pan the target - the point under the cursor stays roughly under the cursor | `PanButton` |
| Hold <kbd>Shift</kbd> | Faster zoom and pan | `SpeedFactor` (5) |
| <kbd>H</kbd> | Reset to the starting view | - |
| <kbd>F2</kbd> | Collapse or expand the help, with the live target, distance and angles | `HelpToggleKey`, `HelpCollapsed` |

`E11_3D_Charts` uses it: the fly controller from the setup is removed and an orbit centred on the
chart takes its place.

## When the defaults are wrong

Both controllers are ordinary `SyncScript`s with nothing else attached to the camera, so replacing
one is a matter of not adding it:

```csharp
game.Run(new SceneSetup3D { Camera = CameraSetup.Perspective() }, start: Start);   // no controller

void Start(Scene scene) => game.GetCameraEntity().Add(new MyOrbitCameraController());
```

If you keep a controller but want the on-screen help gone, pass `showHelp: false` to the 3D helper,
or set `ShowHelp = false` on either script, before it starts or at any time after. Hiding the
whole overlay with <kbd>F4</kbd> is the runtime equivalent.

## See them in use

- [Basic 2D Scene (Debug Rendering)](../code-only/examples/debug-render-2d.md) - the 2D controller on a
  physics playground.
- [Spawn Menu (2D)](../code-only/examples/spawn-menu-2d.md) - a scene that adds its own key help to the
  same overlay the camera uses.
- [Instancing](../code-only/examples/instancing.md) - the 3D controller flown around a large scene.