using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace E05_3D_FirstPersonCharacter;

// Drives every FirstPersonControllerComponent each frame. Two modes, toggled with V:
//  • Walk - first-person with gravity; WASD + Space to jump (buffered) + Shift to sprint;
//           collides via the Bepu CharacterComponent.
//  • Fly  - noclip free camera; WASD + mouse, Space/Ctrl up/down, Shift boost; moves the camera
//           transform directly while the physics body is parked.
public class FirstPersonControllerProcessor : EntityProcessor<FirstPersonControllerComponent>
{
    private const float MaxPitch = MathUtil.PiOverTwo * 0.99f;

    // How long a Space press stays live waiting for ground support (jump buffering). Keep it
    // shorter than any jump's airtime, so a buffered press can never fire twice.
    private const float JumpBufferSeconds = 0.15f;

    // Characters feel floaty under real-earth gravity (-9.81); most games run ~1.5–2×.
    private const float Gravity = -17f;

    public override void Update(GameTime time)
    {
        var input = Services.GetService<InputManager>();

        if (input is null) return;

        var dt = (float)time.Elapsed.TotalSeconds;

        foreach (var pair in ComponentDatas)
        {
            UpdateOne(pair.Key, pair.Key.Entity, input, dt);
        }
    }

    private void UpdateOne(FirstPersonControllerComponent controller, Entity entity, InputManager input, float dt)
    {
        if (!controller.Initialized)
        {
            SetMouseLock(controller, input, true);

            if (controller.Character is not null)
            {
                controller.FlyPosition = controller.Character.Entity.Transform.Position + new Vector3(0, controller.EyeHeight, 0);
            }

            controller.Initialized = true;
        }

        // Escape frees the cursor; clicking the window grabs it again.
        if (input.IsKeyPressed(Keys.Escape))
        {
            SetMouseLock(controller, input, false);
        }
        else if (!controller.MouseLocked && input.IsMouseButtonPressed(MouseButton.Left))
        {
            SetMouseLock(controller, input, true);
        }

        // Mouse look (both modes). Row-vector convention: RotationX*RotationY = intrinsic
        // yaw-then-local-pitch.
        if (controller.MouseLocked)
        {
            controller.Yaw -= input.MouseDelta.X * controller.MouseSensitivity;
            controller.Pitch = MathUtil.Clamp(controller.Pitch - input.MouseDelta.Y * controller.MouseSensitivity, -MaxPitch, MaxPitch);
            entity.Transform.Rotation = Quaternion.RotationX(controller.Pitch) * Quaternion.RotationY(controller.Yaw);
        }

        var character = controller.Character;

        if (character is null) return;

        // Set world gravity once the body has joined a simulation (Simulation is null until the
        // body is attached, when its entity enters the scene).
        if (!controller.GravityApplied && character.Simulation is not null)
        {
            character.Simulation.PoseGravity = new Vector3(0f, Gravity, 0f);
            controller.GravityApplied = true;
        }

        if (input.IsKeyPressed(Keys.V))
        {
            SetFly(controller, !controller.Fly);
        }

        if (controller.Fly)
        {
            UpdateFly(controller, entity, input, dt);
        }
        else
        {
            UpdateWalk(controller, entity, input, dt);
        }
    }

    private void SetMouseLock(FirstPersonControllerComponent controller, InputManager input, bool locked)
    {
        if (locked)
        {
            input.LockMousePosition(true);
        }
        else
        {
            input.UnlockMousePosition();
        }

        var game = Services.GetService<IGame>();

        if (game is not null) game.IsMouseVisible = !locked;

        controller.MouseLocked = locked;
    }

    private static void SetFly(FirstPersonControllerComponent controller, bool on)
    {
        controller.Fly = on;

        if (controller.Character is null) return;

        if (on)
        {
            // Enter fly from the current eye position; park the body. MoveVector PERSISTS - every
            // physics tick re-applies it as the character's target velocity, so without clearing it
            // the parked capsule keeps walking on its own.
            controller.FlyPosition = controller.Character.Entity.Transform.Position + new Vector3(0, controller.EyeHeight, 0);
            controller.Character.MoveVector = Vector2.Zero;
            controller.Character.Gravity = false;
            controller.Character.LinearVelocity = Vector3.Zero;
        }
        else
        {
            // Exit fly: teleport the body under the camera, then resume gravity. Writing
            // Transform.Position instead would move only the mesh; the physics sync overwrites it.
            controller.Character.Teleport(controller.FlyPosition - new Vector3(0, controller.EyeHeight, 0), controller.Character.Orientation);
            controller.Character.LinearVelocity = Vector3.Zero;
            controller.Character.Gravity = true;
        }
    }

    private static void UpdateWalk(FirstPersonControllerComponent controller, Entity entity, InputManager input, float dt)
    {
        // Horizontal axes from yaw only (no vertical component when walking).
        var (sinY, cosY) = MathF.SinCos(controller.Yaw);
        var forward = new Vector3(-sinY, 0, -cosY);
        var right = new Vector3(cosY, 0, -sinY);
        var move = Vector3.Zero;

        if (input.IsKeyDown(Keys.W)) move += forward;
        if (input.IsKeyDown(Keys.S)) move -= forward;
        if (input.IsKeyDown(Keys.A)) move -= right;
        if (input.IsKeyDown(Keys.D)) move += right;
        if (move.LengthSquared() > 0) move.Normalize();

        var character = controller.Character!;

        // Sprint: Shift scales the move vector (MoveVector treats vector length as a speed factor).
        if (input.IsKeyDown(Keys.LeftShift) && move.LengthSquared() > 0)
        {
            move *= controller.SprintMultiplier;
        }

        // MoveVector is the XZ plane of the old Move(Vector3): X stays X, world Z becomes its Y
        character.MoveVector = new Vector2(move.X, move.Z);

        // Jump buffering: Bepu consumes-and-clears TryJump every physics tick even when the
        // attempt fails, and ground support flickers off on slopes and just before landings - so a
        // raw edge-triggered press is often swallowed. Keep re-arming for a short window instead;
        // the first supported tick executes it. Re-arming right after a successful jump is
        // harmless: the character is airborne (unsupported) for far longer than the buffer.
        if (input.IsKeyPressed(Keys.Space)) controller.JumpBuffer = JumpBufferSeconds;

        if (controller.JumpBuffer > 0f)
        {
            character.TryJump();
            controller.JumpBuffer -= dt;
        }

        // The camera rides at eye height above the capsule; physics moves the capsule, we follow.
        entity.Transform.Position = character.Entity.Transform.Position + new Vector3(0, controller.EyeHeight, 0);
    }

    private static void UpdateFly(FirstPersonControllerComponent controller, Entity entity, InputManager input, float dt)
    {
        // Full 3D axes from the camera orientation (W follows where you look).
        var rotation = entity.Transform.Rotation;
        var forward = Vector3.Transform(-Vector3.UnitZ, rotation);
        var right = Vector3.Transform(Vector3.UnitX, rotation);
        var move = Vector3.Zero;

        if (input.IsKeyDown(Keys.W)) move += forward;
        if (input.IsKeyDown(Keys.S)) move -= forward;
        if (input.IsKeyDown(Keys.A)) move -= right;
        if (input.IsKeyDown(Keys.D)) move += right;
        if (input.IsKeyDown(Keys.Space)) move += Vector3.UnitY;
        if (input.IsKeyDown(Keys.LeftCtrl)) move -= Vector3.UnitY;
        if (move.LengthSquared() > 0) move.Normalize();

        var speed = controller.FlySpeed * (input.IsKeyDown(Keys.LeftShift) ? controller.FlyBoost : 1f);

        controller.FlyPosition += move * speed * dt;
        entity.Transform.Position = controller.FlyPosition;
        // The body stays parked on its own: motor target zeroed and gravity off since SetFly.
    }
}