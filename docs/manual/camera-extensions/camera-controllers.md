# Camera Controllers

Every code-only example lets you fly or pan the camera straight away, without writing an input
script first. That comes from two ready-made `SyncScript`s in the `Stride.CommunityToolkit.Scripts`
namespace - [`Basic2DCameraController`](xref:Stride.CommunityToolkit.Scripts.Basic2DCameraController)
and [`Basic3DCameraController`](xref:Stride.CommunityToolkit.Scripts.Basic3DCameraController) - which
the setup helpers attach to the main camera. This page lists what they respond to, which knobs they
expose, and how to tune or replace them once a project outgrows the defaults.

The keys are the same in every scene, so the controllers also print them on screen: press
<kbd>F2</kbd> to expand the camera's help, <kbd>F3</kbd> to move the overlay to another corner and
<kbd>F4</kbd> to hide the whole overlay. If you forget everything else on this page, remember
<kbd>F2</kbd>.

## Where they come from

```csharp
game.SetupBase2DScene();   // compositor + 2D camera + Basic2DCameraController + ground
game.SetupBase3DScene();   // compositor + 3D camera + light + Basic3DCameraController + ground
```

The `SetupBase2DScene()` / `SetupBase3DScene()` helpers from the Bepu and Bullet packages include a
controller. The plain `SetupBase2D()` / `SetupBase3D()` helpers do not, so when you build a scene
from those you add the controller yourself - either from the game, which finds the main camera for
you, or from the camera entity if you already hold it:

```csharp
game.SetupBase3D();
game.Add3DCameraController();                     // finds the camera named "Main"

// or, on an entity you created
cameraEntity.Add2DCameraController();
```

Both helpers accept the help overlay's toggle key and whether it starts collapsed; the 3D one also
takes an optional `DisplayPosition` for the overlay corner - leave it out and the overlay keeps
whatever position it already has (`DisplayPosition.None` registers no help at all).
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
| Mouse wheel | Zoom, 10 % of the view per notch | `ZoomStep` (0.1), `MinOrthographicSize` (0.1), `MaxOrthographicSize` (100) |
| Middle mouse drag | Pan - the point under the cursor stays under the cursor | `EnableMouseDragPan` (on), `MouseDragButton` |
| <kbd>H</kbd> | Reset to where the camera *started*, at `OrthographicSizeDefault` (10) | `OrthographicSizeDefault` |

Three of those defaults are decisions rather than accidents:

- **WASD is opt-in.** A camera helper that silently owns <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd>
  <kbd>D</kbd> is a nuisance the moment you start a game on top of the toolkit, because those are the
  keys the game wants. The arrow keys are always live; turn WASD on for tools and playgrounds where
  nothing else needs it.
- **Zoom is per notch, not per second.** A wheel notch is an impulse - it arrives on one frame and
  is gone the next - so scaling it by delta time, as an earlier version did, only made each notch
  depend on the frame rate: a stutter turned one click into a lurch. Zoom is also multiplicative, so
  every notch changes the visible area by the same fraction whether you are zoomed far in or far
  out; subtracting a constant is a nudge at size 100 and a wall at size 1.
- **Drag is cursor-locked.** `OrthographicSize` is the visible height and mouse positions are
  normalised, so a drag moves the world by exactly the cursor's movement in world units. There is no
  speed to tune, and <kbd>Shift</kbd> has no effect on it.

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
own axes, look around with the right mouse button, and never roll. It is adapted from the camera
script in Stride's own templates.

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

Two details worth knowing before you try to fight them:

- **Pitch is clamped** just short of straight up and straight down, and yaw always turns around the
  world's up axis. That is what keeps the horizon level however long you fly - there is no way to
  roll the camera, by design.
- **The help overlay prints the live position and rotation** while expanded, and the rotation is
  yaw/pitch/roll in degrees - the same order and unit that `Add3DCamera()`'s `initialRotation` and
  `SetCameraRotation()` take. Fly to a view you like, press <kbd>F2</kbd>, and the numbers on screen
  paste straight into the setup code. Plain XYZ Euler angles would look just as plausible and aim the
  camera somewhere else entirely, which is why they are not the ones printed.

The overlay corner can be chosen with the `displayPosition` parameter of `Add3DCameraController()`;
without it the controller leaves the corner alone, so `DebugOverlay.GetOrCreate(game).Position` set
anywhere - even before the controller has started - is respected. The 2D controller has no such
parameter because the overlay belongs to the whole scene, not to the camera; move it with
<kbd>F3</kbd> or set the position directly. Size, font and
background of that block are the overlay's own settings - see [Debug Overlay](../rendering/debug-overlay.md).

## When the defaults are wrong

Both controllers are ordinary `SyncScript`s with nothing else attached to the camera, so replacing
one is a matter of not adding it:

```csharp
game.SetupBase3D();                  // no controller
game.GetCameraEntity().Add(new MyOrbitCamera());
```

If you keep a controller but want the on-screen help gone, pass `displayPosition: DisplayPosition.None`
to the 3D helper, or set `ShowInstructions = false` on either script after it has started. Hiding the
whole overlay with <kbd>F4</kbd> is the runtime equivalent.

## See them in use

- [Basic2D Scene (Debug Rendering)](../code-only/examples/debug-render-2d.md) - the 2D controller on a
  physics playground.
- [Spawn Menu (2D)](../code-only/examples/spawn-menu-2d.md) - a scene that adds its own key help to the
  same overlay the camera uses.
- [Instancing](../code-only/examples/instancing.md) - the 3D controller flown around a large scene.