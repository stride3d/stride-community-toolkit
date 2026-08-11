using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

namespace Example20_BepuFirstPersonCharacter;

// Drives every FirstPersonControllerComponent each frame. Two modes, toggled with V:
//  • Walk - first-person with gravity; WASD + Space to jump (buffered) + Shift to sprint;
//           collides via the Bepu CharacterComponent.
//  • Fly  - noclip free camera; WASD + mouse, Space/Ctrl up/down, Shift boost; moves the camera
//           transform directly while the physics body is parked.
public class FirstPersonControllerProcessor : EntityProcessor<FirstPersonControllerComponent>
{
    const float MaxPitch = MathUtil.PiOverTwo * 0.99f;

    // How long a Space press stays live waiting for ground support (jump buffering). Keep it
    // shorter than any jump's airtime, so a buffered press can never fire twice.
    const float JumpBufferSeconds = 0.15f;

    // Characters feel floaty under real-earth gravity (-9.81); most games run ~1.5–2×.
    const float Gravity = -17f;

    public override void Update(GameTime time)
    {
        var input = Services.GetService<InputManager>();
        if (input == null) return;
        float dt = (float)time.Elapsed.TotalSeconds;

        foreach (var pair in ComponentDatas)
            UpdateOne(pair.Key, pair.Key.Entity, input, dt);
    }

    void UpdateOne(FirstPersonControllerComponent c, Entity entity, InputManager input, float dt)
    {
        if (!c.Initialized)
        {
            SetMouseLock(c, input, true);
            if (c.Character != null)
                c.FlyPosition = c.Character.Entity.Transform.Position + new Vector3(0, c.EyeHeight, 0);
            c.Initialized = true;
        }

        // Escape frees the cursor; clicking the window grabs it again.
        if (input.IsKeyPressed(Keys.Escape)) SetMouseLock(c, input, false);
        else if (!c.MouseLocked && input.IsMouseButtonPressed(MouseButton.Left)) SetMouseLock(c, input, true);

        // Mouse look (both modes). Row-vector convention: RotationX*RotationY = intrinsic
        // yaw-then-local-pitch.
        if (c.MouseLocked)
        {
            c.Yaw -= input.MouseDelta.X * c.MouseSensitivity;
            c.Pitch = MathUtil.Clamp(c.Pitch - input.MouseDelta.Y * c.MouseSensitivity, -MaxPitch, MaxPitch);
            entity.Transform.Rotation = Quaternion.RotationX(c.Pitch) * Quaternion.RotationY(c.Yaw);
        }

        var character = c.Character;
        if (character == null) return;

        // Set world gravity once the body has joined the simulation (Simulation is null before
        // the entity's first physics tick).
        if (!c.GravityApplied && character.Simulation != null)
        {
            character.Simulation.PoseGravity = new Vector3(0f, Gravity, 0f);
            c.GravityApplied = true;
        }

        if (input.IsKeyPressed(Keys.V)) SetFly(c, !c.Fly);

        if (c.Fly) UpdateFly(c, entity, input, dt);
        else UpdateWalk(c, entity, input, dt);
    }

    void SetMouseLock(FirstPersonControllerComponent c, InputManager input, bool locked)
    {
        if (locked) input.LockMousePosition(true);
        else input.UnlockMousePosition();

        var game = Services.GetService<IGame>();
        if (game != null) game.IsMouseVisible = !locked;
        c.MouseLocked = locked;
    }

    static void SetFly(FirstPersonControllerComponent c, bool on)
    {
        c.Fly = on;
        if (c.Character == null) return;
        if (on)
        {
            // Enter fly from the current eye position; park the body. The motor target set by
            // Move() PERSISTS - every physics tick re-applies it as the character's target
            // velocity, so without clearing it the parked capsule keeps walking on its own.
            c.FlyPosition = c.Character.Entity.Transform.Position + new Vector3(0, c.EyeHeight, 0);
            c.Character.Move(Vector3.Zero);
            c.Character.Gravity = false;
            c.Character.LinearVelocity = Vector3.Zero;
        }
        else
        {
            // Exit fly: keep the player where the camera is - teleport the body under the camera,
            // then resume gravity. Character.Teleport is the proper API; setting Transform.Position
            // directly is overwritten by the physics sync and flings you elsewhere.
            c.Character.Teleport(c.FlyPosition - new Vector3(0, c.EyeHeight, 0), c.Character.Orientation);
            c.Character.LinearVelocity = Vector3.Zero;
            c.Character.Gravity = true;
        }
    }

    static void UpdateWalk(FirstPersonControllerComponent c, Entity entity, InputManager input, float dt)
    {
        // Horizontal axes from yaw only (no vertical component when walking).
        var sinY = MathF.Sin(c.Yaw);
        var cosY = MathF.Cos(c.Yaw);
        var forward = new Vector3(-sinY, 0, -cosY);
        var right = new Vector3(cosY, 0, -sinY);

        var move = Vector3.Zero;
        if (input.IsKeyDown(Keys.W)) move += forward;
        if (input.IsKeyDown(Keys.S)) move -= forward;
        if (input.IsKeyDown(Keys.A)) move -= right;
        if (input.IsKeyDown(Keys.D)) move += right;
        if (move.LengthSquared() > 0) move.Normalize();

        var character = c.Character!;

        // Sprint: Shift scales the move vector (Character.Move treats vector length as a speed factor).
        if (input.IsKeyDown(Keys.LeftShift) && move.LengthSquared() > 0)
            move *= c.SprintMultiplier;
        character.Move(move);

        // Jump buffering: Bepu consumes-and-clears TryJump every physics tick even when the
        // attempt fails, and ground support flickers off on slopes and just before landings - so a
        // raw edge-triggered press is often swallowed. Keep re-arming for a short window instead;
        // the first supported tick executes it. Re-arming right after a successful jump is
        // harmless: the character is airborne (unsupported) for far longer than the buffer.
        if (input.IsKeyPressed(Keys.Space)) c.JumpBuffer = JumpBufferSeconds;
        if (c.JumpBuffer > 0f)
        {
            character.TryJump();
            c.JumpBuffer -= dt;
        }

        // The camera rides at eye height above the capsule; physics moves the capsule, we follow.
        entity.Transform.Position = character.Entity.Transform.Position + new Vector3(0, c.EyeHeight, 0);
    }

    static void UpdateFly(FirstPersonControllerComponent c, Entity entity, InputManager input, float dt)
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

        float speed = c.FlySpeed * (input.IsKeyDown(Keys.LeftShift) ? c.FlyBoost : 1f);
        c.FlyPosition += move * speed * dt;
        entity.Transform.Position = c.FlyPosition;
        // The body stays parked on its own: motor target zeroed and gravity off since SetFly.
    }
}
